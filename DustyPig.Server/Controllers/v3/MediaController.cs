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


            //Genres
            Dictionary<Genres, Task<List<GenreListDTO>>> genreTasks = new();
            foreach(var genre in GenresUtils.AllGenres.Where(item => item.Key != Genres.Unknown))
            {
                var task = GenresQuery(genre.Key, new AppDbContext())
                    .Take(LIST_SIZE)
                    .ToListAsync();
                genreTasks.Add(genre.Key, task);
            }
            allTasks.AddRange(genreTasks.Values);

            await Task.WhenAll(allTasks);


            //Continue Watching
            var cwResults = cwTask.Result;
            if (cwResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING,
                    Title = DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING_TITLE,
                    Items = new List<BasicMedia>(cwResults.Select(item => item.ToBasicMedia()))
                });


            //Watchlist
            var wlResults = wlTask.Result;
            if (wlResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST,
                    Title = DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST_TITLE,
                    Items = new List<BasicMedia>(wlResults.Select(item => item.ToBasicMedia()))
                });


            //Playlists
            var plResults = plTask.Result;
            if (plResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS,
                    Title = DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS_TITLE,
                    Items = new List<BasicMedia>(plResults.Select(item => item.ToBasicMedia()))
                });


            //Recently Added
            var raResults = raTask.Result;
            if (raResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED,
                    Title = DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED_TITLE,
                    Items = new List<BasicMedia>(raResults.Select(item => item.ToBasicMedia()))
                });


            //Popular
            var popResults = popTask.Result;
            if (popResults.Count > 0)
                ret.Sections.Add(new HomeScreenList
                {
                    ListId = DustyPig.API.v3.Clients.MediaClient.ID_POPULAR,
                    Title = DustyPig.API.v3.Clients.MediaClient.ID_POPULAR_TITLE,
                    Items = new List<BasicMedia>(popResults.Select(item => item.ToBasicMedia()))
                });

            //Genres
            foreach(var kvp in genreTasks)
                if (kvp.Value.Result.Count >= 5)
                    ret.Sections.Add(new HomeScreenList
                    {
                        ListId =(long)kvp.Key,
                        Title = kvp.Key.AsString(),
                        Items = kvp.Value.Result.Select(item => item.MediaEntry.ToBasicMedia()).ToList()
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

            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING)
                results = (await ContinueWatchingQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());

            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST)
                results = (await WatchlistQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED)
                results = (await RecentlyAddedQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS)
                results = (await PlaylistQuery(DB)
                            .Skip(request.Start)
                            .Take(LIST_SIZE)
                            .ToListAsync())
                            .Select(item => item.ToBasicMedia());


            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_POPULAR)
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



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<SimpleValue<Ratings>>> GetAllAvailableRatings()
        {
            var available = await DB.MoviesAndSeriesPlayableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Where(item => item.Rated.HasValue)
                .Select(item => item.Rated.Value)
                .Distinct()
                .ToListAsync();

            var strings = available.Select(item => item.ToString());

            return new SimpleValue<Ratings>(strings.ToRatings());
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<SimpleValue<Genres>>> GetAllAvailableGenres()
        {
            var available = await DB.MoviesAndSeriesPlayableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Where(item => item.Genres.HasValue)
                .Where(item => item.Genres != Genres.Unknown)
                .Select(item => item.Genres.Value)
                .Distinct()
                .ToListAsync();

            var ret = Genres.Unknown;
            foreach(var item in available)
                foreach(Genres g in Enum.GetValues(typeof(Genres)))
                    if (g != Genres.Unknown)
                        if (item.HasFlag(g))
                            ret |= g;

            return new SimpleValue<Genres>(ret);
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        public async Task<ActionResult<TitlePermissionInfo>> GetTitlePermissions(int id)
        {
            if (!UserProfile.IsMain)
                return Forbid();

            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id == id)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFoundObject("Media not found");

            var ret = new TitlePermissionInfo { TitleId = id };

            var profiles = await DB.Profiles
                .AsNoTracking()
                .Include(item => item.ProfileLibraryShares)
                .Include(item => item.TitleOverrides)
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => !item.IsMain)
                .OrderBy(item => item.Name)
                .ToListAsync();
            

            foreach(var profile in profiles)
            {
                var profInfo = new ProfileTitlePermissionInfo
                {
                    AvatarUrl = profile.AvatarUrl,
                    HasPin = profile.PinNumber != null && profile.PinNumber > 999,
                    Id = profile.Id,
                    IsMain = profile.IsMain,
                    Name = profile.Name
                };

                profInfo.HasLibraryAccess = profile.ProfileLibraryShares.Any(item => item.LibraryId == mediaEntry.LibraryId);
                if(profInfo.HasLibraryAccess)
                {
                    profInfo.CanWatchByDefault = profile.AllowedRatings == Ratings.All || (profile.AllowedRatings & mediaEntry.Rated) == mediaEntry.Rated;
                    var ovrride = profile.TitleOverrides.FirstOrDefault(item => item.MediaEntryId == mediaEntry.Id);
                    if (ovrride != null)
                        profInfo.Override = ovrride.State;
                }

                ret.Profiles.Add(profInfo);
            }

            return ret;
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<List<BasicMedia>>> Explore(ExploreRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var maxAddedForSeriesQ =
              from mediaEntry in DB.EpisodesPlayableByProfile(UserAccount, UserProfile)
              group mediaEntry by mediaEntry.LinkedToId into g
              select new
              {
                  Id = g.Key,
                  Timestamp = g.Max(item => item.Added),
                  Released = g.Min(item => item.Date)
              };

            var seriesQ =
                from mediaEntry in DB.SeriesPlayableByProfile(UserAccount, UserProfile)
                join maxAddedForSeries in maxAddedForSeriesQ on mediaEntry.Id equals maxAddedForSeries.Id
                select new
                {
                    MediaEntry = mediaEntry,
                    maxAddedForSeries.Timestamp,
                    maxAddedForSeries.Released
                };


            var movieQ =
               from mediaEntry in DB.MoviesPlayableByProfile(UserAccount, UserProfile)
               select new
               {
                   MediaEntry = mediaEntry,
                   Timestamp = mediaEntry.Added,
                   Released = mediaEntry.Date
               };


            var mediaQ =
                request.ReturnMovies && request.ReturnSeries
                ? movieQ.Union(seriesQ)
                : request.ReturnMovies
                ? movieQ
                : seriesQ;


            if(request.FilterOnGenres != null)
            {
                if (request.IncludeUnknownGenres)
                    mediaQ = mediaQ.Where(item => item.MediaEntry.Genres == null || item.MediaEntry.Genres == Genres.Unknown || (request.FilterOnGenres & item.MediaEntry.Genres) == request.FilterOnGenres);
                else
                    mediaQ = mediaQ.Where(item => item.MediaEntry.Genres != null && item.MediaEntry.Genres != Genres.Unknown && (request.FilterOnGenres & item.MediaEntry.Genres) == request.FilterOnGenres);
            }

            if(request.FilterOnRatings != null)
            {
                if(request.IncludeNoneRatings)
                    mediaQ = mediaQ.Where(item => item.MediaEntry.Rated == null || item.MediaEntry.Rated == Ratings.None || (request.FilterOnRatings & item.MediaEntry.Rated) == item.MediaEntry.Rated);
                else
                    mediaQ = mediaQ.Where(item => item.MediaEntry.Rated != null && item.MediaEntry.Rated != Ratings.None && (request.FilterOnRatings & item.MediaEntry.Rated) == item.MediaEntry.Rated);
            }

                
            if(request.LibraryIds != null && request.LibraryIds.Count > 0)
                mediaQ = mediaQ.Where(item => request.LibraryIds.Contains(item.MediaEntry.LibraryId));
            

            switch (request.SortBy)
            {
                case SortOrder.Added:
                    mediaQ = mediaQ.OrderBy(item => item.Timestamp);
                    break;

                case SortOrder.Added_Descending:
                    mediaQ = mediaQ.OrderByDescending(item => item.Timestamp);
                    break;

                case SortOrder.Alphabetical:
                    mediaQ = mediaQ.OrderBy(item => item.MediaEntry.SortTitle);
                    break;

                case SortOrder.Alphabetical_Descending:
                    mediaQ = mediaQ.OrderByDescending(item => item.MediaEntry.SortTitle);
                    break;

                case SortOrder.Popularity:
                    mediaQ = mediaQ.OrderBy(item => item.MediaEntry.Popularity).ThenBy(item => item.MediaEntry.SortTitle);
                    break;

                case SortOrder.Released:
                    mediaQ = mediaQ.OrderBy(item => item.Released).ThenBy(item => item.MediaEntry.SortTitle);
                    break;

                case SortOrder.Released_Descending:
                    mediaQ = mediaQ.OrderByDescending(item => item.Released).ThenBy(item => item.MediaEntry.SortTitle);
                    break;


                default: // SortOrder.Popularity_Descending
                    mediaQ = mediaQ.OrderByDescending(item => item.MediaEntry.Popularity).ThenBy(item => item.MediaEntry.SortTitle);
                    break;
            }


            var ret = await mediaQ
                .AsNoTracking()
                .Skip(request.Start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return ret.Select(item => item.MediaEntry.ToBasicMedia()).ToList();
        }





        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> RequestAccessOverride(int id)
        {
            if (UserProfile.IsMain)
                return BadRequest("Main profile cannot requst overrides");

            if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                return CommonResponses.Forbid;

            var media = await DB.MediaEntriesSearchableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Include(item => item.TitleOverrides)
                .Where(item => item.Id == id)
                .FirstOrDefaultAsync();

            if (media == null)
                return NotFound();

            //Check if already requested
            var existingOverride = media.TitleOverrides.FirstOrDefault(item => item.ProfileId == UserProfile.Id);
            if (existingOverride != null)
            {
                if (existingOverride.State == OverrideState.Allow)
                    return BadRequest($"You already have access to this {media.EntryType.ToString().ToLower()}");

                if (existingOverride.Status != OverrideRequestStatus.NotRequested)
                    return BadRequest($"You have already requested access to this {media.EntryType.ToString().ToLower()}");

                existingOverride.Status = OverrideRequestStatus.Requested;
                DB.Entry(existingOverride).State = EntityState.Modified;

                DB.Notifications.Add(new Data.Models.Notification
                {
                    MediaEntryId = id,
                    TitleOverrideId = existingOverride.Id,
                    Message = $"{UserProfile.Name} has requsted access to \"{media.FormattedTitle()}\"",
                    NotificationType = NotificationType.OverrideRequest,
                    ProfileId = UserAccount.Profiles.First(item => item.IsMain).Id,
                    Title = "Access Request",
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                var request = DB.TitleOverrides.Add(new Data.Models.TitleOverride
                {
                    MediaEntryId = id,
                    ProfileId = UserProfile.Id,
                    State = OverrideState.Default,
                    Status = OverrideRequestStatus.Requested
                }).Entity;

                DB.Notifications.Add(new Data.Models.Notification
                {
                    MediaEntryId = id,
                    TitleOverride = request,
                    Message = $"{UserProfile.Name} has requsted access to \"{media.FormattedTitle()}\"",
                    NotificationType = NotificationType.OverrideRequest,
                    ProfileId = UserAccount.Profiles.First(item => item.IsMain).Id,
                    Title = "Access Request",
                    Timestamp = DateTime.UtcNow
                });
            }

            await DB.SaveChangesAsync();

            return Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Set access override for a specific movie</remarks>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> SetAccessOverride(API.v3.Models.TitleOverride info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            // Check the profiles
            foreach (var ptoi in info.Overrides)
                if (!UserAccount.Profiles.Any(item => item.Id == ptoi.ProfileId))
                    return NotFound("Profile not found");

            //Get the media entry
            var media = await DB.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
                .Include(item => item.TitleOverrides)
                .Where(item => item.Id == info.MediaEntryId)
                .FirstOrDefaultAsync();

            if (media == null)
                return NotFound();

            foreach (var ptoi in info.Overrides)
            {
                var overrideEntity = media.TitleOverrides
                    .Where(item => item.ProfileId == ptoi.ProfileId)
                    .FirstOrDefault();

                if (overrideEntity == null)
                {
                    if (ptoi.NewState == OverrideState.Default)
                    {
                        if (overrideEntity != null)
                            media.TitleOverrides.Remove(overrideEntity);
                    }
                    else
                    {
                        overrideEntity = new Data.Models.TitleOverride
                        {
                            ProfileId = ptoi.ProfileId,
                            MediaEntryId = info.MediaEntryId
                        };
                        media.TitleOverrides.Add(overrideEntity);
                    }
                }
                else
                {
                    if (overrideEntity.Status == OverrideRequestStatus.Requested)
                    {
                        if (ptoi.NewState == OverrideState.Allow)
                        {
                            overrideEntity.Status = OverrideRequestStatus.Granted;
                        }
                        else if (ptoi.NewState == OverrideState.Block)
                        {
                            overrideEntity.Status = OverrideRequestStatus.Denied;
                        }
                        else
                        {
                            //Default
                            var profile = UserAccount.Profiles.Single(item => item.Id == ptoi.ProfileId);
                            if (profile.AllowedRatings == Ratings.All)
                                overrideEntity.Status = OverrideRequestStatus.Granted;

                            else if (media.Rated.HasValue && ((profile.AllowedRatings & media.Rated) == media.Rated))
                                overrideEntity.Status = OverrideRequestStatus.Granted;

                            else
                                overrideEntity.Status = OverrideRequestStatus.Denied;
                        }

                        overrideEntity.State = ptoi.NewState;

                        DB.Notifications.Add(new Data.Models.Notification
                        {
                            MediaEntryId = info.MediaEntryId,
                            TitleOverrideId = overrideEntity.Id,
                            Message = $"{UserAccount.Profiles.First(item => item.Id == ptoi.ProfileId).Name} has {overrideEntity.Status.ToString().ToLower()} access to \"{media.FormattedTitle()}\"",
                            NotificationType = NotificationType.OverrideRequest,
                            ProfileId = ptoi.ProfileId,
                            Title = "Access Request",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else if(ptoi.NewState == OverrideState.Default)
                    {
                        DB.Entry(overrideEntity).State = EntityState.Deleted;
                    }
                }                
            }

            await DB.SaveChangesAsync();

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
