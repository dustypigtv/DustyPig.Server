using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(EpisodesController))]
    public class EpisodesController : _MediaControllerBase
    {
        public EpisodesController(AppDbContext db, TMDBClient tmdbClient) : base(db, tmdbClient)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ResponseWrapper<DetailedEpisode>> Details(int id)
        {
            var q =
                from qep in DB.MediaEntries
                join qseries in DB.MediaEntries on qep.LinkedToId equals qseries.Id
                join qseriesEp in DB.MediaEntries on qseries.Id equals qseriesEp.LinkedToId
                join qlib in DB.Libraries on qseries.LibraryId equals qlib.Id

                join qsub in DB.Subtitles on qep.Id equals qsub.MediaEntryId into qsub_lj
                from qsub in qsub_lj.DefaultIfEmpty()

                join qfls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on qlib.Id equals qfls into qfls_lj
                from qfls in qfls_lj.DefaultIfEmpty()


                join qpls in DB.ProfileLibraryShares
                    on new { LibraryId = qlib.Id, ProfileId = UserProfile.Id }
                    equals new { qpls.LibraryId, qpls.ProfileId }
                    into qpls_lj
                from qpls in qpls_lj.DefaultIfEmpty()


                join qovrride in DB.TitleOverrides
                    on new { MediaEntryId = qseries.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { qovrride.MediaEntryId, qovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(qovrride.State) }
                    into qovrride_lj
                from qovrride in qovrride_lj.DefaultIfEmpty()

                join qpmp in DB.ProfileMediaProgresses
                    on new { MediaEntryId = qseries.Id, ProfileId = UserProfile.Id }
                    equals new { qpmp.MediaEntryId, qpmp.ProfileId }
                    into qpmp_lj
                from qpmp in qpmp_lj.DefaultIfEmpty()


                where

                    qep.Id == id

                    //Allow to play filters
                    && qep.EntryType == MediaTypes.Episode
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                qlib.AccountId == UserAccount.Id
                                ||
                                (
                                    qfls.HasValue
                                    && qovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            qpls != null
                            && qovrride.State != OverrideState.Block
                            && qseries.TVRating <= UserProfile.MaxTVRating
                        )
                        || qovrride.State == OverrideState.Allow
                    )



                select new
                {
                    qep,
                    qseries,
                    qseriesEp,
                    qpmp,
                    qsub
                };

            var response = await q.AsNoTracking().ToListAsync();

            if (response.Count == 0)
                return CommonResponses.NotFound<DetailedEpisode>(nameof(id));

            var episode = response.First().qep;
            var series = response.First().qseries;
            var pmp = response.First().qpmp;
            var allEpisodes = response.Select(item => item.qseriesEp).OrderBy(item => item.Xid).ToList();


            //Build the response
            var ret = new DetailedEpisode
            {
                ArtworkUrl = episode.ArtworkUrl,
                BifUrl = episode.BifUrl,
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
                Title = episode.Title,
                TMDB_Id = episode.TMDB_Id,
                VideoUrl = episode.VideoUrl
            };


            ret.ExternalSubtitles = response
                .Select(item => item.qsub)
                .Where(item => item != null)
                .Where(item => item.MediaEntryId == id)
                .Select(item => item.ToExternalSubtitle())
                .ToList();

            if (pmp != null)
            {
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


            return new ResponseWrapper<DetailedEpisode>(ret);
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper<SimpleValue<int>>> Create(CreateEpisode episodeInfo)
        {
            // ***** Tons of validation *****
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }

            //Make sure the series is owned
            var ownedSeries = await DB.MediaEntries
                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares)
                .Include(item => item.Subscriptions)
                .Where(item => item.Id == episodeInfo.SeriesId)
                .SingleOrDefaultAsync();

            if (ownedSeries == null)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(episodeInfo.SeriesId));

            if (ownedSeries.Library.AccountId != UserAccount.Id)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(episodeInfo.SeriesId));


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
                VideoUrl = episodeInfo.VideoUrl
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
                return new ResponseWrapper<SimpleValue<int>>($"An episode already exists with the following parameters: {nameof(ownedSeries.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}");


            //Add the new item
            DB.MediaEntries.Add(newItem);


            //Add Subtitles
            if (episodeInfo.ExternalSubtitles != null)
                foreach (var srt in episodeInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntry = newItem,
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
                            NotificationType = NotificationType.GetRequest,
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
                            NotificationType = NotificationType.Media,
                            ProfileId = subscription.ProfileId,
                            Timestamp = DateTime.UtcNow,
                            Title = "New Episode Available"
                        });


            //Updating the Added field of the series MediaEntry allows the RecentlyAdded query to run far more efficently
            ownedSeries.Added = newItem.Added;

            //Moment of truth!
            await DB.SaveChangesAsync();

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int>(newItem.Id));
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Update(UpdateEpisode episodeInfo)
        {
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }


            //Update
            var existingEpisode = await DB.MediaEntries
                .Include(item => item.LinkedTo)
                .ThenInclude(item => item.Library)
                .Where(item => item.Id == episodeInfo.Id)
                .SingleOrDefaultAsync();

            if (existingEpisode == null)
                return CommonResponses.NotFound(nameof(episodeInfo.Id));

            if (existingEpisode.Library.AccountId != UserAccount.Id)
                return CommonResponses.NotFound(nameof(episodeInfo.Id));


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
                return new ResponseWrapper($"An episode already exists with the following parameters: {nameof(existingEpisode.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}");



            //Redo Subtitles
            var subLst = await DB.Subtitles
                .Where(item => item.MediaEntryId == existingEpisode.Id)
                .ToListAsync();
            subLst.ForEach(item => DB.Subtitles.Remove(item));
            if (episodeInfo.ExternalSubtitles != null)
                foreach (var srt in episodeInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntryId = existingEpisode.Id,
                        Name = srt.Name,
                        Url = srt.Url
                    });

            //Moment of truth!
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        public Task<ResponseWrapper> Delete(int id) => DeleteMedia(id);
    }
}
