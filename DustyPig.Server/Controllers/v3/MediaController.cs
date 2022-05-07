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


            //Continue Watching
            var cwResults = await ContinueWatchingQuery
                .Take(LIST_SIZE)
                .ToListAsync();

            if (cwResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_CONTINUE_WATCHING,
                    Title = "Continue Watching",
                    Items = new List<BasicMedia>(cwResults.Select(item => item.ToBasicMedia()))
                });


            //Watchlist
            var wlResults = await WatchlistQuery
                .Take(LIST_SIZE)
                .ToListAsync();

            if (wlResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_WATCHLIST,
                    Title = "Watchlist",
                    Items = new List<BasicMedia>(wlResults.Select(item => item.ToBasicMedia()))
                });


            //Playlists
            var plResults = await PlaylistQuery
                .Take(LIST_SIZE)
                .ToListAsync();
            if (plResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_PLAYLISTS,
                    Title = "Playlists",
                    Items = new List<BasicMedia>(plResults.Select(item => item.ToBasicMedia()))
                });


            //Recently Added
            var raResults = await RecentlyAddedQuery
                .Take(LIST_SIZE)
                .ToListAsync();

            if (raResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = ID_RECENTLY_ADDED,
                    Title = "Recently Added",
                    Items = new List<BasicMedia>(raResults.Select(item => item.ToBasicMedia()))
                });


            //Genres
            var genresQ = GenresQuery(Genres.Action).Take(LIST_SIZE);
            foreach (Genres genre in Enum.GetValues(typeof(Genres)))
                if (genre != Genres.Unknown && genre != Genres.Action)
                    genresQ = genresQ.Union(GenresQuery(genre).Take(LIST_SIZE));

            var gResults = await genresQ.ToListAsync();
            foreach (var result in gResults)
            {
                var lst = ret.Sections.FirstOrDefault(item => item.ListId == (long)result.Genre);
                if (lst == null)
                {
                    lst = new HomeScreenList
                    {
                        ListId = (long)result.Genre,
                        Title = result.Genre.AsString(),
                        Items = new List<BasicMedia>()
                    };
                    ret.Sections.Add(lst);
                }

                lst.Items.Add(result.MediaEntry.ToBasicMedia());
            }

            var tooSmall = new List<long>();
            foreach (var sect in ret.Sections.Where(item => item.ListId > 0))
                if (sect.Items.Count < 25)
                    tooSmall.Add(sect.ListId);
            ret.Sections.RemoveAll(item => tooSmall.Contains(item.ListId));

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
                results = (await ContinueWatchingQuery
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());

            if (request.ListId == ID_WATCHLIST)
                results = (await WatchlistQuery
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == ID_RECENTLY_ADDED)
                results = (await RecentlyAddedQuery
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == ID_PLAYLISTS)
                results = (await PlaylistQuery
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());

            if (request.ListId > 0)
            {
                var q = GenresQuery((Genres)request.ListId);
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
                from mediaEntry in DB.MoviesAndSeriesPlayableByProfile(UserProfile)
                where request.LibraryId == mediaEntry.LibraryId
                select mediaEntry;

            var sortedQ = ApplySortOrder(q, request.Sort);

            var entries = await sortedQ
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

            var q = GenresQuery(request.Genre);

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
        public async Task<ActionResult<SearchResults>> Search(SimpleValue<string> q)
        {
            var ret = new SearchResults();

            if (string.IsNullOrWhiteSpace(q.Value))
                return ret;

            q.Value = StringUtils.NormalizedQueryString(q.Value);
            if (string.IsNullOrWhiteSpace(q.Value))
                return ret;

            var terms = q.Value.Tokenize();




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

            mediaEntries = await mediaEntriesQ.Distinct().ToListAsync();


            //Search sort
            mediaEntries.Sort((x, y) =>
            {
                int ret = -x.QueryTitle.ICEquals(q.Value).CompareTo(y.QueryTitle.ICEquals(q.Value));
                if (ret == 0)
                    ret = -x.QueryTitle.ICStartsWith(q.Value).CompareTo(y.QueryTitle.ICStartsWith(q.Value));
                if (ret == 0)
                    ret = -x.QueryTitle.ICContains(q.Value).CompareTo(y.QueryTitle.ICContains(q.Value));
                if (ret == 0)
                    ret = x.SortTitle.CompareTo(y.SortTitle);
                if (ret == 0)
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                return ret;
            });

            ret.Available.AddRange(mediaEntries.Select(item => item.ToBasicMedia()));


            /****************************************
             * Search online databases
             ****************************************/
            //if (UserAccount.Id != TestAccount.AccountId)
            //{
            //    if (UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled)
            //    {
            //        var response = await _tmdbClient.SearchAsync(q.Value);
            //        if (response.Success)
            //        {
            //            var skipMovies = ret.Available.Where(item => item.MediaType == MediaTypes.Movie).Select(item => item.Id);
            //            var skipTV = ret.Available.Where(item => item.MediaType == MediaTypes.Series).Select(item => item.Id);
            //            foreach (var result in response.Data)
            //            {
            //                bool add = result.IsMovie ?
            //                    !skipMovies.Contains(result.Id) :
            //                    !skipTV.Contains(result.Id);

            //                if (add)
            //                    ret.OtherTitles.Add(result.ToBasicTMDBInfo());
            //            }
            //        }
            //    }
            //}

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
            var mediaEntry = await DB.MediaEntriesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return NotFound("Media not found");


            var alreadyAdded = await DB.WatchListItems
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.MediaEntryId == id)
                .AnyAsync();

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





        private IQueryable<MediaEntry> ContinueWatchingQuery
        {
            get
            {
                //Get the max Xid for series
                var maxXidQ =
                   from mediaEntry in DB.EpisodesPlayableByProfile(UserProfile)
                   group mediaEntry by mediaEntry.LinkedToId into g
                   select new
                   {
                       SeriesId = g.Key,
                       LastXid = g.Max(item => item.Xid)
                   };

                //Get the timings for last Xid in series
                var lastEpInfoQ =
                    from maxXid in maxXidQ
                    join mediaEntry in DB.EpisodesPlayableByProfile(UserProfile)
                        on new { maxXid.SeriesId, maxXid.LastXid } equals new { SeriesId = mediaEntry.LinkedToId, LastXid = mediaEntry.Xid }

                    where
                        mediaEntry.LinkedToId.HasValue
                        && mediaEntry.Xid.HasValue
                        && mediaEntry.Length.HasValue
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
                var watchedSeriesQ = MediaProgress
                    .Include(item => item.MediaEntry)
                    .Where(item => item.MediaEntry.EntryType == MediaTypes.Episode)
                    .Where(item => item.MediaEntry.LinkedToId.HasValue)
                    .Where(item => item.MediaEntry.Xid.HasValue)
                    .Select(item => new
                    {
                        item.MediaEntry.LinkedToId,
                        item.MediaEntry.Xid,
                        item.Played,
                        item.Timestamp
                    });

                //Finalize the series query
                var seriesFinalQ =
                    from mediaEntry in DB.SeriesPlayableByProfile(UserProfile)
                    join watchedSeries in watchedSeriesQ on mediaEntry.Id equals watchedSeries.LinkedToId
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
                    from mediaEntry in DB.MoviesPlayableByProfile(UserProfile)
                    join profileMediaProgress in MediaProgress on mediaEntry.Id equals profileMediaProgress.MediaEntryId

                    where
                        profileMediaProgress.Played > 1000 && profileMediaProgress.Played < (mediaEntry.CreditsStartTime ?? mediaEntry.Length.Value * 0.9)

                    select new { mediaEntry, profileMediaProgress.Timestamp };



                var combinedQ = seriesFinalQ.Union(movieQ);

                var continueWatchingQ =
                    from item in combinedQ
                    orderby item.Timestamp descending
                    select item.mediaEntry;

                return continueWatchingQ.AsNoTracking();

            }
        }


        private IQueryable<MediaEntry> RecentlyAddedQuery
        {
            get
            {
                var maxAddedForSeriesQ =
                   from mediaEntry in DB.EpisodesPlayableByProfile(UserProfile)
                   group mediaEntry by mediaEntry.LinkedToId into g
                   select new
                   {
                       Id = g.Key,
                       Timestamp = g.Max(item => item.Added)
                   };

                var seriesQ =
                    from mediaEntry in DB.SeriesPlayableByProfile(UserProfile)
                    join maxAddedForSeries in maxAddedForSeriesQ on mediaEntry.Id equals maxAddedForSeries.Id
                    select new
                    {
                        MediaEntry = mediaEntry,
                        maxAddedForSeries.Timestamp
                    };


                var movieQ =
                   from mediaEntry in DB.MoviesPlayableByProfile(UserProfile)
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
        }


        private IQueryable<GenreListDTO> GenresQuery(Genres genre)
        {
            var genreQ =
                from mediaEntry in DB.MoviesAndSeriesPlayableByProfile(UserProfile)
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


        private IQueryable<MediaEntry> WatchlistQuery
        {
            get
            {
                var watchlistQ = DB.WatchListItems
                    .Where(item => item.ProfileId == UserProfile.Id);

                var ret =
                    from mediaEntry in DB.MoviesAndSeriesPlayableByProfile(UserProfile)
                    join watchListItem in watchlistQ on mediaEntry.Id equals watchListItem.MediaEntryId
                    orderby watchListItem.Added
                    select mediaEntry;

                return ret.AsNoTracking();
            }
        }

        private IQueryable<Data.Models.Playlist> PlaylistQuery
        {
            get
            {
                var ret = DB.Playlists
                    .Where(item => item.ProfileId == UserProfile.Id)
                    .OrderBy(item => item.Name);

                return ret.AsNoTracking();
            }
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
