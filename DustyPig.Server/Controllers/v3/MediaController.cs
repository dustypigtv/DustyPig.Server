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
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(MediaController))]
    public class MediaController : _MediaControllerBase
    {
        public MediaController(AppDbContext db, TMDBClient tmdbClient) : base(db, tmdbClient)
        {
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<HomeScreen>> HomeScreen()
        {
            var ret = new HomeScreen();            

            //To speed up, run all queries at once
            var allTasks = new List<Task>();

            //Continue Watching
            var cwTask = ContinueWatchingQuery(new AppDbContext())
                .Take(LIST_SIZE)
                .ToListAsync();
            allTasks.Add(cwTask);

            //Watchlist
            var wlTask = WatchlistQuery(new AppDbContext())
                .Take(LIST_SIZE)
                .ToListAsync();
            allTasks.Add(wlTask);

            //Playlists
            var plTask = PlaylistQuery(new AppDbContext())
                .Take(LIST_SIZE)
                .ToListAsync();
            allTasks.Add(plTask);

            //Recently Added
            var raTask = RecentlyAddedQuery(new AppDbContext())
                .Take(LIST_SIZE)
                .ToListAsync();
            allTasks.Add(raTask);

            //Popular
            var popTask = PopularQuery(new AppDbContext())
                .Take(LIST_SIZE)
                .ToListAsync();
            allTasks.Add(popTask);



            await Task.WhenAll(allTasks);


            //Continue Watching
            var cwResults = cwTask.Result;
            if (cwResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_CONTINUE_WATCHING,
                    Title = ID_CONTINUE_WATCHING_TITLE,
                    Items = new List<BasicMedia>(cwResults.Select(item => item.ToBasicMedia()))
                });


            //Watchlist
            var wlResults = wlTask.Result;
            if (wlResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_WATCHLIST,
                    Title = ID_WATCHLIST_TITLE,
                    Items = new List<BasicMedia>(wlResults.Select(item => item.ToBasicMedia()))
                });


            //Playlists
            var plResults = plTask.Result;
            if (plResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_PLAYLISTS,
                    Title = ID_PLAYLISTS_TITLE,
                    Items = new List<BasicMedia>(plResults.Select(item => item.ToBasicMedia()))
                });


            //Recently Added
            var raResults = raTask.Result;
            if (raResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_RECENTLY_ADDED,
                    Title = "Recently Added",
                    Items = new List<BasicMedia>(raResults.Select(item => item.ToBasicMedia()))
                });


            //Popular
            var popResults = popTask.Result;
            if (popResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_POPULAR,
                    Title = "Popular",
                    Items = new List<BasicMedia>(popResults.Select(item => item.ToBasicMedia()))
                });

            ret.Sections.Sort((x, y) => x.ListId.CompareTo(y.ListId));
            return ret;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns more items for the specified home screen list based on start position</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> LoadMoreHomeScreenItems(IDListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            IEnumerable<BasicMedia> results = null;

            if (request.ListId == ID_CONTINUE_WATCHING)
                results = (await ContinueWatchingQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());

            if (request.ListId == ID_WATCHLIST)
                results = (await WatchlistQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == ID_RECENTLY_ADDED)
                results = (await RecentlyAddedQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == ID_PLAYLISTS)
                results = (await PlaylistQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == ID_POPULAR)
                results = (await PopularQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId > 0)
            {
                var q = GenresQuery((Genres)request.ListId, (DB));
                var sortedQ = ApplySortOrder(q, SortOrder.Popularity_Descending);
                results = (await sortedQ
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.MediaEntry.ToBasicMedia());
            }


            if (results == null)
                return new List<BasicMedia>();

            return results.ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 items in a library based on start position and sort order</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> ListLibraryItems(LibraryListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var q =
                from mediaEntry in DB.MoviesAndSeriesPlayableByProfile(UserAccount, UserProfile)
                where request.LibraryId == mediaEntry.LibraryId
                select mediaEntry;

            var sortedQ = ApplySortOrder(q, request.Sort);

            var entries = await sortedQ
                .AsNoTracking()
                .Skip(request.Start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return entries.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 items in a Genre based on start position and sort order</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> ListGenreItems(GenreListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var q = GenresQuery(request.Genre, DB);

            var sortedQ = ApplySortOrder(q, SortOrder.Popularity_Descending);

            var entries = (await sortedQ
                        .Skip(request.Start)
                        .Take(LIST_SIZE)
                        .ToListAsync())
                        .Select(item => item.MediaEntry);

            return entries.Select(item => item.ToBasicMedia()).ToList();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        /// <param name="q">
        /// Url encoded title to search for
        /// </param>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<SearchResults>> Search(SearchRequest q, CancellationToken cancellationToken)
        {
            var ret = new SearchResults();

            if (string.IsNullOrWhiteSpace(q.Query))
                return ret;

            q.Query = StringUtils.NormalizedQueryString(q.Query);
            if (string.IsNullOrWhiteSpace(q.Query))
                return ret;

            var terms = q.Query.Tokenize();




            /****************************************
             * Get available items
             ****************************************/
            List<MediaEntry> mediaEntries;

            var searchQ = DB.MediaSearchBridges
                .Include(item => item.SearchTerm)
                .Include(item => item.MediaEntry)
                .Select(item => item);

            foreach (var term in terms)
                searchQ =
                    from st1 in searchQ
                    join st2 in DB.MediaSearchBridges
                        .Include(item => item.SearchTerm)
                        .Include(item => item.MediaEntry)
                        .Where(item => item.SearchTerm.Term.Contains(term)) on st1.MediaEntryId equals st2.MediaEntryId
                    select st1;

            var mediaEntriesQ = searchQ
                .Select(item => item.MediaEntry)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .Distinct();

            mediaEntriesQ =
                from mediaEntry in mediaEntriesQ
                join dummy in DB.MediaEntriesSearchableByProfile(UserAccount, UserProfile) on mediaEntry.Id equals dummy.Id
                select mediaEntry;

            mediaEntries = await mediaEntriesQ
                .AsNoTracking()
                .ToListAsync(cancellationToken);


            //Search sort
            mediaEntries.Sort((x, y) =>
            {
                int ret = -x.QueryTitle.ICEquals(q.Query).CompareTo(y.QueryTitle.ICEquals(q.Query));
                if (ret == 0 && x.QueryTitle.ICEquals(y.QueryTitle))
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                if (ret == 0)
                    ret = -x.QueryTitle.ICStartsWith(q.Query).CompareTo(y.QueryTitle.ICStartsWith(q.Query));
                if (ret == 0)
                    ret = -x.QueryTitle.ICContains(q.Query).CompareTo(y.QueryTitle.ICContains(q.Query));
                if (ret == 0)
                    ret = x.SortTitle.CompareTo(y.SortTitle);
                if (ret == 0)
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                return ret;
            });

            ret.Available.AddRange(mediaEntries.Select(item => item.ToBasicMedia()).Take(LIST_SIZE));


            /****************************************
             * Search online databases
             ****************************************/
            ret.OtherTitlesAllowed = UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled;

            if (q.SearchTMDB && UserAccount.Id != TestAccount.AccountId && ret.OtherTitlesAllowed)
            {
                var response = await _tmdbClient.SearchAsync(q.Query, cancellationToken);
                if (response.Success)
                    ret.OtherTitles.AddRange(response.Data.Select(item => item.ToBasicTMDBInfo()).Take(LIST_SIZE));
            }

            return ret;
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> AddToWatchlist(int id)
        {
            var mediaEntry = await DB.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Include(item => item.WatchlistItems)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return NotFound("Media not found");


            var alreadyAdded = mediaEntry.WatchlistItems.Any(item => item.ProfileId == UserProfile.Id);

            if (!alreadyAdded)
            {
                DB.WatchListItems.Add(new WatchlistItem
                {
                    Added = DateTime.UtcNow,
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
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> DeleteFromWatchlist(int id)
        {
            var item = await DB.WatchListItems
                .Where(item => item.MediaEntryId == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (item != null)
            {
                DB.WatchListItems.Remove(item);
                await DB.SaveChangesAsync();
            }

            return Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> UpdatePlaybackProgress(PlaybackProgress hist)
        {
            if (hist == null)
                return NotFound();

            if (hist.Id <= 0)
                return NotFound();

            var mediaEntry = await DB.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == hist.Id)
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return NotFound();

            if (mediaEntry.EntryType != MediaTypes.Movie && mediaEntry.EntryType != MediaTypes.Episode)
                return BadRequest("This method is only for Movies and Episodes");

            int id = mediaEntry.EntryType == MediaTypes.Movie
                ? mediaEntry.Id
                : mediaEntry.LinkedToId.Value;

            var prog = await DB.MediaProgress(UserProfile)
                .FirstOrDefaultAsync(item => item.MediaEntryId == id);
            
            if (prog == null)
            {
                if (hist.Seconds < 1 && mediaEntry.EntryType == MediaTypes.Movie)
                    return Ok();

                //Add
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = id,
                    ProfileId = UserProfile.Id,
                    Played = Math.Max(0, hist.Seconds),
                    Timestamp = DateTime.UtcNow,
                    Xid = mediaEntry.Xid
                });

                await DB.SaveChangesAsync();
            }
            else
            {
                if (hist.Seconds < 1 && mediaEntry.EntryType == MediaTypes.Movie)
                {
                    //Reset
                    DB.ProfileMediaProgresses.Remove(prog);
                }
                else
                {
                    //Update
                    prog.Played = Math.Max(0, hist.Seconds);
                    prog.Timestamp = DateTime.UtcNow;
                    prog.Xid = mediaEntry.Xid;
                }

                await DB.SaveChangesAsync();
            }

            return Ok();
        }



        private IQueryable<MediaEntry> ContinueWatchingQuery(AppDbContext dbInstance)
        {
            //Get the max Xid for series
            var maxXidQ =
               from mediaEntry in dbInstance.EpisodesPlayableByProfile(UserAccount, UserProfile)
               group mediaEntry by mediaEntry.LinkedToId into g
               select new
               {
                   SeriesId = g.Key,
                   LastXid = g.Max(item => item.Xid)
               };

            //Get the timings for last Xid in series
            var lastEpInfoQ =
                from maxXid in maxXidQ
                join mediaEntry in dbInstance.EpisodesPlayableByProfile(UserAccount, UserProfile)
                    on new { maxXid.SeriesId, maxXid.LastXid } equals new { SeriesId = mediaEntry.LinkedToId, LastXid = mediaEntry.Xid }

                where
                    mediaEntry.EntryType == MediaTypes.Episode
                    && maxXid.SeriesId.HasValue
                    && maxXid.LastXid.HasValue
                select new
                {
                    maxXid.SeriesId,
                    maxXid.LastXid,
                    mediaEntry.Length,
                    mediaEntry.CreditsStartTime,
                    mediaEntry.Added
                };


            //Get seriesId for watched
            var watchedSeriesQ = dbInstance.MediaProgress(UserProfile)
                .Include(item => item.MediaEntry)
                .Where(item => item.MediaEntry.EntryType == MediaTypes.Series);
                

            //Finalize the series query
            var seriesFinalQ =
                from mediaEntry in dbInstance.SeriesPlayableByProfile(UserAccount, UserProfile)
                join watchedSeries in watchedSeriesQ on mediaEntry.Id equals watchedSeries.MediaEntryId
                join lastEpInfo in lastEpInfoQ on mediaEntry.Id equals lastEpInfo.SeriesId
                where
                    watchedSeries.Xid.HasValue
                    && lastEpInfo.LastXid.HasValue
                    && lastEpInfo.Length.HasValue
                    &&
                    (
                        watchedSeries.Xid < lastEpInfo.LastXid
                        ||
                        (
                            watchedSeries.Xid == lastEpInfo.LastXid
                            && watchedSeries.Played < (lastEpInfo.CreditsStartTime ?? lastEpInfo.Length - 30)
                        )
                    )

                select new
                {
                    mediaEntry,
                    Timestamp = watchedSeries.Timestamp > lastEpInfo.Added ? watchedSeries.Timestamp : lastEpInfo.Added.Value
                };


            //The movie query is pretty simple compared to the series
            var movieQ =
                from mediaEntry in dbInstance.MoviesPlayableByProfile(UserAccount, UserProfile)
                join profileMediaProgress in dbInstance.MediaProgress(UserProfile) on mediaEntry.Id equals profileMediaProgress.MediaEntryId

                where
                    profileMediaProgress.Played >= 1 && profileMediaProgress.Played < (mediaEntry.CreditsStartTime ?? mediaEntry.Length.Value * 0.9)

                select new { mediaEntry, profileMediaProgress.Timestamp };



            var combinedQ = seriesFinalQ.Union(movieQ);

            var continueWatchingQ =
                from item in combinedQ
                orderby item.Timestamp descending
                select item.mediaEntry;

            return continueWatchingQ.AsNoTracking();
        }


        private IQueryable<MediaEntry> RecentlyAddedQuery(AppDbContext dbInstance)
        {
            var maxAddedForSeriesQ =
               from mediaEntry in dbInstance.EpisodesPlayableByProfile(UserAccount, UserProfile)
               group mediaEntry by mediaEntry.LinkedToId into g
               select new
               {
                   Id = g.Key,
                   Timestamp = g.Max(item => item.Added)
               };

            var seriesQ =
                from mediaEntry in dbInstance.SeriesPlayableByProfile(UserAccount, UserProfile)
                join maxAddedForSeries in maxAddedForSeriesQ on mediaEntry.Id equals maxAddedForSeries.Id
                select new
                {
                    MediaEntry = mediaEntry,
                    maxAddedForSeries.Timestamp
                };


            var movieQ =
               from mediaEntry in dbInstance.MoviesPlayableByProfile(UserAccount, UserProfile)
               select new
               {
                   MediaEntry = mediaEntry,
                   Timestamp = mediaEntry.Added
               };


            var recentlyAddedQ =
                from item in seriesQ.Union(movieQ)
                orderby item.Timestamp descending
                select item.MediaEntry;

            return recentlyAddedQ.AsNoTracking();
        }


        private IQueryable<MediaEntry> PopularQuery(AppDbContext dbInstance)
        {
            var maxAddedForSeriesQ =
               from mediaEntry in dbInstance.EpisodesPlayableByProfile(UserAccount, UserProfile)
               group mediaEntry by mediaEntry.LinkedToId into g
               select g.Key;

            var seriesQ =
                from mediaEntry in dbInstance.SeriesPlayableByProfile(UserAccount, UserProfile)
                join maxAddedForSeries in maxAddedForSeriesQ on mediaEntry.Id equals maxAddedForSeries
                select mediaEntry;


            var movieQ =
               from mediaEntry in dbInstance.MoviesPlayableByProfile(UserAccount, UserProfile)
               select mediaEntry;


            var recentlyAddedQ =
                from item in seriesQ.Union(movieQ)
                where item.Popularity > 0
                orderby item.Popularity descending
                select item;

            return recentlyAddedQ.AsNoTracking();
        }


        private IQueryable<GenreListDTO> GenresQuery(Genres genre, AppDbContext dbInstance)
        {
            var genreQ =
                from mediaEntry in dbInstance.MoviesAndSeriesPlayableByProfile(UserAccount, UserProfile)
                where (mediaEntry.Genres & genre) == genre
                orderby
                    mediaEntry.Popularity descending,
                    mediaEntry.SortTitle

                select new GenreListDTO
                {
                    Genre = genre,
                    MediaEntry = mediaEntry
                };

            return genreQ.AsNoTracking();
        }


        private IQueryable<MediaEntry> WatchlistQuery(AppDbContext dbInstance)
        {
            var watchlistQ = dbInstance.WatchListItems
                .Where(item => item.ProfileId == UserProfile.Id);

            var ret =
                from mediaEntry in dbInstance.MoviesAndSeriesPlayableByProfile(UserAccount, UserProfile)
                join watchListItem in watchlistQ on mediaEntry.Id equals watchListItem.MediaEntryId
                orderby watchListItem.Added
                select mediaEntry;

            return ret.AsNoTracking();
        }

        private IQueryable<Data.Models.Playlist> PlaylistQuery(AppDbContext dbInstance)
        {
            var playableIds = dbInstance.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
                .Select(item => item.Id);

            var ret = dbInstance.Playlists
                .Include(item => item.PlaylistItems.Where(item2 => playableIds.Contains(item2.MediaEntryId)))
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo)
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Name);

            return ret.AsNoTracking();
        }

       


        private static IOrderedQueryable<GenreListDTO> ApplySortOrder(IQueryable<GenreListDTO> q, SortOrder sortOrder)
        {
            if (sortOrder == SortOrder.Alphabetical)
                return q.OrderBy(item => item.MediaEntry.SortTitle);

            if (sortOrder == SortOrder.Alphabetical_Descending)
                return q.OrderByDescending(item => item.MediaEntry.SortTitle);

            if (sortOrder == SortOrder.Added)
                return q.OrderBy(item => item.MediaEntry.Added);

            if (sortOrder == SortOrder.Added_Descending)
                return q.OrderByDescending(item => item.MediaEntry.Added);

            if (sortOrder == SortOrder.Released)
                return q.OrderBy(item => item.MediaEntry.Date);

            if (sortOrder == SortOrder.Released_Descending)
                return q.OrderByDescending(item => item.MediaEntry.Date);

            if (sortOrder == SortOrder.Popularity)
                return q.OrderBy(item => item.MediaEntry.Popularity);

            if (sortOrder == SortOrder.Popularity_Descending)
                return q.OrderByDescending(item => item.MediaEntry.Popularity);

            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }


        private class GenreListDTO
        {
            public Genres Genre { get; set; }
            public MediaEntry MediaEntry { get; set; }
        }

        
    }
}
