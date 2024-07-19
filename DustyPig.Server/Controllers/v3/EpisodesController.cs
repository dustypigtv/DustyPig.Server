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
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result<DetailedEpisodeEx>))]
        public async Task<Result<DetailedEpisodeEx>> Details(int id)
        {
            var seriesId = await DB.MediaEntries
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Where(m => m.EntryType == MediaTypes.Episode)
                .Select(m => m.LinkedToId)
                .FirstOrDefaultAsync();

            if (seriesId == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var series = await GetSeriesDetailsAsync(seriesId.Value);
            if (series == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var episode = series.Episodes.First(e => e.Id == id);
            var ret = new DetailedEpisodeEx
            {
                Added = episode.Added,
                ArtworkUrl = episode.ArtworkUrl,
                BifUrl = episode.BifUrl,
                CreditsStartTime = episode.CreditsStartTime,
                Date = episode.Date,
                Description = episode.Description,
                EpisodeNumber = episode.EpisodeNumber,
                Id = episode.Id,
                IntroEndTime = episode.IntroEndTime,
                IntroStartTime = episode.IntroStartTime,
                Length = episode.Length,
                Played = episode.Played,
                SeasonNumber = episode.SeasonNumber,
                SeriesArtworkUrl = series.ArtworkUrl,
                SeriesBackdropUrl = series.BackdropUrl,
                SeriesId = series.Id,
                SeriesTitle = series.Title,
                SRTSubtitles = episode.SRTSubtitles,
                Title = episode.Title,
                TMDB_Id = episode.TMDB_Id,
                UpNext = episode.UpNext,
                VideoUrl = series.CanPlay ? episode.VideoUrl : null,
            };


            return ret;
        }



        /// <summary>
        /// Requires main profile
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
                BifUrl = episodeInfo.BifUrl,
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
                        Language = srt.Language,
                        Name = srt.Name,
                        Url = srt.Url
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
        /// Requires profile
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
               .Include(m => m.ProfileMediaProgress.Where(p => p.ProfileId == UserProfile.Id))
               .Where(m => m.Id == episodeEntry.LinkedToId.Value)
               .Where(m => m.EntryType == MediaTypes.Series)
               .FirstOrDefaultAsync();


            if (seriesEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            seriesEntry.EverPlayed = true;

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
        /// Requires main profile
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
            existingEpisode.BifUrl = episodeInfo.BifUrl;
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
                        Language = srt.Language,
                        Name = srt.Name,
                        Url = srt.Url
                    });

            //Moment of truth!
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> Delete(int id) => DeleteMedia(id);
    }
}
