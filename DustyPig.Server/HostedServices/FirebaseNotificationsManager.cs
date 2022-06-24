using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
using FirebaseAdmin.Messaging;
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

            //First, create all any needed notifications
            await RemoveOldDeviceTokensAsync();
            await CreateNotificationsForMovieRequestsAsync();
            await CreateNotificationsForSeriesRequestsAsync();
            await CreateNotificiationsForNewEpisodesAsync();
            await CreateNotificationsForOverrideRequestsAsync();
            await CreateNotificationsForFriendshipsAsync();


            //Now do the notifications
            await SendNotificationsAsync();
        }


        private async Task RemoveOldDeviceTokensAsync()
        {
            using var db = new AppDbContext();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DeviceTokens WHERE LastSeen < {DateTime.UtcNow.AddMonths(-3):yyyy-MM-dd}", _cancellationToken);
        }

        private async Task CreateNotificationsForMovieRequestsAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {
                //Notify of movie requests fulfilled
                var moviesLst = await db.MediaEntries
                    .Include(Item => Item.Library)
                    .ThenInclude(item => item.ProfileLibraryShares)
                    .Where(item => item.EntryType == API.v3.Models.MediaTypes.Movie)
                    .Where(item => item.TMDB_Id > 0)
                    .Where(item => item.NotificationsCreated == false)
                    .OrderBy(item => item.Id)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync(_cancellationToken);

                foreach (var movie in moviesLst)
                {
                    var profileIds = movie.Library.ProfileLibraryShares.Select(item => item.ProfileId).ToList();
                    var getRequests = await db.GetRequests
                        .Include(item => item.Profile)
                        .Where(item => item.TMDB_Id == movie.TMDB_Id)
                        .Where(item => item.EntryType == API.v3.Models.TMDB_MediaTypes.Movie)
                        .Where(item => profileIds.Contains(item.ProfileId))
                        .Where(item => item.Status == API.v3.Models.RequestStatus.Pending)
                        .ToListAsync(_cancellationToken);


                    foreach (var request in getRequests)
                    {
                        request.Status = API.v3.Models.RequestStatus.Fufilled;
                        request.Timestamp = DateTime.UtcNow;

                        db.Notifications.Add(new Data.Models.Notification
                        {
                            GetRequestId = request.Id,
                            MediaEntryId = movie.Id,
                            Message = movie.Title + " is now available!",
                            NotificationType = NotificationType.Media,
                            ProfileId = request.ProfileId,
                            Timestamp = DateTime.UtcNow,
                            Title = "New movie Available"
                        });
                    }

                    movie.NotificationsCreated = true;
                }

                await db.SaveChangesAsync(_cancellationToken);

                if (moviesLst.Count < CHUNK_SIZE)
                    return;

                start += CHUNK_SIZE;
            }
        }

        private async Task CreateNotificationsForSeriesRequestsAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {

                //Notify of series requests fulfilled (Season 1 Episode 1 must exist)
                var seriesLst = await db.MediaEntries
                    .Include(item => item.LinkedTo)
                    .ThenInclude(item => item.Library)
                    .ThenInclude(item => item.ProfileLibraryShares)

                    .Where(item => item.EntryType == API.v3.Models.MediaTypes.Episode)
                    .Where(item => item.Season == 1)
                    .Where(item => item.Episode == 1)
                    .Where(item => item.LinkedTo.EntryType == API.v3.Models.MediaTypes.Series)
                    .Where(item => item.LinkedTo.TMDB_Id > 0)
                    .Where(item => item.LinkedTo.NotificationsCreated == false)

                    .OrderBy(item => item.Id)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .Select(Item => Item.LinkedTo)
                    .ToListAsync(_cancellationToken);

                if (seriesLst.Count == 0)
                    return;

                foreach (var series in seriesLst)
                {
                    var profileIds = series.Library.ProfileLibraryShares.Select(item => item.ProfileId).ToList();
                    var getRequests = await db.GetRequests
                        .Include(item => item.Profile)
                        .Where(item => item.TMDB_Id == series.TMDB_Id)
                        .Where(item => item.EntryType == API.v3.Models.TMDB_MediaTypes.Series)
                        .Where(item => profileIds.Contains(item.ProfileId))
                        .Where(item => item.Status == API.v3.Models.RequestStatus.Pending)
                        .ToListAsync(_cancellationToken);

                    foreach (var request in getRequests)
                    {
                        request.Status = API.v3.Models.RequestStatus.Fufilled;
                        request.Timestamp = DateTime.UtcNow;

                        db.Notifications.Add(new Data.Models.Notification
                        {
                            GetRequestId = request.Id,
                            MediaEntryId = series.Id,
                            Message = $"{series.Title} is now available",
                            NotificationType = NotificationType.Media,
                            ProfileId = request.ProfileId,
                            Timestamp = DateTime.UtcNow,
                            Title = "New series available"
                        });
                    }

                    series.NotificationsCreated = true;
                }

                await db.SaveChangesAsync(_cancellationToken);

                if (seriesLst.Count < CHUNK_SIZE)
                    return;

                start += CHUNK_SIZE;
            }
        }

        private async Task CreateNotificiationsForNewEpisodesAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {
                //Notify subscribers of new episodes
                var episodeLst = await db.MediaEntries
                    .Include(item => item.LinkedTo)
                    .ThenInclude(item => item.Library)
                    .ThenInclude(item => item.ProfileLibraryShares)

                    .Include(item => item.LinkedTo)
                    .ThenInclude(item => item.Subscriptions)

                    .Where(item => item.EntryType == API.v3.Models.MediaTypes.Episode)
                    .Where(item => item.NotificationsCreated == false)
                    .Where(item => item.LinkedToId.HasValue)
                    .Where(item => item.LinkedTo.EntryType == API.v3.Models.MediaTypes.Series)

                    .OrderBy(item => item.Id)
                    .Skip(start)
                    .Take(CHUNK_SIZE)

                    .OrderBy(item => item.LinkedTo.SortTitle)
                    .ThenBy(item => item.Xid)

                    .ToListAsync(_cancellationToken);

                foreach (var episode in episodeLst)
                {
                    var series = episode.LinkedTo;

                    foreach (var subscription in series.Subscriptions)
                    {
                        if (series.Library.ProfileLibraryShares.Any(item => item.ProfileId == subscription.ProfileId))
                        {
                            db.Notifications.Add(new Data.Models.Notification
                            {
                                MediaEntryId = episode.Id,
                                Message = $"{series.Title} - s{episode.Season:00}e{episode.Episode:00} is now available",
                                NotificationType = NotificationType.Media,
                                ProfileId = subscription.ProfileId,
                                Timestamp = DateTime.UtcNow,
                                Title = "New episode available"
                            });
                        }
                    }

                    episode.NotificationsCreated = true;
                }

                await db.SaveChangesAsync(_cancellationToken);

                if (episodeLst.Count < CHUNK_SIZE)
                    return;

                start += CHUNK_SIZE;
            }
        }

        private async Task CreateNotificationsForOverrideRequestsAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {
                //Check for override requests and create notifications
                var overrideRequests = await db.OverrideRequests
                    .Include(item => item.Profile)
                    .ThenInclude(item => item.Account)
                    .ThenInclude(item => item.Profiles)
                    .Include(item => item.MediaEntry)
                    .Where(item => item.NotificationCreated == false)
                    .OrderBy(item => item.Id)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync();

                foreach (var overrideRequest in overrideRequests)
                {
                    var mainProfile = overrideRequest.Profile.Account.Profiles.Single(item => item.IsMain);

                    if (overrideRequest.Status == API.v3.Models.RequestStatus.Requested)
                    {
                        db.Notifications.Add(new Data.Models.Notification
                        {
                            OverrideRequestId = overrideRequest.Id,
                            MediaEntryId = overrideRequest.MediaEntryId,
                            Message = $"{overrideRequest.Profile.Name} is requesting access to \"{overrideRequest.MediaEntry.FormattedTitle()}\"",
                            NotificationType = NotificationType.OverrideRequest,
                            ProfileId = mainProfile.Id,
                            Title = "Access Request",
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if (overrideRequest.Status == API.v3.Models.RequestStatus.Fufilled)
                    {
                        db.Notifications.Add(new Data.Models.Notification
                        {
                            OverrideRequestId = overrideRequest.Id,
                            MediaEntryId = overrideRequest.MediaEntryId,
                            Message = $"{mainProfile.Name} has granted access to \"{overrideRequest.MediaEntry.FormattedTitle()}\"",
                            NotificationType = NotificationType.OverrideRequest,
                            ProfileId = overrideRequest.ProfileId,
                            Title = "Access Granted",
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    if (overrideRequest.Status == API.v3.Models.RequestStatus.Denied)
                    {
                        db.Notifications.Add(new Data.Models.Notification
                        {
                            OverrideRequestId = overrideRequest.Id,
                            MediaEntryId = overrideRequest.MediaEntryId,
                            Message = $"{mainProfile.Name} has denied access to \"{overrideRequest.MediaEntry.FormattedTitle()}\"",
                            NotificationType = NotificationType.OverrideRequest,
                            ProfileId = overrideRequest.ProfileId,
                            Title = "Access Denied",
                            Timestamp = DateTime.UtcNow
                        });
                    }


                    overrideRequest.NotificationCreated = true;
                }

                await db.SaveChangesAsync(_cancellationToken);

                if (overrideRequests.Count < CHUNK_SIZE)
                    return;

                start += CHUNK_SIZE;
            }
        }

        private async Task CreateNotificationsForFriendshipsAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {
                //Check for friendships and create notifications
                var friendships = await db.Friendships
                    .Include(item => item.Account1)
                    .ThenInclude(item => item.Profiles)
                    .Include(item => item.Account2)
                    .ThenInclude(item => item.Profiles)
                    .Where(item => item.NotificationCreated == false)
                    .OrderBy(item => item.Id)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync();

                foreach (var friendship in friendships)
                {
                    if (friendship.Accepted)
                    {
                        //Inform Account1 of new friend
                        db.Notifications.Add(new Data.Models.Notification
                        {
                            Friendship = friendship,
                            Title = "Friend Request",
                            Message = $"{friendship.Account1.Profiles.Single(item => item.IsMain).Name} has sent you a friend request",
                            NotificationType = NotificationType.Friend,
                            ProfileId = friendship.Account2.Profiles.Single(item => item.IsMain).Id,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        //Inform Account2 of friend request
                        db.Notifications.Add(new Data.Models.Notification
                        {
                            FriendshipId = friendship.Id,
                            Title = "You have a new friend!",
                            Message = $"{friendship.Account2.Profiles.Single(item => item.IsMain).Name} has accepted your friend request",
                            NotificationType = NotificationType.Friend,
                            ProfileId = friendship.Account1.Profiles.Single(item => item.IsMain).Id,
                            Timestamp = DateTime.UtcNow
                        });

                    }

                    friendship.NotificationCreated = true;
                }

                await db.SaveChangesAsync(_cancellationToken);

                if (friendships.Count < CHUNK_SIZE)
                    return;

                start += CHUNK_SIZE;
            }
        }



        private async Task SendNotificationsAsync()
        {
            using var db = new AppDbContext();

            int start = 0;
            while (true)
            {
                var notifications = await db.Notifications
                    .Include(item => item.Profile)
                    .ThenInclude(item => item.DeviceTokens)
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
                        foreach (var deviceToken in notification.Profile.DeviceTokens)
                        {

                            var msg = new Message
                            {
                                Token = deviceToken.Token,
                                Notification = new FirebaseAdmin.Messaging.Notification
                                {
                                    Title = notification.Title,
                                    Body = notification.Message
                                }
                            };

                            msg.Data = new Dictionary<string, string> { { "deeplink", DeepLinks.Create(notification) } };

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

            var response = await FirebaseMessaging.DefaultInstance.SendAllAsync(msgs, _cancellationToken);
            for (int i = 0; i < response.Responses.Count; i++)
                if (response.Responses[i].IsSuccess)
                    dict[i].Sent = true;

            msgs.Clear();
            dict.Clear();
            await db.SaveChangesAsync(_cancellationToken);
        }
    }
}
