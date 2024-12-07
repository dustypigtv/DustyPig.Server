using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public sealed class FirebaseNotificationsManager : IHostedService, IDisposable
    {
        private const int CHUNK_SIZE = 1000;

        //About once/minute check for and send any new notifications
        private const int ONE_SECOND = 1000;
        private const int ONE_MINUTE = ONE_SECOND * 60;


        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;
        private readonly ILogger<FirebaseNotificationsManager> _logger;

        public FirebaseNotificationsManager(ILogger<FirebaseNotificationsManager> logger)
        {
            _logger = logger;
            _timer = new Timer(new TimerCallback(TimerTickedAsync), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _timer.Change(0, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return Task.CompletedTask;
        }



        private async void TimerTickedAsync(object state)
        {
            try
            {
                await DoWorkAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(ONE_MINUTE, Timeout.Infinite);
        }


        private async Task DoWorkAsync()
        {
            //Separate functions aren't called more than once, but are separated to make
            //Scoping easier, and smaller-faster progress saves

            await RemoveOldFCMTokensAsync();


            //Now do the notifications
            await SendNotifications2Async();
        }


        private async Task RemoveOldFCMTokensAsync()
        {
            using var db = new AppDbContext();
            string query = $"DELETE FROM {nameof(db.FCMTokens)} WHERE {nameof(FCMToken.LastSeen)} < '{DateTime.UtcNow.AddMonths(-3):yyyy-MM-dd}'";
            await db.Database.ExecuteSqlRawAsync(query, _cancellationToken);
        }



        private async Task SendNotifications2Async()
        {
            using var db = new AppDbContext();

            while (true)
            {
                //Throttle
                await Task.Delay(ONE_SECOND, _cancellationToken);
                
                //Get a profile with unseen/unsent notifications
                var profile = await db.Profiles
                    .AsNoTracking()

                    .Include(p => p.FCMTokens)

                    .Include(p => p.Notifications.Where(n => !(n.Sent || n.Seen)))
                    .ThenInclude(n => n.MediaEntry)

                    .Include(p => p.Notifications.Where(n => !(n.Sent || n.Seen)))
                    .ThenInclude(n => n.GetRequest)

                    .Include(p => p.Notifications.Where(n => !(n.Sent || n.Seen)))
                    .ThenInclude(n => n.Friendship)

                    .Where(p => p.Notifications.Any(n => !(n.Sent || n.Seen)))

                    .OrderBy(p => p.Notifications.Select(n => n.Timestamp).OrderBy(t => t).First())
                    
                    .FirstOrDefaultAsync(_cancellationToken);

                if (profile == null)
                    return;


                // Update Firestore to let mobile listeners know to update their list of notifications
                try
                {
                    DocumentReference docRef = FDB.Service.Collection(Constants.FDB_KEY_ALERTS_COLLECTION).Document(profile.Id.ToString());
                    
                    //The clients only look for a change to the document, not what the changes are
                    //A 1-char key and 8 byte timestamp is small, fast and always updates
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "t", DateTime.UtcNow.Ticks }
                    };
                    await docRef.SetAsync(data);

                    //If there are not FCM tokens to send pushes to, consider all notifications sent
                    if (profile.FCMTokens.Count == 0)
                    {
                        foreach (var n in profile.Notifications)
                        {
                            n.Sent = true;
                            db.Notifications.Update(n);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating alerts in Firestore");
                }

                //Push any notifications to mobile devices
                if (profile.FCMTokens.Count > 0)
                {
                    foreach (var notification in profile.Notifications)
                    {
                        foreach (var fcmToken in notification.Profile.FCMTokens)
                        {
                            var msgData = new Dictionary<string, string>
                                {
                                    { Constants.FCM_KEY_ID, notification.Id.ToString() },
                                    { Constants.FCM_KEY_PROFILE_ID, notification.ProfileId.ToString() },
                                    { Constants.FCM_KEY_NOTIFICATION_TYPE, ((int)notification.NotificationType).ToString() }
                                };


                            if (notification.MediaEntry != null)
                            {
                                msgData.Add(Constants.FCM_KEY_MEDIA_ID, notification.MediaEntry.Id.ToString());
                                msgData.Add(Constants.FCM_KEY_MEDIA_TYPE, ((int)notification.MediaEntry.EntryType).ToString());
                            }
                            else
                            {
                                var newMediaNotificationTypes = new NotificationTypes[]
                                {
                                        NotificationTypes.NewMediaPending,
                                        NotificationTypes.NewMediaRejected,
                                        NotificationTypes.NewMediaRequested
                                };

                                if (newMediaNotificationTypes.Contains(notification.NotificationType))
                                {
                                    msgData.Add(Constants.FCM_KEY_MEDIA_ID, notification.GetRequest.TMDB_Id.ToString());
                                    msgData.Add(Constants.FCM_KEY_MEDIA_TYPE, ((int)notification.GetRequest.EntryType).ToString());
                                }
                            }

                            if (notification.Friendship != null)
                                msgData.Add(Constants.FCM_KEY_FRIENDSHIP_ID, notification.FriendshipId.ToString());

                            var msg = new Message
                            {
                                Token = fcmToken.Token,
                                Data = msgData,
                                Notification = new FirebaseAdmin.Messaging.Notification
                                {
                                    Title = notification.Title,
                                    Body = notification.Message
                                },
                                Apns = new ApnsConfig
                                {
                                    Aps = new Aps
                                    {
                                        Badge = profile.Notifications.Count > 0 ? profile.Notifications.Count : null
                                    }
                                },
                                Android = new AndroidConfig
                                {
                                    Priority = Priority.High,
                                    Notification = new AndroidNotification
                                    {
                                        Color = Constants.FCM_KEY_ANDROID_COLOR,
                                        Icon = Constants.FCM_KEY_ANDROID_ICON
                                    }
                                }
                            };

                            try
                            {
                                var msgId = await FirebaseMessaging.DefaultInstance.SendAsync(msg);
                                if (!string.IsNullOrWhiteSpace(msgId))
                                {
                                    notification.Sent = true;
                                    db.Notifications.Update(notification);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, ex.Message);
                            }
                        }

                    }
                }


                await db.SaveChangesAsync(_cancellationToken);
            }
        }        
    }
}
