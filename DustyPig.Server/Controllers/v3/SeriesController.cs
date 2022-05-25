using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
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
    [ExceptionLogger(typeof(SeriesController))]
    public class SeriesController : _MediaControllerBase
    {
        public SeriesController(AppDbContext db, TMDBClient tmdbClient) : base(db, tmdbClient)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 series based on start position and sort order</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> List(ListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var seriesQ =
                from mediaEntry in DB.SeriesPlayableByProfile(UserAccount, UserProfile)
                select mediaEntry;

            var sortedQ = ApplySortOrder(seriesQ, request.Sort);

            var series = await sortedQ
                .AsNoTracking()
                .Skip(request.Start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return series.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Returns the next 100 series based on start position and sort order. Designed for admin tools, will return all series owned by the account</remarks>
        [HttpGet("{start}")]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> AdminList(int start)
        {
            var movies = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .OrderBy(item => item.SortTitle)
                .Skip(start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return movies.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedSeries>> Details(int id)
        {
            var media = await DB.SeriesSearchableByProfile(UserAccount, UserProfile)
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
                
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                
                .Include(item => item.WatchlistItems)
                .Include(item => item.ProfileMediaProgress)
                
                .Where(item => item.Id == id)
                
                .FirstOrDefaultAsync();

            if(media == null)
                return NotFound();

            bool playable = await DB.SeriesPlayableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();



            //Build the response
            var ret = new DetailedSeries
            {
                ArtworkUrl = media.ArtworkUrl,
                BackdropUrl = media.BackdropUrl,
                CanPlay = playable,
                CanManage = UserProfile.IsMain,
                Cast = media.GetPeople(Roles.Cast),
                Description = media.Description,
                Directors = media.GetPeople(Roles.Director),
                Genres = media.Genres ?? Genres.Unknown,
                Id = id,
                LibraryId = media.LibraryId,
                Producers = media.GetPeople(Roles.Producer),
                Rated = media.Rated ?? Ratings.None,
                Title = media.Title,
                TMDB_Id = media.TMDB_Id,
                Writers = media.GetPeople(Roles.Writer)
            };

            if (playable)
                ret.InWatchlist = media.WatchlistItems.Any(item => item.ProfileId == UserProfile.Id);

            //Get the media owner
            if (media.Library.AccountId == UserAccount.Id)
            {
                ret.Owner = UserAccount.Profiles.Single(item => item.IsMain).Name;
            }
            else
            {
                ret.Owner = media.Library.FriendLibraryShares
                    .Select(item => item.Friendship)
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .First()
                    .GetFriendDisplayNameForAccount(UserAccount.Id);
            }

            var progress = media.ProfileMediaProgress.FirstOrDefault(item => item.ProfileId == UserProfile.Id);

            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();


            foreach (var dbEp in dbEps)
            {
                var ep = new DetailedEpisode
                {
                    ArtworkUrl = dbEp.ArtworkUrl,
                    BifUrl = playable ? dbEp.BifUrl : null,
                    CreditsStartTime = dbEp.CreditsStartTime,
                    Date = dbEp.Date.Value,
                    Description = dbEp.Description,
                    EpisodeNumber = (ushort)dbEp.Episode.Value,
                    ExternalSubtitles = playable ? dbEp.Subtitles.ToExternalSubtitleList() : null,
                    Id = dbEp.Id,
                    IntroEndTime = dbEp.IntroEndTime,
                    IntroStartTime = dbEp.IntroStartTime,
                    Length = dbEp.Length.Value,
                    SeasonNumber = (ushort)dbEp.Season.Value,
                    SeriesId = id,
                    Title = dbEp.Title,
                    TMDB_Id = dbEp.TMDB_Id,
                    VideoUrl = playable ? dbEp.VideoUrl : null
                };

                ret.Episodes.Add(ep);
            }


            if (playable && ret.Episodes.Count > 0)
            {
                if (progress != null)
                {
                    var dbEp = dbEps.FirstOrDefault(item => item.Xid == progress.Xid);
                    if(dbEp != null)
                    {
                        var upNextEp = ret.Episodes.First(item => item.Id == dbEp.Id);
                        if (progress.Played < (upNextEp.CreditsStartTime ?? dbEp.Length.Value - 30))
                        {
                            //Partially played episode
                            upNextEp.UpNext = true;
                            upNextEp.Played = progress.Played;
                        }
                        else
                        {
                            //Fully played episode, find the next one
                            var nextDBEp = dbEps.FirstOrDefault(item => item.Xid > dbEp.Xid);
                            if (nextDBEp == null)
                            {
                                //Progress was on last episode
                                upNextEp.UpNext = true;
                                upNextEp.Played = progress.Played;
                            }
                            else
                            {
                                //Next episode after progress
                                upNextEp = ret.Episodes.First(item => item.Id == nextDBEp.Id);
                                upNextEp.UpNext = true;
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
        /// <remarks>Designed for admin tools, this will return info on any series owned by the account</remarks>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedSeries>> AdminDetails(int id)
        {
            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Include(Item => Item.Library)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return NotFound();

            //Build the response
            var ret = new DetailedSeries
            {
                ArtworkUrl = mediaEntry.ArtworkUrl,
                BackdropUrl = mediaEntry.BackdropUrl,
                Cast = mediaEntry.GetPeople(Roles.Cast),
                Description = mediaEntry.Description,
                Directors = mediaEntry.GetPeople(Roles.Director),
                Genres = mediaEntry.Genres ?? Genres.Unknown,
                Id = id,
                LibraryId = mediaEntry.LibraryId,
                Producers = mediaEntry.GetPeople(Roles.Producer),
                Rated = mediaEntry.Rated ?? Ratings.None,
                Title = mediaEntry.Title,
                TMDB_Id = mediaEntry.TMDB_Id,
                Writers = mediaEntry.GetPeople(Roles.Writer)
            };


            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();


            foreach (var dbEp in dbEps)
            {
                var ep = new DetailedEpisode
                {
                    ArtworkUrl = dbEp.ArtworkUrl,
                    BifUrl = dbEp.BifUrl,
                    CreditsStartTime = dbEp.CreditsStartTime,
                    Date = dbEp.Date.Value,
                    Description = dbEp.Description,
                    EpisodeNumber = (ushort)dbEp.Episode.Value,
                    Id = dbEp.Id,
                    IntroEndTime = dbEp.IntroEndTime,
                    IntroStartTime = dbEp.IntroStartTime,
                    Length = dbEp.Length.Value,
                    SeasonNumber = (ushort)dbEp.Season.Value,
                    SeriesId = id,
                    Title = dbEp.Title,
                    TMDB_Id = dbEp.TMDB_Id,
                    VideoUrl = dbEp.VideoUrl
                };

                ep.ExternalSubtitles = dbEp.Subtitles.ToExternalSubtitleList();

                ret.Episodes.Add(ep);
            }

            //Extra Search Terms
            var allTerms = mediaEntry.MediaSearchBridges.Select(item => item.SearchTerm.Term).ToList();
            var coreTerms = mediaEntry.Title.NormalizedQueryString().Tokenize();
            allTerms.RemoveAll(item => coreTerms.Contains(item));
            ret.ExtraSearchTerms = allTerms;

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
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateSeries seriesInfo)
        {
            // ***** Tons of validation *****
            try { seriesInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }



            //Make sure the library is owned
            var ownedLib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == seriesInfo.LibraryId)
                .AnyAsync();
            if (!ownedLib)
                return NotFound(nameof(seriesInfo.LibraryId));


            // ***** Ok at this point the mediaInfo has all required data, build the new entry *****
            var newItem = new MediaEntry
            {
                Added = DateTime.UtcNow,
                ArtworkUrl = seriesInfo.ArtworkUrl,
                BackdropUrl = seriesInfo.BackdropUrl,
                Description = seriesInfo.Description,
                EntryType = MediaTypes.Series,
                Genres = seriesInfo.Genres,
                LibraryId = seriesInfo.LibraryId,
                Rated = seriesInfo.Rated,
                SortTitle = StringUtils.SortTitle(seriesInfo.Title),
                Title = seriesInfo.Title,
                TMDB_Id = seriesInfo.TMDB_Id
            };

            newItem.Hash = newItem.ComputeHash();


            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return BadRequest($"An series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}");

            //Get popularity
            await UpdatePopularity(newItem);


            //Add the new item
            DB.MediaEntries.Add(newItem);

            //People
            await MediaEntryLogic.UpdatePeople(DB, newItem, seriesInfo.Cast, seriesInfo.Directors, seriesInfo.Producers, seriesInfo.Writers);

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(DB, newItem, GetSearchTerms(newItem, seriesInfo.ExtraSearchTerms));

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
        public async Task<ActionResult> Update(UpdateSeries seriesInfo)
        {
            // ***** Tons of validation *****
            try { seriesInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var existingItem = await DB.MediaEntries
                .Where(item => item.Id == seriesInfo.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (existingItem == null)
                return NotFound();

            //Make sure this item is owned
            var ownedLibs = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Select(item => item.Id)
                .ToListAsync();

            if (!ownedLibs.Contains(existingItem.LibraryId))
                return CommonResponses.ForbidObject("This account does not own this series");

            if (!ownedLibs.Contains(seriesInfo.LibraryId))
                return NotFound(nameof(seriesInfo.LibraryId));




            //Update info
            bool tmdb_changed = existingItem.TMDB_Id != seriesInfo.TMDB_Id;
            bool library_changed = existingItem.LibraryId != seriesInfo.LibraryId;
            bool rated_changed = existingItem.Rated != seriesInfo.Rated;

            existingItem.ArtworkUrl = seriesInfo.ArtworkUrl;
            existingItem.BackdropUrl = seriesInfo.BackdropUrl;
            existingItem.Description = seriesInfo.Description;
            existingItem.Genres = seriesInfo.Genres;
            existingItem.LibraryId = seriesInfo.LibraryId;
            existingItem.Rated = seriesInfo.Rated;
            existingItem.SortTitle = StringUtils.SortTitle(seriesInfo.Title);
            existingItem.Title = seriesInfo.Title;
            existingItem.TMDB_Id = seriesInfo.TMDB_Id;

            existingItem.Hash = existingItem.ComputeHash();


            //Dup check
            var dup = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id != existingItem.Id)
                .Where(item => item.LibraryId == existingItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => item.TMDB_Id == existingItem.TMDB_Id)
                .Where(item => item.Hash == existingItem.Hash)
                .AnyAsync();

            if (dup)
                return BadRequest($"A series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}");

            //Get popularity
            if (tmdb_changed)
                await UpdatePopularity(existingItem);


            //People
            await MediaEntryLogic.UpdatePeople(DB, existingItem, seriesInfo.Cast, seriesInfo.Directors, seriesInfo.Producers, seriesInfo.Writers);

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(DB, existingItem, GetSearchTerms(existingItem, seriesInfo.ExtraSearchTerms));


            //Update library/rated for episodes
            if (library_changed || rated_changed)
            {
                var episodes = await DB.MediaEntries
                    .Where(item => item.LinkedToId == existingItem.Id)
                    .ToListAsync();
                episodes.ForEach(item =>
                {
                    item.LibraryId = existingItem.LibraryId;
                    item.Rated = existingItem.Rated;
                });
            }


            //Moment of truth!
            await DB.SaveChangesAsync();

            return Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! For series, this will also delete all episodes.  For videos, this will delete all linked subtitles. It will also delete all subscriptions, overrides, and watch progess, and remove the media from any watchlists and playlists</remarks>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> Delete(int id) => DeleteMedia(id);


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> ListSubscriptions()
        {
            var subsQ =
                from sub in DB.Subscriptions
                    .Include(item => item.MediaEntry)

                join allowed in DB.SeriesPlayableByProfile(UserAccount, UserProfile) on sub.MediaEntryId equals allowed.Id

                orderby sub.MediaEntry.SortTitle

                select sub;

            var subs = await subsQ
                .AsNoTracking()
                .ToListAsync();

            return subs.Select(item => item.MediaEntry.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Subscribe to notificaitons when new episodes are added to a series</remarks>
        [HttpGet("{id}")]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Subscribe(int id)
        {
            //Get the series
            var series = await DB.SeriesPlayableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Include(item => item.Subscriptions)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (series == null)
                return BadRequest();

            if (!series.Subscriptions.Any(item => item.ProfileId == UserProfile.Id))
            {
                DB.Subscriptions.Add(new Subscription
                {
                    MediaEntryId = id,
                    ProfileId = UserProfile.Id
                });

                await DB.SaveChangesAsync();
            }

            return Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Unsubcribe from notifications when new episodes are added to a series</remarks>
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> Unsubscribe(int id)
        {
            var rec = await DB.Subscriptions
                .Where(item => item.MediaEntryId == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (rec != null)
            {
                DB.Subscriptions.Remove(rec);
                await DB.SaveChangesAsync();
            }

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> RemoveFromContinueWatching(int id)
        {
            if (id <= 0)
                return NotFound();

            var prog = await DB.MediaProgress(UserProfile)
                .Include(item => item.MediaEntry)
                .Where(item => item.MediaEntryId == id)
                .Where(item => item.MediaEntry.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (prog == null)
                return Ok();

            DB.ProfileMediaProgresses.Remove(prog);

            await DB.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> MarkSeriesWatched(int id)
        {
            if (id <= 0)
                return NotFound();

            //Get the max Xid for series
            var maxXidQ =
               from mediaEntry in DB.EpisodesPlayableByProfile(UserAccount, UserProfile)
               group mediaEntry by mediaEntry.LinkedToId into g
               select new
               {
                   SeriesId = g.Key,
                   LastXid = g.Max(item => item.Xid)
               };

            var maxXid = await maxXidQ
                .AsNoTracking()
                .Where(item => item.SeriesId == id)
                .FirstOrDefaultAsync();

            if (maxXid == null)
                return NotFound();

            var lastEpisode = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.EntryType == MediaTypes.Episode)
                .Where(item => item.LinkedToId == id)
                .Where(item => item.Xid == maxXid.LastXid)
                .FirstOrDefaultAsync();

            var prog = await DB.ProfileMediaProgresses
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.MediaEntryId == id)
                .FirstOrDefaultAsync();

            if (prog == null)
            {
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = id,
                    Played = lastEpisode.Length.Value,
                    ProfileId = UserProfile.Id,
                    Timestamp = DateTime.UtcNow,
                    Xid = lastEpisode.Xid
                });
            }
            else
            {
                prog.Xid = lastEpisode.Xid;
                prog.Played = lastEpisode.Length.Value;
                prog.Timestamp = DateTime.UtcNow;
            }


            await DB.SaveChangesAsync();

            return Ok();
        }




        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> RequestAccessOverride(int id) => InternalRequestAccessOverride(id);


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Set access override for a specific series</remarks>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> SetAccessOverride(API.v3.Models.TitleOverride info) => InternalSetAccessOverride(info);

    }
}
