using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
using FirebaseAdmin.Messaging;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private const int MILLISECONDS_PER_MINUTE = 1000 * 60;

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

#if !DEBUG
            _timer.Change(0, Timeout.Infinite);
#endif

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
                _timer.Change(MILLISECONDS_PER_MINUTE, Timeout.Infinite);
        }


        private async Task DoWorkAsync()
        {
            //Separate functions aren't called more than once, but are separated to make
            //Scoping easier, and smaller-faster progress saves

            await RemoveOldFCMTokensAsync();


            //Now do the notifications
            await SendNotificationsAsync();
        }


        private async Task RemoveOldFCMTokensAsync()
        {
            using var db = new AppDbContext();
            string query = $"DELETE FROM {nameof(db.FCMTokens)} WHERE {nameof(FCMToken.LastSeen)} < '{DateTime.UtcNow.AddMonths(-3):yyyy-MM-dd}'";
            await db.Database.ExecuteSqlRawAsync(query, _cancellationToken);
        }



        

        private async Task SendNotificationsAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {
                var notifications = await db.Notifications
                    .Include(item => item.Profile)
                    .ThenInclude(item => item.FCMTokens)
                    .Include(item => item.MediaEntry)
                    .Include(item => item.GetRequest)
                    .Include(item => item.Friendship)
                    .Where(item => item.Sent == false)
                    .Where(item => item.Seen == false)
                    .OrderBy(item => item.Id)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync(_cancellationToken);

                if (notifications.Count == 0)
                    return;

                var msgs = new List<Message>();
                var dict = new Dictionary<int, Data.Models.Notification>();
                foreach (var notification in notifications)
                {
                    if (!(notification.Sent || notification.Seen))
                    {
                        foreach (var fcmToken in notification.Profile.FCMTokens)
                        {
                            var msgData = new Dictionary<string, string>
                            {
                                { "id", notification.Id.ToString() },
                                { "title", notification.Title },
                                { "message", notification.Message },
                                { "profile_id", notification.ProfileId.ToString() },
                                { "notificaiton_type", ((int)notification.NotificationType).ToString() }
                            };


                            if(notification.MediaEntry != null)
                            {
                                msgData.Add("media_id", notification.MediaEntry.Id.ToString());
                                msgData.Add("media_type", ((int)notification.MediaEntry.EntryType).ToString());
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
                                    msgData.Add("media_id", notification.GetRequest.TMDB_Id.ToString());
                            }
                            
                            if(notification.Friendship != null)
                                msgData.Add("friendship_id", notification.FriendshipId.ToString());

                            var msg = new Message
                            {
                                Token = fcmToken.Token,
                                Data = new Dictionary<string, string>
                                {
                                    { "id", notification.Id.ToString() },
                                    { "profile_id", notification.ProfileId.ToString() },
                                    { "title", notification.Title },
                                    { "message", notification.Message },
                                    { "notificaiton_type", ((int)notification.NotificationType).ToString() },
                                }
                            };


                            msgs.Add(msg);
                            dict.Add(msgs.Count - 1, notification);

                            if (msgs.Count == 500)
                                await SendBatchOfNotificationsAsync(msgs, dict, db);
                        }
                    }
                }

                if (msgs.Count > 0)
                    await SendBatchOfNotificationsAsync(msgs, dict, db);

                if (notifications.Count < CHUNK_SIZE)
                    return;

                start += CHUNK_SIZE;
            }
        }

        private async Task SendBatchOfNotificationsAsync(List<Message> msgs, Dictionary<int, Data.Models.Notification> dict, AppDbContext db)
        {
            if (_cancellationToken.IsCancellationRequested)
                return;

            //Deprecated
            //var response = await FirebaseMessaging.DefaultInstance.SendAllAsync(msgs, _cancellationToken);
            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(msgs, _cancellationToken);

            for (int i = 0; i < response.Responses.Count; i++)
                if (response.Responses[i].IsSuccess)
                    dict[i].Sent = true;

            msgs.Clear();
            dict.Clear();
            await db.SaveChangesAsync(_cancellationToken);
        }
    }
}
