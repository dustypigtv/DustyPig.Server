using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(EpisodesController))]
    public class EpisodesController : _MediaControllerBase
    {
        public EpisodesController(AppDbContext db) : base(db)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<DetailedEpisode>))]
        public async Task<Result<DetailedEpisode>> Details(int id)
        {

            var episode = await DB.MediaEntries
                .AsNoTracking()
                .Include(m => m.Subtitles)
                .Where(m => m.Id == id)
                .Where(m => m.EntryType == MediaTypes.Episode)
                .FirstOrDefaultAsync();

            if (episode == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var series = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .ThenInclude(item => item.Account)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(item2 => item2.Friendship.Account1Id == UserAccount.Id || item2.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(item2 => item2.Friendship.Account1Id == UserAccount.Id || item2.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.Id == UserProfile.Id))

                .Include(item => item.TitleOverrides.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.WatchlistItems.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.ProfileMediaProgress.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Where(item => item.Id == episode.LinkedToId.Value)
                .Where(item => item.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (series == null)
                return CommonResponses.ValueNotFound(nameof(id));

            if (series.Library.AccountId != UserAccount.Id)
                if (!series.Library.FriendLibraryShares.Any())
                    if (!series.TitleOverrides.Any())
                        if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                            return CommonResponses.ValueNotFound(nameof(id));


            bool playable = (UserProfile.IsMain)
                || series.TitleOverrides.Any(item => item.State == OverrideState.Allow)
                ||
                (
                    !series.TitleOverrides.Any(item => item.State == OverrideState.Block)
                    &&
                    (
                        series.Library.ProfileLibraryShares.Any()
                        && UserProfile.MaxTVRating >= (series.TVRating ?? TVRatings.NotRated)
                    )
                );


            //Build the response
            var ret = new DetailedEpisode
            {
                ArtworkUrl = episode.ArtworkUrl,
                ArtworkSize = episode.ArtworkSize,
                BifUrl = episode.BifUrl,
                BifSize = episode.BifSize,
                CreditsStartTime = episode.CreditsStartTime,
                Date = episode.Date.Value,
                Description = episode.Description,
                EpisodeNumber = (ushort)episode.Episode.Value,
                Id = episode.Id,
                IntroEndTime = episode.IntroEndTime,
                IntroStartTime = episode.IntroStartTime,
                Length = episode.Length.Value,
                SeasonNumber = (ushort)episode.Season.Value,
                SeriesId = series.Id,
                SeriesTitle = series.Title,
                Title = episode.Title,
                TMDB_Id = episode.TMDB_Id,
                VideoUrl = playable ? episode.VideoUrl : null,
                VideoSize = episode.VideoSize
            };

            ret.SRTSubtitles = episode
                .Subtitles
                .Select(s => s.ToSRTSubtitle())
                .ToList();



            var pmp = series.ProfileMediaProgress.FirstOrDefault();
            if (pmp != null)
            {
                var allEpisodes = await DB.MediaEntries
                    .AsNoTracking()
                    .Include(item => item.Subtitles)
                    .Where(item => item.LinkedToId == series.Id)
                    .OrderBy(item => item.Xid)
                    .ToListAsync();

                if (allEpisodes.Count > 0)
                {
                    var dbEp = allEpisodes.FirstOrDefault(item => item.Xid == pmp.Xid);
                    if (dbEp != null)
                    {
                        if (dbEp.Xid == episode.Xid)
                        {
                            if (pmp.Played < (episode.CreditsStartTime ?? episode.Length.Value - 30))
                            {
                                //Partially played episode
                                ret.UpNext = true;
                                ret.Played = pmp.Played;
                            }
                            else
                            {
                                //Fully played episode, find the next one
                                var nextDBEp = allEpisodes.FirstOrDefault(item => item.Xid > dbEp.Xid);
                                if (nextDBEp == null)
                                {
                                    //Progress was on last episode
                                    ret.UpNext = true;
                                    ret.Played = pmp.Played;
                                }
                            }
                        }
                        else
                        {
                            var prev = allEpisodes.LastOrDefault(item => item.Xid < episode.Xid);
                            if (prev != null && prev.Xid == pmp.Xid)
                            {
                                if (pmp.Played >= (prev.CreditsStartTime ?? prev.Length.Value - 30))
                                {
                                    ret.UpNext = true;
                                    ret.Played = 0;
                                }
                            }
                        }
                    }
                }
            }


            return ret;
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<int>))]
        public async Task<Result<int>> Create(CreateEpisode episodeInfo)
        {
            // ***** Tons of validation *****
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            //Make sure the series is owned
            var ownedSeries = await DB.MediaEntries
                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares)
                .Include(item => item.Subscriptions)
                .Where(item => item.Id == episodeInfo.SeriesId)
                .SingleOrDefaultAsync();

            if (ownedSeries == null)
                return CommonResponses.ValueNotFound(nameof(episodeInfo.SeriesId));

            if (ownedSeries.Library.AccountId != UserAccount.Id)
                return CommonResponses.ValueNotFound(nameof(episodeInfo.SeriesId));


            // ***** Ok at this point the mediaInfo has all required data, build the new entry *****
            var newItem = new MediaEntry
            {
                Added = DateTime.UtcNow,
                ArtworkUrl = episodeInfo.ArtworkUrl,
                ArtworkSize = episodeInfo.ArtworkSize,
                BifUrl = episodeInfo.BifUrl,
                BifSize = episodeInfo.BifSize,
                CreditsStartTime = episodeInfo.CreditsStartTime,
                Date = episodeInfo.Date,
                Description = episodeInfo.Description,
                EntryType = MediaTypes.Episode,
                Episode = episodeInfo.EpisodeNumber,
                IntroEndTime = episodeInfo.IntroEndTime,
                IntroStartTime = episodeInfo.IntroStartTime,
                Length = episodeInfo.Length,
                LibraryId = ownedSeries.LibraryId,
                LinkedToId = episodeInfo.SeriesId,
                TVRating = ownedSeries.TVRating,
                Season = episodeInfo.SeasonNumber,
                Title = episodeInfo.Title,
                TMDB_Id = episodeInfo.TMDB_Id,
                VideoUrl = episodeInfo.VideoUrl,
                VideoSize = episodeInfo.VideoSize
            };

            newItem.Hash = newItem.ComputeHash();
            newItem.Xid = newItem.ComputeXid();

            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return $"An episode already exists with the following parameters: {nameof(ownedSeries.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}";


            //Add the new item
            DB.MediaEntries.Add(newItem);


            //Add Subtitles
            if (episodeInfo.SRTSubtitles != null)
                foreach (var srt in episodeInfo.SRTSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntry = newItem,
                        Name = srt.Name,
                        Url = srt.Url,
                        FileSize = srt.FileSize
                    });

            //Notifications
            var notifiedProfiles = new List<int>();
            if (ownedSeries.TMDB_Id > 0 && episodeInfo.SeasonNumber == 1 && episodeInfo.EpisodeNumber == 1)
            {
                var getRequest = await DB.GetRequests
                    .Include(item => item.NotificationSubscriptions)
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                    .Where(item => item.EntryType == TMDB_MediaTypes.Series)
                    .Where(item => item.Status != RequestStatus.Fulfilled)
                    .FirstOrDefaultAsync();

                if (getRequest != null)
                {
                    getRequest.Status = RequestStatus.Fulfilled;

                    foreach (var sub in getRequest.NotificationSubscriptions)
                    {
                        DB.Notifications.Add(new Data.Models.Notification
                        {
                            MediaEntry = newItem,
                            Message = "\"" + ownedSeries.Title + "\" is now availble!",
                            NotificationType = NotificationTypes.NewMediaFulfilled,
                            ProfileId = sub.ProfileId,
                            Timestamp = DateTime.UtcNow,
                            Title = "Your Series Is Now Available"
                        });

                        DB.GetRequestSubscriptions.Remove(sub);
                        notifiedProfiles.Add(sub.ProfileId);
                    }
                }
            }

            foreach (var subscription in ownedSeries.Subscriptions)
                if (ownedSeries.Library.ProfileLibraryShares.Any(item => item.ProfileId == subscription.ProfileId))
                    if (!notifiedProfiles.Contains(subscription.ProfileId))
                        DB.Notifications.Add(new Data.Models.Notification
                        {
                            MediaEntry = newItem,
                            Message = $"{ownedSeries.Title} - s{episodeInfo.SeasonNumber:00}e{episodeInfo.EpisodeNumber:00} is now available",
                            NotificationType = NotificationTypes.NewMediaAvailable,
                            ProfileId = subscription.ProfileId,
                            Timestamp = DateTime.UtcNow,
                            Title = "New Episode Available"
                        });


            //Updating the Added field of the series MediaEntry allows the RecentlyAdded query to run far more efficently
            ownedSeries.Added = newItem.Added;


            // Add to subscribed playlists
            var apsSubs = await DB.AutoPlaylistSeries
                .AsNoTracking()
                .Include(e => e.Playlist)
                .ThenInclude(p => p.PlaylistItems)
                .Include(e => e.Playlist)
                .ThenInclude(e => e.Profile)
                .Where(e => e.MediaEntryId == episodeInfo.SeriesId)
                .ToListAsync();

            //This will be slow, but it's admin-facing, and slow here is better than slow user-facing
            var playlistsToUpdate = new List<int>();
            foreach (var aps in apsSubs)
            {
                var seriesPlayable = await DB.MediaEntries
                    .Where(m => m.EntryType == MediaTypes.Series)

                    .Where(m =>
                        m.TitleOverrides
                            .Where(t => t.ProfileId == aps.Playlist.ProfileId)
                            .Where(t => t.State == OverrideState.Allow)
                            .Any()
                        ||
                        (
                            aps.Playlist.Profile.IsMain
                            &&
                            (
                                m.Library.AccountId == aps.Playlist.Profile.AccountId
                                ||
                                (
                                    m.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == aps.Playlist.Profile.AccountId || f.Friendship.Account2Id == aps.Playlist.Profile.AccountId)
                                    && !m.TitleOverrides
                                        .Where(t => t.ProfileId == aps.Playlist.ProfileId)
                                        .Where(t => t.State == OverrideState.Block)
                                        .Any()
                                )
                            )
                        )
                        ||
                        (
                            m.Library.ProfileLibraryShares.Any(p => p.ProfileId == aps.Playlist.ProfileId)
                            && aps.Playlist.Profile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                            && !m.TitleOverrides
                                .Where(t => t.ProfileId == aps.Playlist.ProfileId)
                                .Where(t => t.State == OverrideState.Block)
                                .Any()
                        )
                    )
                    .AnyAsync();

                if (seriesPlayable)
                {
                    DB.PlaylistItems.Add(new Data.Models.PlaylistItem
                    {
                        Index = aps.Playlist.PlaylistItems.Count > 0 ? aps.Playlist.PlaylistItems.Max(p => p.Index) + 1 : 0,
                        MediaEntry = newItem,
                        PlaylistId = aps.PlaylistId
                    });
                    playlistsToUpdate.Add(aps.PlaylistId);
                }
            }



            //Moment of truth!
            await DB.SaveChangesAsync();

            await HostedServices.ArtworkUpdater.SetNeedsUpdateAsync(playlistsToUpdate);

            return newItem.Id;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> MarkWatched(int id)
        {
            var episodeEntry = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .FirstOrDefaultAsync();

            if (episodeEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            double seconds = episodeEntry.Length ?? 0;

            var seriesEntry = await DB.TopLevelWatchableMediaByProfileQuery(UserProfile)
               .AsNoTracking()
               .Include(m => m.ProfileMediaProgress.Where(p => p.ProfileId == UserProfile.Id))
               .Where(m => m.Id == episodeEntry.LinkedToId.Value)
               .Where(m => m.EntryType == MediaTypes.Series)
               .FirstOrDefaultAsync();


            if (seriesEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var existingProgress = seriesEntry.ProfileMediaProgress.FirstOrDefault();
            if (existingProgress == null)
            {
                //Add
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = seriesEntry.Id,
                    ProfileId = UserProfile.Id,
                    Played = seconds,
                    Timestamp = DateTime.UtcNow,
                    Xid = episodeEntry.Xid
                });
            }
            else
            {
                //Update
                existingProgress.Played = Math.Max(0, seconds);
                existingProgress.Timestamp = DateTime.UtcNow;
                existingProgress.Xid = episodeEntry.Xid;
                DB.ProfileMediaProgresses.Update(existingProgress);
            }

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }




        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Update(UpdateEpisode episodeInfo)
        {
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            //Update
            var existingEpisode = await DB.MediaEntries
                .Include(item => item.LinkedTo)
                .ThenInclude(item => item.Library)
                .Where(item => item.Id == episodeInfo.Id)
                .SingleOrDefaultAsync();

            if (existingEpisode == null)
                return CommonResponses.ValueNotFound(nameof(episodeInfo.Id));

            if (existingEpisode.Library.AccountId != UserAccount.Id)
                return CommonResponses.ValueNotFound(nameof(episodeInfo.Id));


            //Don't update Added or EntryType

            existingEpisode.ArtworkUrl = episodeInfo.ArtworkUrl;
            existingEpisode.ArtworkSize = episodeInfo.ArtworkSize;
            existingEpisode.BifUrl = episodeInfo.BifUrl;
            existingEpisode.BifSize = episodeInfo.BifSize;
            existingEpisode.CreditsStartTime = episodeInfo.CreditsStartTime;
            existingEpisode.Date = episodeInfo.Date;
            existingEpisode.Description = episodeInfo.Description;
            existingEpisode.Episode = episodeInfo.EpisodeNumber;
            existingEpisode.IntroEndTime = episodeInfo.IntroEndTime;
            existingEpisode.IntroStartTime = episodeInfo.IntroStartTime;
            existingEpisode.Length = episodeInfo.Length;
            existingEpisode.Season = episodeInfo.SeasonNumber;
            existingEpisode.Title = episodeInfo.Title;
            existingEpisode.TMDB_Id = episodeInfo.TMDB_Id;
            existingEpisode.VideoUrl = episodeInfo.VideoUrl;
            existingEpisode.VideoSize = episodeInfo.VideoSize;

            existingEpisode.Hash = existingEpisode.ComputeHash();
            existingEpisode.Xid = existingEpisode.ComputeXid();

            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id != existingEpisode.Id)
                .Where(item => item.LibraryId == existingEpisode.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .Where(item => item.TMDB_Id == existingEpisode.TMDB_Id)
                .Where(item => item.Hash == existingEpisode.Hash)
                .AnyAsync();

            if (existingItem)
                return $"An episode already exists with the following parameters: {nameof(existingEpisode.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}";



            //Redo Subtitles
            var subLst = await DB.Subtitles
                .Where(item => item.MediaEntryId == existingEpisode.Id)
                .ToListAsync();
            subLst.ForEach(item => DB.Subtitles.Remove(item));
            if (episodeInfo.SRTSubtitles != null)
                foreach (var srt in episodeInfo.SRTSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntryId = existingEpisode.Id,
                        Name = srt.Name,
                        Url = srt.Url,
                        FileSize = srt.FileSize
                    });

            //Moment of truth!
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> Delete(int id) => DeleteMedia(id);
    }
}
