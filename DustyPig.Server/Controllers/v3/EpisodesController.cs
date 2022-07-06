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
        public async Task<ActionResult<DetailedEpisode>> Details(int id)
        {
            //Get the media entry
            var media = await DB.MediaEntriesSearchableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Include(Item => Item.Library)
                .ThenInclude(item => item.Account)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Subtitles)
                .Include(item => item.LinkedTo)
                .ThenInclude(item => item.ProfileMediaProgress)
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Episode)
                .SingleOrDefaultAsync();


            if (media == null)
                return NotFound();
                       
            bool playable = await DB.EpisodesPlayableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();




            //Build the response
            var ret = new DetailedEpisode
            {
                ArtworkUrl = media.ArtworkUrl,
                BifUrl = playable ? media.BifUrl : null,
                CreditsStartTime = media.CreditsStartTime,
                Date = media.Date.Value,
                Description = media.Description,
                EpisodeNumber = (ushort)media.Episode.Value,
                Id = media.Id,
                IntroEndTime = media.IntroEndTime,
                IntroStartTime = media.IntroStartTime,
                Length = media.Length.Value,
                SeasonNumber = (ushort)media.Season.Value,
                SeriesId = media.LinkedToId.Value,
                Title = media.Title,
                TMDB_Id = media.TMDB_Id,
                VideoUrl = playable ? media.VideoUrl : null
            };


            if (playable)
            {
                ret.ExternalSubtitles = media.Subtitles.ToExternalSubtitleList();

                var progress = media.LinkedTo.ProfileMediaProgress.FirstOrDefault(item => item.ProfileId == UserProfile.Id);
                if(progress != null)
                {
                    //Get the episodes
                    var dbEps = await DB.MediaEntries
                        .AsNoTracking()
                        .Where(item => item.LinkedToId == id)
                        .OrderBy(item => item.Xid)
                        .ToListAsync();

                    if(dbEps.Count > 0)
                    {
                        var dbEp = dbEps.FirstOrDefault(item => item.Xid == progress.Xid);
                        if (dbEp != null)
                        {
                            if(dbEp.Xid == media.Xid)
                            {
                                if (progress.Played < (media.CreditsStartTime ?? media.Length.Value - 30))
                                {
                                    //Partially played episode
                                    ret.UpNext = true;
                                    ret.Played = progress.Played;
                                }
                                else
                                {
                                    //Fully played episode, find the next one
                                    var nextDBEp = dbEps.FirstOrDefault(item => item.Xid > dbEp.Xid);
                                    if (nextDBEp == null)
                                    {
                                        //Progress was on last episode
                                        ret.UpNext = true;
                                        ret.Played = progress.Played;
                                    }
                                }
                            }
                            else 
                            {
                                var prev = dbEps.LastOrDefault(item => item.Xid < media.Xid);
                                if(prev != null && prev.Xid == progress.Xid)
                                {
                                    if (progress.Played >= (prev.CreditsStartTime ?? prev.Length.Value - 30))
                                    {
                                        ret.UpNext = true;
                                        ret.Played = 0;
                                    }
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
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateEpisode episodeInfo)
        {
            // ***** Tons of validation *****
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            //Make sure the series is owned
            var ownedSeries = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares)
                .Include(item => item.Subscriptions)
                .Where(item => item.Id == episodeInfo.SeriesId)
                .SingleOrDefaultAsync();

            if (ownedSeries == null)
                return NotFound(nameof(episodeInfo.SeriesId));

            if (ownedSeries.Library.AccountId != UserAccount.Id)
                return NotFound(nameof(episodeInfo.SeriesId));


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
                Rated = ownedSeries.Rated,
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
                return BadRequest($"An episode already exists with the following parameters: {nameof(ownedSeries.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}");


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
                    }
                }
            }

            foreach (var subscription in ownedSeries.Subscriptions)
            {
                if (ownedSeries.Library.ProfileLibraryShares.Any(item => item.ProfileId == subscription.ProfileId))
                {
                    DB.Notifications.Add(new Data.Models.Notification
                    {
                        MediaEntry = newItem,
                        Message = $"{ownedSeries.Title} - s{episodeInfo.SeasonNumber:00}e{episodeInfo.EpisodeNumber:00} is now available",
                        NotificationType = NotificationType.Media,
                        ProfileId = subscription.ProfileId,
                        Timestamp = DateTime.UtcNow,
                        Title = "New Episode Available"
                    });
                }
            }


            //Moment of truth!
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(newItem.Id));
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Update(UpdateEpisode episodeInfo)
        {
            try { episodeInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            //Update
            var existingEpisode = await DB.MediaEntries
                .Include(item => item.LinkedTo)
                .ThenInclude(item => item.Library)
                .Where(item => item.Id == episodeInfo.Id)
                .SingleOrDefaultAsync();

            if (existingEpisode == null)
                return NotFound(nameof(episodeInfo.Id));

            if (existingEpisode.Library.AccountId != UserAccount.Id)
                return NotFound(nameof(episodeInfo.Id));


            existingEpisode.Added = DateTime.UtcNow;
            existingEpisode.ArtworkUrl = episodeInfo.ArtworkUrl;
            existingEpisode.BifUrl = episodeInfo.BifUrl;
            existingEpisode.CreditsStartTime = episodeInfo.CreditsStartTime;
            existingEpisode.Date = episodeInfo.Date;
            existingEpisode.Description = episodeInfo.Description;
            existingEpisode.EntryType = MediaTypes.Episode;
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
                return BadRequest($"An episode already exists with the following parameters: {nameof(existingEpisode.LibraryId)}, {nameof(episodeInfo.TMDB_Id)}, {nameof(episodeInfo.Title)}");



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

            return Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> Delete(int id) => DeleteMedia(id);


    }
}
