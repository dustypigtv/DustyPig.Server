﻿using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        public SeriesController(AppDbContext db, TMDBClient tmdbClient, IMemoryCache memoryCache) : base(db, tmdbClient, memoryCache)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 series based on start position and sort order</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<List<BasicMedia>> List(ListRequest request)
        {
            var seriesQ =
                from mediaEntry in DB.SeriesPlayableByProfile(UserProfile)
                select mediaEntry;

            var sortedQ = ApplySortOrder(seriesQ, request.Sort);

            var series = await sortedQ
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
        public async Task<List<BasicMedia>> AdminList(int start)
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
            //Get the media entry
            var data = await DB.MediaEntries
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
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .SingleOrDefaultAsync();

            if (data == null)
                return NotFound();


            //See if the series is searchable and playable by profile
            bool searchable = await DB.SeriesSearchableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();

            if (!searchable)
                return NotFound();

            bool playable = await DB.SeriesPlayableByProfile(UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();




            //Build the response
            var ret = new DetailedSeries
            {
                ArtworkUrl = data.ArtworkUrl,
                Cast = data.GetPeople(Roles.Cast),
                Description = data.Description,
                Directors = data.GetPeople(Roles.Director),
                Genres = data.Genres.Value,
                Id = id,
                LibraryId = data.LibraryId,
                Producers = data.GetPeople(Roles.Producer),
                Rated = data.Rated ?? Ratings.None,
                Title = data.Title,
                TMDB_Id = data.TMDB_Id,
                Writers = data.GetPeople(Roles.Writer)
            };

            //Get the media owner
            if (data.Library.AccountId == UserAccount.Id)
            {
                ret.Owner = data.Library.Account.Profiles.Single(item => item.IsMain).Name;
            }
            else
            {
                ret.Owner = data.Library.FriendLibraryShares
                    .Select(item => item.Friendship)
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .First()
                    .GetFriendDisplayNameForAccount(UserAccount.Id);
            }


            //Get the episodes
            var epQ =
                from mediaEntry in DB.MediaEntries
                    .AsNoTracking()
                    .Include(item => item.BifServiceCredential)
                    .Include(item => item.VideoServiceCredential)
                    .Include(item => item.Subtitles)
                    .ThenInclude(item => item.ServiceCredential)
                    .Include(item => item.People)
                    .ThenInclude(item => item.Person)

                join progress in MediaProgress on mediaEntry.Id equals progress.MediaEntryId into progressLJ
                from progress in progressLJ.DefaultIfEmpty()

                where mediaEntry.LinkedToId == id

                orderby mediaEntry.Xid

                select new { mediaEntry, progress };

            var dbEps = await epQ.ToListAsync();

            DetailedEpisode upnext = null;
            var lastTS = DateTime.MinValue;

            foreach (var dbEp in dbEps)
            {
                var ep = new DetailedEpisode
                {
                    ArtworkUrl = dbEp.mediaEntry.ArtworkUrl,
                    BifAsset = playable ? Utils.GetAsset(dbEp.mediaEntry.BifServiceCredential, _memoryCache, dbEp.mediaEntry.BifUrl) : null,
                    CreditsStartTime = dbEp.mediaEntry.CreditsStartTime,
                    Date = dbEp.mediaEntry.Date.Value,
                    Description = dbEp.mediaEntry.Description,
                    EpisodeNumber = (ushort)dbEp.mediaEntry.Episode.Value,
                    Id = dbEp.mediaEntry.Id,
                    IntroEndTime = dbEp.mediaEntry.IntroEndTime,
                    IntroStartTime = dbEp.mediaEntry.IntroStartTime,
                    Length = dbEp.mediaEntry.Length.Value,
                    SeasonNumber = (ushort)dbEp.mediaEntry.Season.Value,
                    SeriesId = id,
                    Title = dbEp.mediaEntry.Title,
                    TMDB_Id = dbEp.mediaEntry.TMDB_Id,
                    VideoAsset = playable ? Utils.GetAsset(dbEp.mediaEntry.VideoServiceCredential, _memoryCache, dbEp.mediaEntry.VideoUrl) : null
                };

                if (playable)
                {
                    if (upnext == null)
                        upnext = ep;

                    if (dbEp.progress != null)
                    {
                        if (dbEp.progress.Played > 1000 && dbEp.progress.Played < (dbEp.mediaEntry.CreditsStartTime ?? dbEp.mediaEntry.Length.Value - 30))
                            ep.Played = dbEp.progress.Played;

                        if (dbEp.progress.Timestamp > lastTS)
                        {
                            upnext = ep;
                            lastTS = dbEp.progress.Timestamp;
                        }
                    }

                    if (dbEp.mediaEntry.Subtitles != null && dbEp.mediaEntry.Subtitles.Count > 0)
                    {
                        ep.ExternalSubtitles = new List<ExternalSubtitle>();
                        foreach (var dbSub in dbEp.mediaEntry.Subtitles)
                        {
                            var asset = Utils.GetAsset(dbSub.ServiceCredential, _memoryCache, dbSub.Url);
                            var xs = new ExternalSubtitle { Name = dbSub.Name };
                            if (asset != null)
                            {
                                xs.ExpiresUTC = asset.ExpiresUTC;
                                xs.ServiceCredentialId = asset.ServiceCredentialId;
                                xs.Token = asset.Token;
                                xs.Url = asset.Url;
                            }
                            ep.ExternalSubtitles.Add(xs);
                        }
                    }
                }


                ret.Episodes.Add(ep);
            }

            if (playable && upnext != null)
                foreach (var ep in ret.Episodes)
                    if (ep.Id == upnext.Id)
                        ep.UpNext = true;
                    else
                        ep.Played = null;

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
                Cast = mediaEntry.GetPeople(Roles.Cast),
                Description = mediaEntry.Description,
                Directors = mediaEntry.GetPeople(Roles.Director),
                Genres = mediaEntry.Genres.Value,
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
                    .Include(item => item.BifServiceCredential)
                    .Include(item => item.VideoServiceCredential)
                    .Include(item => item.Subtitles)
                    .ThenInclude(item => item.ServiceCredential)
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
                    BifAsset = Utils.GetAsset(dbEp.BifServiceCredential, _memoryCache, dbEp.BifUrl),
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
                    VideoAsset = Utils.GetAsset(dbEp.VideoServiceCredential, _memoryCache, dbEp.VideoUrl)
                };

                if(ep.BifAsset != null)
                    ep.BifAsset = new StreamingAsset { AssetType = ep.BifAsset.AssetType, ServiceCredentialId = dbEp.BifServiceCredentialId, Url = dbEp.BifUrl };
                ep.VideoAsset = new StreamingAsset { AssetType = ep.VideoAsset.AssetType, ServiceCredentialId = dbEp.VideoServiceCredentialId, Url = dbEp.VideoUrl };

                if (dbEp.Subtitles != null && dbEp.Subtitles.Count > 0)
                {
                    ep.ExternalSubtitles = new List<ExternalSubtitle>();
                    foreach (var dbSub in dbEp.Subtitles)
                    {
                        var asset = Utils.GetAsset(dbSub.ServiceCredential, _memoryCache, dbSub.Url);
                        asset = new StreamingAsset { AssetType = asset.AssetType, ServiceCredentialId = dbSub.ServiceCredentialId, Url = dbSub.Url };
                        var xs = new ExternalSubtitle { Name = dbSub.Name };
                        xs.ExpiresUTC = asset.ExpiresUTC;
                        xs.ServiceCredentialId = asset.ServiceCredentialId;
                        xs.Token = asset.Token;
                        xs.Url = asset.Url;
                        ep.ExternalSubtitles.Add(xs);
                    }
                }


                ret.Episodes.Add(ep);
            }

            //Extra Search Terms
            var allTerms = mediaEntry.MediaSearchBridges.Select(item => item.SearchTerm.Term).ToList();
            var coreTerms = mediaEntry.Title.Tokenize();
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

                join allowed in DB.SeriesPlayableByProfile(UserProfile) on sub.MediaEntryId equals allowed.Id

                orderby sub.MediaEntry.SortTitle

                select sub;

            var subs = await subsQ.ToListAsync();

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
            var series = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares)
                .Include(item => item.TitleOverrides)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (series == null)
                return NotFound();

            //Check if this user has access to the lib
            if (!series.Library.ProfileLibraryShares.Select(item => item.ProfileId).Contains(UserProfile.Id))
                return NotFound();

            //Check ratings
            if (!UserProfile.IsMain)
            {
                bool allowed = series.Rated.HasValue ? (UserProfile.AllowedRatings & series.Rated) == series.Rated : UserProfile.AllowedRatings == Ratings.All;
                if (!allowed)
                    allowed = series
                        .TitleOverrides
                        .Where(item => item.ProfileId == UserProfile.Id)
                        .Where(item => item.State == OverrideState.Allow)
                        .Any();

                if (!allowed)
                    return CommonResponses.Forbid;
            }

            //Make sure this is a series
            if (series.EntryType != MediaTypes.Series)
                return BadRequest("The specified id is not a series");


            var newItem = DB.Subscriptions.Add(new Subscription
            {
                MediaEntryId = id,
                ProfileId = UserProfile.Id
            }).Entity;

            await DB.SaveChangesAsync();

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

            var query =
                from series in DB.SeriesPlayableByProfile(UserProfile)
                join episode in DB.EpisodesPlayableByProfile(UserProfile) on series.Id equals episode.LinkedToId
                join progress in MediaProgress on episode.Id equals progress.MediaEntryId
                where series.Id == id
                select progress;

            var lst = await query.ToListAsync();
            lst.ForEach(item => DB.Entry(item).State = EntityState.Deleted);

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
        public Task<ActionResult> RequestAccessOverride(int id) => RequestAccessOverride(id, MediaTypes.Series);


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
        public Task<ActionResult> SetAccessOverride(API.v3.Models.TitleOverride info) => SetAccessOverride(info, MediaTypes.Series);

    }
}
