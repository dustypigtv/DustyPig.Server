using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Utilities;
using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices;

public sealed class FirebaseNotificationsManager : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly FirestoreDb _firestoreDb;
    private readonly SafeTimer _timer;
    private readonly ILogger<FirebaseNotificationsManager> _logger;

    private bool _disposed;

    //Only delete old tokens once a day
    //private static DateTime _lastTokenDelete = DateTime.Now.AddDays(-2);

    public FirebaseNotificationsManager(FirestoreDb firestoreDb, IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> dbContextFactory, ILogger<FirebaseNotificationsManager> logger)
    {
        _serviceProvider = serviceProvider;
        _dbContextFactory = dbContextFactory;
        _firestoreDb = firestoreDb;
        _logger = logger;
        _timer = new(TimerTick);
    }



    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Enabled = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryForceStop();
        return Task.CompletedTask;
    }


    private async Task TimerTick(CancellationToken cancellationToken)
    {
        using var db = _dbContextFactory.CreateDbContext();


        try
        {
            var profileIds = await db.Profiles
                .AsNoTracking()
                .Include(p => p.FCMTokens)
                .Include(p => p.Notifications.Where(n => !(n.Sent || n.Seen)))
                .ThenInclude(n => n.MediaEntry)
                .Include(p => p.Notifications.Where(n => !(n.Sent || n.Seen)))
                .ThenInclude(n => n.GetRequest)
                .Include(p => p.Notifications.Where(n => !(n.Sent || n.Seen)))
                .ThenInclude(n => n.Friendship)
                .Where(p => p.Notifications.Any(n => !(n.Sent || n.Seen)))

                .Where(p => p.FCMTokens.Any())

                .Select(_ => _.Id)
                .Distinct()
                .ToListAsync();

            foreach (var profileId in profileIds)
                await SendNotificationsAsync(db, profileId, cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(TimerTick));
        }
    }


    private async Task SendNotificationsAsync(AppDbContext db, int profileId, CancellationToken cancellationToken)
    {
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

            .Where(p => p.Id == profileId)
            .OrderBy(p => p.Notifications.Select(n => n.Timestamp).OrderBy(t => t).First())

            .FirstOrDefaultAsync(cancellationToken);


        // Update Firestore to let mobile listeners know to update their list of notifications
        try
        {
            DocumentReference docRef = _firestoreDb.Collection(Constants.FDB_KEY_ALERTS_COLLECTION).Document(profileId.ToString());

            //The clients only wait for a change to the document, and don't care what the changes are
            //A 1-char key and 8 byte timestamp is small, fast and always updates
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "t", DateTime.UtcNow.Ticks }
            };
            await docRef.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alerts in Firestore");
        }


        //If there are not FCM tokens to send pushes to, consider all notifications sent
        if (profile.FCMTokens.Count == 0)
        {
            foreach (var n in profile.Notifications)
            {
                n.Sent = true;
                db.Notifications.Update(n);
            }
        }
        else
        {
            //Push any notifications to mobile devices
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
                    catch (FirebaseMessagingException ex)
                    {
                        _logger.LogError(ex, ex.Message);

                        //If the FCM Token no longer exists, remove from the database
                        if (ex.ErrorCode == FirebaseAdmin.ErrorCode.NotFound)
                            db.FCMTokens.Remove(fcmToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }

            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _timer.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~FirebaseNotificationsManager()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}