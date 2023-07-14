using Amazon.Runtime.Internal.Transform;
using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using DustyPig.TMDB.Models;
using K4os.Compression.LZ4.Streams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        /// <param name="initialEntriesPerRow">The server will bound any specified value into the range of 2:25</param>
        [HttpGet]
        public async Task<ResponseWrapper<HomeScreen>> HomeScreen([FromQuery] int? initialEntriesPerRow)
        {
            int take = Math.Max(2, Math.Min(DEFAULT_LIST_SIZE, initialEntriesPerRow ?? DEFAULT_LIST_SIZE));

            var ret = new HomeScreen();

            var taskDict = new Dictionary<KeyValuePair<long, string>, Task>();


            taskDict.Add
                (
                    new KeyValuePair<long, string>
                    (
                        DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING,
                        DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING_TITLE
                    ),
                    ContinueWatchingAsync(new AppDbContext(), 0, take)
               );


            taskDict.Add
                (
                    new KeyValuePair<long, string>
                    (
                        DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST,
                        DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST_TITLE
                    ),
                    WatchlistAsync(new AppDbContext(), 0, take)
                );


            taskDict.Add
                (
                    new KeyValuePair<long, string>
                    (
                        DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS,
                        DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS_TITLE
                    ),
                    PlaylistsAsync(new AppDbContext(), 0, take)
                );

            taskDict.Add
               (
                   new KeyValuePair<long, string>
                   (
                       DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED,
                       DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED_TITLE
                   ),
                   RecentlyAddedAsync(new AppDbContext(), 0, take)
               );

            taskDict.Add
               (
                   new KeyValuePair<long, string>
                   (
                       DustyPig.API.v3.Clients.MediaClient.ID_POPULAR,
                       DustyPig.API.v3.Clients.MediaClient.ID_POPULAR_TITLE
                   ),
                   PopularAsync(new AppDbContext(), 0, take)
               );


            await Task.WhenAll(taskDict.Values);


            foreach (var query in taskDict)
                if (query.Key.Key == DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS)
                {
                    var result = (query.Value as Task<List<Data.Models.Playlist>>).Result;
                    if (result.Count > 0)
                        ret.Sections.Add(new HomeScreenList
                        {
                            ListId = query.Key.Key,
                            Title = query.Key.Value,
                            Items = result.Select(item => item.ToBasicMedia()).ToList()
                        });
                }
                else if (query.Key.Key == DustyPig.API.v3.Clients.MediaClient.ID_POPULAR)
                {
                    var result = (query.Value as Task<List<MediaEntry>>).Result;
                    if (result.Count > 0)
                        ret.Sections.Add(new HomeScreenList
                        {
                            ListId = query.Key.Key,
                            Title = query.Key.Value,
                            Items = result.Take(take).Select(item => item.ToBasicMedia()).ToList()
                        });

                    var gd = new Dictionary<Genres, List<BasicMedia>>();
                    foreach (Genres g in Enum.GetValues<Genres>().Where(item => item != Genres.Unknown))
                        gd.Add(g, new List<BasicMedia>());
                    foreach (var me in result)
                    {
                        if (me.Genre_Action && gd[Genres.Action].Count < DEFAULT_LIST_SIZE) gd[Genres.Action].Add(me.ToBasicMedia());
                        if (me.Genre_Adventure && gd[Genres.Adventure].Count < DEFAULT_LIST_SIZE) gd[Genres.Adventure].Add(me.ToBasicMedia());
                        if (me.Genre_Animation && gd[Genres.Animation].Count < DEFAULT_LIST_SIZE) gd[Genres.Animation].Add(me.ToBasicMedia());
                        if (me.Genre_Anime && gd[Genres.Anime].Count < DEFAULT_LIST_SIZE) gd[Genres.Anime].Add(me.ToBasicMedia());
                        if (me.Genre_Awards_Show && gd[Genres.Awards_Show].Count < DEFAULT_LIST_SIZE) gd[Genres.Awards_Show].Add(me.ToBasicMedia());
                        if (me.Genre_Children && gd[Genres.Children].Count < DEFAULT_LIST_SIZE) gd[Genres.Children].Add(me.ToBasicMedia());
                        if (me.Genre_Comedy && gd[Genres.Comedy].Count < DEFAULT_LIST_SIZE) gd[Genres.Comedy].Add(me.ToBasicMedia());
                        if (me.Genre_Crime && gd[Genres.Crime].Count < DEFAULT_LIST_SIZE) gd[Genres.Crime].Add(me.ToBasicMedia());
                        if (me.Genre_Documentary && gd[Genres.Documentary].Count < DEFAULT_LIST_SIZE) gd[Genres.Documentary].Add(me.ToBasicMedia());
                        if (me.Genre_Drama && gd[Genres.Drama].Count < DEFAULT_LIST_SIZE) gd[Genres.Drama].Add(me.ToBasicMedia());
                        if (me.Genre_Family && gd[Genres.Family].Count < DEFAULT_LIST_SIZE) gd[Genres.Family].Add(me.ToBasicMedia());
                        if (me.Genre_Fantasy && gd[Genres.Fantasy].Count < DEFAULT_LIST_SIZE) gd[Genres.Fantasy].Add(me.ToBasicMedia());
                        if (me.Genre_Food && gd[Genres.Food].Count < DEFAULT_LIST_SIZE) gd[Genres.Food].Add(me.ToBasicMedia());
                        if (me.Genre_Game_Show && gd[Genres.Game_Show].Count < DEFAULT_LIST_SIZE) gd[Genres.Game_Show].Add(me.ToBasicMedia());
                        if (me.Genre_History && gd[Genres.History].Count < DEFAULT_LIST_SIZE) gd[Genres.History].Add(me.ToBasicMedia());
                        if (me.Genre_Home_and_Garden && gd[Genres.Home_and_Garden].Count < DEFAULT_LIST_SIZE) gd[Genres.Home_and_Garden].Add(me.ToBasicMedia());
                        if (me.Genre_Horror && gd[Genres.Horror].Count < DEFAULT_LIST_SIZE) gd[Genres.Horror].Add(me.ToBasicMedia());
                        if (me.Genre_Indie && gd[Genres.Indie].Count < DEFAULT_LIST_SIZE) gd[Genres.Indie].Add(me.ToBasicMedia());
                        if (me.Genre_Martial_Arts && gd[Genres.Martial_Arts].Count < DEFAULT_LIST_SIZE) gd[Genres.Martial_Arts].Add(me.ToBasicMedia());
                        if (me.Genre_Mini_Series && gd[Genres.Mini_Series].Count < DEFAULT_LIST_SIZE) gd[Genres.Mini_Series].Add(me.ToBasicMedia());
                        if (me.Genre_Music && gd[Genres.Music].Count < DEFAULT_LIST_SIZE) gd[Genres.Music].Add(me.ToBasicMedia());
                        if (me.Genre_Musical && gd[Genres.Musical].Count < DEFAULT_LIST_SIZE) gd[Genres.Musical].Add(me.ToBasicMedia());
                        if (me.Genre_Mystery && gd[Genres.Mystery].Count < DEFAULT_LIST_SIZE) gd[Genres.Mystery].Add(me.ToBasicMedia());
                        if (me.Genre_News && gd[Genres.News].Count < DEFAULT_LIST_SIZE) gd[Genres.News].Add(me.ToBasicMedia());
                        if (me.Genre_Podcast && gd[Genres.Podcast].Count < DEFAULT_LIST_SIZE) gd[Genres.Podcast].Add(me.ToBasicMedia());
                        if (me.Genre_Political && gd[Genres.Political].Count < DEFAULT_LIST_SIZE) gd[Genres.Political].Add(me.ToBasicMedia());
                        if (me.Genre_Reality && gd[Genres.Reality].Count < DEFAULT_LIST_SIZE) gd[Genres.Reality].Add(me.ToBasicMedia());
                        if (me.Genre_Romance && gd[Genres.Romance].Count < DEFAULT_LIST_SIZE) gd[Genres.Romance].Add(me.ToBasicMedia());
                        if (me.Genre_Science_Fiction && gd[Genres.Science_Fiction].Count < DEFAULT_LIST_SIZE) gd[Genres.Science_Fiction].Add(me.ToBasicMedia());
                        if (me.Genre_Soap && gd[Genres.Soap].Count < DEFAULT_LIST_SIZE) gd[Genres.Soap].Add(me.ToBasicMedia());
                        if (me.Genre_Sports && gd[Genres.Sports].Count < DEFAULT_LIST_SIZE) gd[Genres.Sports].Add(me.ToBasicMedia());
                        if (me.Genre_Suspense && gd[Genres.Suspense].Count < DEFAULT_LIST_SIZE) gd[Genres.Suspense].Add(me.ToBasicMedia());
                        if (me.Genre_Talk_Show && gd[Genres.Talk_Show].Count < DEFAULT_LIST_SIZE) gd[Genres.Talk_Show].Add(me.ToBasicMedia());
                        if (me.Genre_Thriller && gd[Genres.Thriller].Count < DEFAULT_LIST_SIZE) gd[Genres.Thriller].Add(me.ToBasicMedia());
                        if (me.Genre_Travel && gd[Genres.Travel].Count < DEFAULT_LIST_SIZE) gd[Genres.Travel].Add(me.ToBasicMedia());
                        if (me.Genre_TV_Movie && gd[Genres.TV_Movie].Count < DEFAULT_LIST_SIZE) gd[Genres.TV_Movie].Add(me.ToBasicMedia());
                        if (me.Genre_War && gd[Genres.War].Count < DEFAULT_LIST_SIZE) gd[Genres.War].Add(me.ToBasicMedia());
                        if (me.Genre_Western && gd[Genres.Western].Count < DEFAULT_LIST_SIZE) gd[Genres.Western].Add(me.ToBasicMedia());

                        bool doneScanning = true;
                        foreach (List<BasicMedia> lst in gd.Values)
                            if (lst.Count < DEFAULT_LIST_SIZE)
                            {
                                doneScanning = false;
                                break;
                            }
                        if (doneScanning)
                            break;
                    }

                    foreach (Genres g in gd.Keys)
                        if (gd[g].Count >= MIN_GENRE_LIST_SIZE)
                            ret.Sections.Add(new HomeScreenList
                            {
                                ListId = (long)g,
                                Title = g.AsString(),
                                Items = gd[g].Take(take).ToList()
                            });

                }
                else
                {
                    var result = (query.Value as Task<List<MediaEntry>>).Result;
                    if (result.Count > 0)
                        ret.Sections.Add(new HomeScreenList
                        {
                            ListId = query.Key.Key,
                            Title = query.Key.Value,
                            Items = result.Select(item => item.ToBasicMedia()).ToList()
                        });
                }

            ret.Sections.Sort((x, y) => x.ListId.CompareTo(y.ListId));
            return new ResponseWrapper<HomeScreen>(ret);
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns more items for the specified home screen list based on start position</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<List<BasicMedia>>> LoadMoreHomeScreenItems(HomeScreenListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<List<BasicMedia>>(ex.ToString()); }

            var results = new List<BasicMedia>();

            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING)
                results = (await ContinueWatchingAsync(DB, request.Start, DEFAULT_LIST_SIZE)).Select(item => item.ToBasicMedia()).ToList();

            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST)
                results = (await WatchlistAsync(DB, request.Start, DEFAULT_LIST_SIZE)).Select(item => item.ToBasicMedia()).ToList();


            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED)
                results = (await RecentlyAddedAsync(DB, request.Start, DEFAULT_LIST_SIZE)).Select(item => item.ToBasicMedia()).ToList();


            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS)
                results = (await PlaylistsAsync(DB, request.Start, DEFAULT_LIST_SIZE)).Select(item => item.ToBasicMedia()).ToList();


            if (request.ListId == DustyPig.API.v3.Clients.MediaClient.ID_POPULAR)
                results = (await PopularAsync(DB, request.Start, DEFAULT_LIST_SIZE)).Select(item => item.ToBasicMedia()).ToList();

            //Genres
            if (request.ListId > 0 && request.ListId <= Enum.GetValues(typeof(Genres)).Cast<long>().Max())
                results = (await GenresAsync(DB, (Genres)request.ListId, request.Start, DEFAULT_LIST_SIZE, SortOrder.Popularity_Descending)).Select(item => item.ToBasicMedia()).ToList();

            return new ResponseWrapper<List<BasicMedia>>(results);
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 items in a library based on start position and sort order</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<List<BasicMedia>>> ListLibraryItems(LibraryListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<List<BasicMedia>>(ex.ToString()); }

            var q =
                from me in DB.MediaEntries
         
                join lib in DB.Libraries 
                    on new { me.LibraryId, AccountId = UserAccount.Id }
                    equals new { LibraryId = lib.Id, lib.AccountId }
                             
                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    new MediaTypes[] { MediaTypes.Movie, MediaTypes.Series }.Contains(me.EntryType)

                    && lib.Id == request.LibraryId

                    &&
                    (
                        // Watch permissions
                        UserProfile.IsMain
                        || ovrride.State == OverrideState.Allow
                        ||
                        (
                            ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                    )

                select me;

            var entries = await q
                .AsNoTracking()
                .ApplySortOrder(request.Sort)
                .Skip(request.Start)
                .Take(DEFAULT_LIST_SIZE)
                .ToListAsync();

            return new ResponseWrapper<List<BasicMedia>>(entries.Select(item => item.ToBasicMedia()).ToList());
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 items in a Genre based on start position and sort order</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<List<BasicMedia>>> ListGenreItems(GenreListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<List<BasicMedia>>(ex.ToString()); }

            var entries = await GenresAsync(DB, request.Genre, request.Start, DEFAULT_LIST_SIZE, request.Sort);

            return new ResponseWrapper<List<BasicMedia>>(entries.Select(item => item.ToBasicMedia()).ToList());
        }



        /// <summary>
        /// Level 2
        /// </summary>
        /// <param name="request">
        /// Url encoded title to search for
        /// </param>
        [HttpPost]
        public async Task<ResponseWrapper<SearchResults>> Search(SearchRequest request, CancellationToken cancellationToken)
        {
            var ret = new SearchResults();

            if (string.IsNullOrWhiteSpace(request.Query))
                return new ResponseWrapper<SearchResults>(ret);

            request.Query = StringUtils.NormalizedQueryString(request.Query);
            if (string.IsNullOrWhiteSpace(request.Query))
                return new ResponseWrapper<SearchResults>(ret);

            var terms = request.Query.Tokenize();




            /****************************************
             * Get available items
             ****************************************/
            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join fls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in DB.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to view filters
                    Constants.TOP_LEVEL_MEDIA_TYPES.Contains(me.EntryType)
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                || fls.HasValue
                            )
                        )
                        ||
                        (
                            pls != null
                            &&
                            (
                                UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled
                                || ovrride.State != OverrideState.Block
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )

                select me;

            foreach (var term in terms)
                q =
                    from me in q
                    join msb in DB.MediaSearchBridges
                        .Where(item => item.SearchTerm.Term.Contains(term))
                        on me.Id equals msb.MediaEntryId
                    select me;

            var mediaEntries = await q
                .AsNoTracking()
                .Distinct()
                .Take(MAX_DB_lIST_sIZE)
                .ToListAsync(cancellationToken);


            //Search sort
            mediaEntries.Sort((x, y) =>
            {
                int ret = -x.QueryTitle.ICEquals(request.Query).CompareTo(y.QueryTitle.ICEquals(request.Query));
                if (ret == 0 && x.QueryTitle.ICEquals(y.QueryTitle))
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                if (ret == 0)
                    ret = -x.QueryTitle.ICStartsWith(request.Query).CompareTo(y.QueryTitle.ICStartsWith(request.Query));
                if (ret == 0)
                    ret = -x.QueryTitle.ICContains(request.Query).CompareTo(y.QueryTitle.ICContains(request.Query));
                if (ret == 0)
                    ret = x.SortTitle.CompareTo(y.SortTitle);
                if (ret == 0)
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                return ret;
            });

            ret.Available.AddRange(mediaEntries.Select(item => item.ToBasicMedia()).Take(DEFAULT_LIST_SIZE));


            /****************************************
             * Search online databases
             ****************************************/
            ret.OtherTitlesAllowed = UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled;

            if (request.SearchTMDB && UserAccount.Id != TestAccount.AccountId && ret.OtherTitlesAllowed)
            {
                var response = await _tmdbClient.SearchAsync(request.Query, cancellationToken);
                if (response.Success)
                    ret.OtherTitles.AddRange(response.Data.Select(item => item.ToBasicTMDBInfo()).Take(DEFAULT_LIST_SIZE));
            }

            return new ResponseWrapper<SearchResults>(ret);
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper> AddToWatchlist(int id)
        {
            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join fls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in DB.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                join wli in DB.WatchListItems
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id }
                    equals new { wli.MediaEntryId, wli.ProfileId }
                    into wli_lj
                from wli in wli_lj.DefaultIfEmpty()

                where

                    me.Id == id

                    //Allow to play filters
                    && new MediaTypes[] { MediaTypes.Movie, MediaTypes.Series }.Contains(me.EntryType)
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )


                select new
                {
                    MediaEntry = me,
                    InWatchList = wli != null
                };


            var mediaEntry = await q
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound(nameof(id));

            if (!mediaEntry.InWatchList)
            {
                DB.WatchListItems.Add(new WatchlistItem
                {
                    Added = DateTime.UtcNow,
                    MediaEntryId = id,
                    ProfileId = UserProfile.Id
                });
                    
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ResponseWrapper> DeleteFromWatchlist(int id)
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

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper> UpdatePlaybackProgress(PlaybackProgress hist)
        {
            if (hist == null)
                return CommonResponses.NotFound();

            if (hist.Id <= 0)
                return CommonResponses.NotFound(nameof(hist.Id));


            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join fls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in DB.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.EntryType == MediaTypes.Episode ? me.LinkedToId.Value : me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                join pmp in DB.ProfileMediaProgresses
                    on new { MediaEntryId = me.EntryType == MediaTypes.Episode ? me.LinkedToId.Value : me.Id, ProfileId = UserProfile.Id }
                    equals new { pmp.MediaEntryId, pmp.ProfileId } into pmp_lj
                from pmp in pmp_lj

                where

                    me.Id == hist.Id

                    //Allow to play filters
                    && Constants.PLAYABLE_MEDIA_TYPES.Contains(me.EntryType)
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Episode
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )

                select new
                {
                    MediaEntryId = me.EntryType == MediaTypes.Episode ? me.LinkedToId.Value : me.Id,
                    EntryType = me.EntryType,
                    Xid = me.Xid,
                    Progress = pmp,
                    me.CreditsStartTime,
                    me.Length
                };

            
            var response = await q
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (response == null)
                return CommonResponses.NotFound(nameof(hist.Id));

            
            if (response.Progress == null)
            {
                if(response.EntryType == MediaTypes.Movie)
                    if (hist.Seconds < 1 || hist.Seconds > (response.CreditsStartTime ?? (response.Length * 0.9)))
                        return CommonResponses.Ok();

                //Add
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = response.MediaEntryId,
                    ProfileId = UserProfile.Id,
                    Played = Math.Max(0, hist.Seconds),
                    Timestamp = DateTime.UtcNow,
                    Xid = response.Xid
                });
            }
            else
            {
                if (response.EntryType == MediaTypes.Movie)
                {   if (hist.Seconds < 1 || hist.Seconds > (response.CreditsStartTime ?? (response.Length * 0.9)))
                        DB.ProfileMediaProgresses.Remove(response.Progress);
                }
                else
                {
                    //Update
                    response.Progress.Played = Math.Max(0, hist.Seconds);
                    response.Progress.Timestamp = DateTime.UtcNow;
                    DB.ProfileMediaProgresses.Update(response.Progress);
                }
            }

            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        public async Task<ResponseWrapper<SimpleValue<Ratings>>> GetAllAvailableRatings()
        {
            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join fls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in DB.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    new MediaTypes[] { MediaTypes.Movie, MediaTypes.Series }.Contains(me.EntryType)
                    &&
                    (
                        ovrride.State == OverrideState.Allow
                        ||
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                    )


                select new
                {
                    me.EntryType,
                    me.MovieRating,
                    me.TVRating,
                };

            var response = await q.AsNoTracking().Distinct().ToListAsync();

            Ratings ret = Ratings.None;
            foreach (var item in response)
                if (item.EntryType == MediaTypes.Movie)
                {
                    switch (item.MovieRating)
                    {
                        case MovieRatings.G:
                            ret |= Ratings.G;
                            break;

                        case MovieRatings.PG:
                            ret |= Ratings.PG;
                            break;

                        case MovieRatings.PG_13:
                            ret |= Ratings.PG_13;
                            break;

                        case MovieRatings.R:
                            ret |= Ratings.R;
                            break;

                        case MovieRatings.NC_17:
                            ret |= Ratings.NC_17;
                            break;

                        case MovieRatings.Unrated:
                            ret |= Ratings.Unrated;
                            break;

                        case MovieRatings.NotRated:
                            ret |= Ratings.NR;
                            break;
                    }
                }
                else
                {
                    switch (item.TVRating)
                    {
                        case TVRatings.Y:
                            ret |= Ratings.TV_Y;
                            break;

                        case TVRatings.Y7:
                            ret |= Ratings.TV_Y7;
                            break;

                        case TVRatings.G:
                            ret |= Ratings.TV_G;
                            break;

                        case TVRatings.PG:
                            ret |= Ratings.TV_PG;
                            break;

                        case TVRatings.TV_14:
                            ret |= Ratings.TV_14;
                            break;

                        case TVRatings.MA:
                            ret |= Ratings.TV_MA;
                            break;

                        case TVRatings.Unrated:
                            ret |= Ratings.Unrated;
                            break;

                        case TVRatings.NotRated:
                            ret |= Ratings.NR;
                            break;
                    }
                }

            return new ResponseWrapper<SimpleValue<Ratings>>(new SimpleValue<Ratings>(ret));
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        public async Task<ResponseWrapper<SimpleValue<Genres>>> GetAllAvailableGenres()
        {
            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join fls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in DB.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    new MediaTypes[] { MediaTypes.Movie, MediaTypes.Series }.Contains(me.EntryType)
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )


                group me by me.LibraryId into g

                select new
                {
                    g.Key,
                    Genre_Action = g.Max(item => item.Genre_Action),
                    Genre_Adventure = g.Max(item => item.Genre_Adventure),
                    Genre_Animation = g.Max(item => item.Genre_Animation),
                    Genre_Anime = g.Max(item => item.Genre_Anime),
                    Genre_Awards_Show = g.Max(item => item.Genre_Awards_Show),
                    Genre_Children = g.Max(item => item.Genre_Children),
                    Genre_Comedy = g.Max(item => item.Genre_Comedy),
                    Genre_Crime = g.Max(item => item.Genre_Crime),
                    Genre_Documentary = g.Max(item => item.Genre_Documentary),
                    Genre_Drama = g.Max(item => item.Genre_Drama),
                    Genre_Family = g.Max(item => item.Genre_Family),
                    Genre_Fantasy = g.Max(item => item.Genre_Fantasy),
                    Genre_Food = g.Max(item => item.Genre_Food),
                    Genre_Game_Show = g.Max(item => item.Genre_Game_Show),
                    Genre_History = g.Max(item => item.Genre_History),
                    Genre_Home_and_Garden = g.Max(item => item.Genre_Home_and_Garden),
                    Genre_Horror = g.Max(item => item.Genre_Horror),
                    Genre_Indie = g.Max(item => item.Genre_Indie),
                    Genre_Martial_Arts = g.Max(item => item.Genre_Martial_Arts),
                    Genre_Mini_Series = g.Max(item => item.Genre_Mini_Series),
                    Genre_Music = g.Max(item => item.Genre_Music),
                    Genre_Musical = g.Max(item => item.Genre_Musical),
                    Genre_Mystery = g.Max(item => item.Genre_Mystery),
                    Genre_News = g.Max(item => item.Genre_News),
                    Genre_Podcast = g.Max(item => item.Genre_Podcast),
                    Genre_Political = g.Max(item => item.Genre_Political),
                    Genre_Reality = g.Max(item => item.Genre_Reality),
                    Genre_Romance = g.Max(item => item.Genre_Romance),
                    Genre_Science_Fiction = g.Max(item => item.Genre_Science_Fiction),
                    Genre_Soap = g.Max(item => item.Genre_Soap),
                    Genre_Sports = g.Max(item => item.Genre_Sports),
                    Genre_Suspense = g.Max(item => item.Genre_Suspense),
                    Genre_Talk_Show = g.Max(item => item.Genre_Talk_Show),
                    Genre_Thriller = g.Max(item => item.Genre_Thriller),
                    Genre_Travel = g.Max(item => item.Genre_Travel),
                    Genre_TV_Movie = g.Max(item => item.Genre_TV_Movie),
                    Genre_War = g.Max(item => item.Genre_War),
                    Genre_Western = g.Max(item => item.Genre_Western)
                };

            var response = await q
                .AsNoTracking()
                .ToListAsync();

            long ret = 0;
            foreach(var item in response)
            {
                if (item.Genre_Action) ret |= (long)Genres.Action;
                if (item.Genre_Adventure) ret |= (long)Genres.Adventure;
                if (item.Genre_Animation) ret |= (long)Genres.Animation;
                if (item.Genre_Anime) ret |= (long)Genres.Anime;
                if (item.Genre_Awards_Show) ret |= (long)Genres.Awards_Show;
                if (item.Genre_Children) ret |= (long)Genres.Children;
                if (item.Genre_Comedy) ret |= (long)Genres.Comedy;
                if (item.Genre_Crime) ret |= (long)Genres.Crime;
                if (item.Genre_Documentary) ret |= (long)Genres.Documentary;
                if (item.Genre_Drama) ret |= (long)Genres.Drama;
                if (item.Genre_Family) ret |= (long)Genres.Family;
                if (item.Genre_Fantasy) ret |= (long)Genres.Fantasy;
                if (item.Genre_Food) ret |= (long)Genres.Food;
                if (item.Genre_Game_Show) ret |= (long)Genres.Game_Show;
                if (item.Genre_History) ret |= (long)Genres.History;
                if (item.Genre_Home_and_Garden) ret |= (long)Genres.Home_and_Garden;
                if (item.Genre_Horror) ret |= (long)Genres.Horror;
                if (item.Genre_Indie) ret |= (long)Genres.Indie;
                if (item.Genre_Martial_Arts) ret |= (long)Genres.Martial_Arts;
                if (item.Genre_Mini_Series) ret |= (long)Genres.Mini_Series;
                if (item.Genre_Music) ret |= (long)Genres.Music;
                if (item.Genre_Musical) ret |= (long)Genres.Musical;
                if (item.Genre_Mystery) ret |= (long)Genres.Mystery;
                if (item.Genre_News) ret |= (long)Genres.News;
                if (item.Genre_Podcast) ret |= (long)Genres.Podcast;
                if (item.Genre_Political) ret |= (long)Genres.Political;
                if (item.Genre_Reality) ret |= (long)Genres.Reality;
                if (item.Genre_Romance) ret |= (long)Genres.Romance;
                if (item.Genre_Science_Fiction) ret |= (long)Genres.Science_Fiction;
                if (item.Genre_Soap) ret |= (long)Genres.Soap;
                if (item.Genre_Sports) ret |= (long)Genres.Sports;
                if (item.Genre_Suspense) ret |= (long)Genres.Suspense;
                if (item.Genre_Talk_Show) ret |= (long)Genres.Talk_Show;
                if (item.Genre_Thriller) ret |= (long)Genres.Thriller;
                if (item.Genre_Travel) ret |= (long)Genres.Travel;
                if (item.Genre_TV_Movie) ret |= (long)Genres.TV_Movie;
                if (item.Genre_War) ret |= (long)Genres.War;
                if (item.Genre_Western) ret |= (long)Genres.Western;
            }

            return new ResponseWrapper<SimpleValue<Genres>>(new SimpleValue<Genres>((Genres)ret));
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper<List<BasicMedia>>> Explore(ExploreRequest request)
        {
            //This is the most insane query yet - good luck with EF's query cache.
            //Based on request options, this takes 90ms to 350ms
            //I can't figure out how to optimize it further

            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<List<BasicMedia>>(ex.ToString()); }

            var entryTypes = new List<MediaTypes>();
            if (request.ReturnSeries)
                entryTypes.Add(MediaTypes.Series);
            if (request.ReturnMovies)
                entryTypes.Add(MediaTypes.Movie);

            if (request.FilterOnGenres == null)
            {
                long allG = 0;
                foreach (long val in Enum.GetValues<Genres>().Select(v => (long)v))
                    allG |= val;
                request.FilterOnGenres = (Genres)allG;
            }
            long g = (long)request.FilterOnGenres;


            if (request.FilterOnRatings == null)
                request.FilterOnRatings = Ratings.All;

            if (!request.ReturnMovies)
                foreach (var r in RatingsUtils.TVRatings)
                    if (r != Ratings.None)
                        request.FilterOnRatings &= ~r;

            if (!request.ReturnSeries)
                foreach (var r in RatingsUtils.MovieRatings)
                    if (r != Ratings.None)
                        request.FilterOnRatings &= ~r;

            MovieRatings maxMovieRating = (MovieRatings)0;
            if ((request.FilterOnRatings & Ratings.G) == Ratings.G) maxMovieRating = MovieRatings.G;
            if ((request.FilterOnRatings & Ratings.PG) == Ratings.PG) maxMovieRating = MovieRatings.PG;
            if ((request.FilterOnRatings & Ratings.PG_13) == Ratings.PG_13) maxMovieRating = MovieRatings.PG_13;
            if ((request.FilterOnRatings & Ratings.R) == Ratings.R) maxMovieRating = MovieRatings.R;
            if ((request.FilterOnRatings & Ratings.NC_17) == Ratings.NC_17) maxMovieRating = MovieRatings.NC_17;
            if ((request.FilterOnRatings & Ratings.Unrated) == Ratings.Unrated) maxMovieRating = MovieRatings.Unrated;
            if ((request.FilterOnRatings & Ratings.NR) == Ratings.NR) maxMovieRating = MovieRatings.NotRated;
            if (request.IncludeNoneRatings)
                maxMovieRating = MovieRatings.NotRated;


            TVRatings maxTVRating = (TVRatings)0;
            if ((request.FilterOnRatings & Ratings.TV_Y) == Ratings.TV_Y) maxTVRating = TVRatings.Y;
            if ((request.FilterOnRatings & Ratings.TV_Y7) == Ratings.TV_Y7) maxTVRating = TVRatings.Y7;
            if ((request.FilterOnRatings & Ratings.TV_G) == Ratings.TV_G) maxTVRating = TVRatings.G;
            if ((request.FilterOnRatings & Ratings.TV_PG) == Ratings.TV_PG) maxTVRating = TVRatings.PG;
            if ((request.FilterOnRatings & Ratings.TV_14) == Ratings.TV_14) maxTVRating = TVRatings.TV_14;
            if ((request.FilterOnRatings & Ratings.TV_MA) == Ratings.TV_MA) maxTVRating = TVRatings.MA;
            if ((request.FilterOnRatings & Ratings.Unrated) == Ratings.Unrated) maxTVRating = TVRatings.Unrated;
            if ((request.FilterOnRatings & Ratings.NR) == Ratings.NR) maxTVRating = TVRatings.NotRated;
            if (request.IncludeNoneRatings)
                maxTVRating = TVRatings.NotRated;




            if (request.LibraryIds == null)
                request.LibraryIds = new();


            var q =
                from me in DB.MediaEntries

                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join fls in DB.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in DB.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()


                join ovrride in DB.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                let genreUnknown =
                (
                    me.Genre_Action == false
                    && me.Genre_Adventure == false
                    && me.Genre_Animation == false
                    && me.Genre_Anime == false
                    && me.Genre_Awards_Show == false
                    && me.Genre_Children == false
                    && me.Genre_Comedy == false
                    && me.Genre_Crime == false
                    && me.Genre_Documentary == false
                    && me.Genre_Drama == false
                    && me.Genre_Family == false
                    && me.Genre_Fantasy == false
                    && me.Genre_Food == false
                    && me.Genre_Game_Show == false
                    && me.Genre_History == false
                    && me.Genre_Home_and_Garden == false
                    && me.Genre_Horror == false
                    && me.Genre_Indie == false
                    && me.Genre_Martial_Arts == false
                    && me.Genre_Mini_Series == false
                    && me.Genre_Music == false
                    && me.Genre_Musical == false
                    && me.Genre_Mystery == false
                    && me.Genre_News == false
                    && me.Genre_Podcast == false
                    && me.Genre_Political == false
                    && me.Genre_Reality == false
                    && me.Genre_Romance == false
                    && me.Genre_Science_Fiction == false
                    && me.Genre_Soap == false
                    && me.Genre_Sports == false
                    && me.Genre_Suspense == false
                    && me.Genre_Talk_Show == false
                    && me.Genre_Thriller == false
                    && me.Genre_Travel == false
                    && me.Genre_TV_Movie == false
                    && me.Genre_War == false
                    && me.Genre_Western == false
                )

                where


                    entryTypes.Contains(me.EntryType)

                    //If this isn't in parenthases, then the above statement entryTypes.Contains(me.EntryType) is ignored when generating the sql
                    && (request.LibraryIds.Count > 0 ? request.LibraryIds.Contains(me.LibraryId) : me.LibraryId >= 0)

                    &&
                    (
                        (request.ReturnMovies && me.EntryType == MediaTypes.Movie && (me.MovieRating ?? MovieRatings.NotRated) <= maxMovieRating)
                        ||
                        (request.ReturnSeries && me.EntryType == MediaTypes.Series && (me.TVRating ?? TVRatings.NotRated) <= maxTVRating)
                    )

                    &&
                    (
                        // So far this is the most optimized I know how to do this
                        // Using this big old if then else expression simplifies the query if only 1 genre is flagged,
                        // and also handles Genres.Unknown.
                        request.FilterOnGenres == Genres.Unknown ? genreUnknown :

                        request.FilterOnGenres == Genres.Action && request.IncludeUnknownGenres ? me.Genre_Action || genreUnknown :
                        request.FilterOnGenres == Genres.Action ? me.Genre_Action :

                        request.FilterOnGenres == Genres.Animation && request.IncludeUnknownGenres ? me.Genre_Adventure || genreUnknown :
                        request.FilterOnGenres == Genres.Animation ? me.Genre_Adventure :

                        request.FilterOnGenres == Genres.Anime && request.IncludeUnknownGenres ? me.Genre_Anime || genreUnknown :
                        request.FilterOnGenres == Genres.Anime ? me.Genre_Anime :

                        request.FilterOnGenres == Genres.Awards_Show && request.IncludeUnknownGenres ? me.Genre_Awards_Show || genreUnknown :
                        request.FilterOnGenres == Genres.Awards_Show ? me.Genre_Awards_Show :

                        request.FilterOnGenres == Genres.Children && request.IncludeUnknownGenres ? me.Genre_Children || genreUnknown :
                        request.FilterOnGenres == Genres.Children ? me.Genre_Children :

                        request.FilterOnGenres == Genres.Comedy && request.IncludeUnknownGenres ? me.Genre_Comedy || genreUnknown :
                        request.FilterOnGenres == Genres.Comedy ? me.Genre_Comedy :

                        request.FilterOnGenres == Genres.Crime && request.IncludeUnknownGenres ? me.Genre_Crime || genreUnknown :
                        request.FilterOnGenres == Genres.Crime ? me.Genre_Crime :

                        request.FilterOnGenres == Genres.Documentary && request.IncludeUnknownGenres ? me.Genre_Documentary || genreUnknown :
                        request.FilterOnGenres == Genres.Documentary ? me.Genre_Documentary :

                        request.FilterOnGenres == Genres.Drama && request.IncludeUnknownGenres ? me.Genre_Drama || genreUnknown :
                        request.FilterOnGenres == Genres.Drama ? me.Genre_Drama :

                        request.FilterOnGenres == Genres.Family && request.IncludeUnknownGenres ? me.Genre_Family || genreUnknown :
                        request.FilterOnGenres == Genres.Family ? me.Genre_Family :

                        request.FilterOnGenres == Genres.Fantasy && request.IncludeUnknownGenres ? me.Genre_Family || genreUnknown :
                        request.FilterOnGenres == Genres.Fantasy ? me.Genre_Family :

                        request.FilterOnGenres == Genres.Food && request.IncludeUnknownGenres ? me.Genre_Food || genreUnknown :
                        request.FilterOnGenres == Genres.Food ? me.Genre_Food :

                        request.FilterOnGenres == Genres.Game_Show && request.IncludeUnknownGenres ? me.Genre_Game_Show || genreUnknown :
                        request.FilterOnGenres == Genres.Game_Show ? me.Genre_Game_Show :

                        request.FilterOnGenres == Genres.History && request.IncludeUnknownGenres ? me.Genre_History || genreUnknown :
                        request.FilterOnGenres == Genres.History ? me.Genre_History :

                        request.FilterOnGenres == Genres.Home_and_Garden && request.IncludeUnknownGenres ? me.Genre_Home_and_Garden || genreUnknown :
                        request.FilterOnGenres == Genres.Home_and_Garden ? me.Genre_Home_and_Garden :

                        request.FilterOnGenres == Genres.Horror && request.IncludeUnknownGenres ? me.Genre_Horror || genreUnknown :
                        request.FilterOnGenres == Genres.Horror ? me.Genre_Horror :

                        request.FilterOnGenres == Genres.Indie && request.IncludeUnknownGenres ? me.Genre_Indie || genreUnknown :
                        request.FilterOnGenres == Genres.Indie ? me.Genre_Indie :

                        request.FilterOnGenres == Genres.Martial_Arts && request.IncludeUnknownGenres ? me.Genre_Martial_Arts || genreUnknown :
                        request.FilterOnGenres == Genres.Martial_Arts ? me.Genre_Martial_Arts :

                        request.FilterOnGenres == Genres.Mini_Series && request.IncludeUnknownGenres ? me.Genre_Mini_Series || genreUnknown :
                        request.FilterOnGenres == Genres.Mini_Series ? me.Genre_Mini_Series :

                        request.FilterOnGenres == Genres.Music && request.IncludeUnknownGenres ? me.Genre_Music || genreUnknown :
                        request.FilterOnGenres == Genres.Music ? me.Genre_Music :

                        request.FilterOnGenres == Genres.Musical && request.IncludeUnknownGenres ? me.Genre_Musical || genreUnknown :
                        request.FilterOnGenres == Genres.Musical ? me.Genre_Musical :

                        request.FilterOnGenres == Genres.Mystery && request.IncludeUnknownGenres ? me.Genre_Mystery || genreUnknown :
                        request.FilterOnGenres == Genres.Mystery ? me.Genre_Mystery :

                        request.FilterOnGenres == Genres.News && request.IncludeUnknownGenres ? me.Genre_News || genreUnknown :
                        request.FilterOnGenres == Genres.News ? me.Genre_News :

                        request.FilterOnGenres == Genres.Podcast && request.IncludeUnknownGenres ? me.Genre_Podcast || genreUnknown :
                        request.FilterOnGenres == Genres.Podcast ? me.Genre_Podcast :

                        request.FilterOnGenres == Genres.Political && request.IncludeUnknownGenres ? me.Genre_Political || genreUnknown :
                        request.FilterOnGenres == Genres.Political ? me.Genre_Political :

                        request.FilterOnGenres == Genres.Reality && request.IncludeUnknownGenres ? me.Genre_Reality || genreUnknown :
                        request.FilterOnGenres == Genres.Reality ? me.Genre_Reality :

                        request.FilterOnGenres == Genres.Romance && request.IncludeUnknownGenres ? me.Genre_Romance || genreUnknown :
                        request.FilterOnGenres == Genres.Romance ? me.Genre_Romance :

                        request.FilterOnGenres == Genres.Science_Fiction && request.IncludeUnknownGenres ? me.Genre_Science_Fiction || genreUnknown :
                        request.FilterOnGenres == Genres.Science_Fiction ? me.Genre_Science_Fiction :

                        request.FilterOnGenres == Genres.Soap && request.IncludeUnknownGenres ? me.Genre_Soap || genreUnknown :
                        request.FilterOnGenres == Genres.Soap ? me.Genre_Soap :

                        request.FilterOnGenres == Genres.Sports && request.IncludeUnknownGenres ? me.Genre_Sports || genreUnknown :
                        request.FilterOnGenres == Genres.Sports ? me.Genre_Sports :

                        request.FilterOnGenres == Genres.Suspense && request.IncludeUnknownGenres ? me.Genre_Suspense || genreUnknown :
                        request.FilterOnGenres == Genres.Suspense ? me.Genre_Suspense :

                        request.FilterOnGenres == Genres.Talk_Show && request.IncludeUnknownGenres ? me.Genre_Talk_Show || genreUnknown :
                        request.FilterOnGenres == Genres.Talk_Show ? me.Genre_Talk_Show :

                        request.FilterOnGenres == Genres.Thriller && request.IncludeUnknownGenres ? me.Genre_Thriller || genreUnknown :
                        request.FilterOnGenres == Genres.Thriller ? me.Genre_Thriller :

                        request.FilterOnGenres == Genres.Travel && request.IncludeUnknownGenres ? me.Genre_Travel || genreUnknown :
                        request.FilterOnGenres == Genres.Travel ? me.Genre_Travel :

                        request.FilterOnGenres == Genres.TV_Movie && request.IncludeUnknownGenres ? me.Genre_TV_Movie || genreUnknown :
                        request.FilterOnGenres == Genres.TV_Movie ? me.Genre_TV_Movie :

                        request.FilterOnGenres == Genres.War && request.IncludeUnknownGenres ? me.Genre_War || genreUnknown :
                        request.FilterOnGenres == Genres.War ? me.Genre_War :

                        request.FilterOnGenres == Genres.Western && request.IncludeUnknownGenres ? me.Genre_Western || genreUnknown :
                        request.FilterOnGenres == Genres.Western ? me.Genre_Western :

                        (
                            // genre is multiple flags.
                            // If it's a valid combo of flags (like Genres.Action | Genres.Western), this will return the correct rows
                            // If it's an invalid flag (like -1), this will return zero rows
                            (request.IncludeUnknownGenres && genreUnknown)

                            || (((g & (long)Genres.Action) == (long)Genres.Action) && me.Genre_Action)
                            || (((g & (long)Genres.Adventure) == (long)Genres.Adventure) && me.Genre_Adventure)
                            || (((g & (long)Genres.Animation) == (long)Genres.Animation) && me.Genre_Animation)
                            || (((g & (long)Genres.Anime) == (long)Genres.Anime) && me.Genre_Anime)
                            || (((g & (long)Genres.Awards_Show) == (long)Genres.Awards_Show) && me.Genre_Awards_Show)
                            || (((g & (long)Genres.Children) == (long)Genres.Children) && me.Genre_Children)
                            || (((g & (long)Genres.Comedy) == (long)Genres.Comedy) && me.Genre_Comedy)
                            || (((g & (long)Genres.Crime) == (long)Genres.Crime) && me.Genre_Crime)
                            || (((g & (long)Genres.Documentary) == (long)Genres.Documentary) && me.Genre_Documentary)
                            || (((g & (long)Genres.Drama) == (long)Genres.Drama) && me.Genre_Drama)
                            || (((g & (long)Genres.Family) == (long)Genres.Family) && me.Genre_Family)
                            || (((g & (long)Genres.Fantasy) == (long)Genres.Fantasy) && me.Genre_Fantasy)
                            || (((g & (long)Genres.Food) == (long)Genres.Food) && me.Genre_Food)
                            || (((g & (long)Genres.Game_Show) == (long)Genres.Game_Show) && me.Genre_Game_Show)
                            || (((g & (long)Genres.History) == (long)Genres.History) && me.Genre_History)
                            || (((g & (long)Genres.Home_and_Garden) == (long)Genres.Home_and_Garden) && me.Genre_Home_and_Garden)
                            || (((g & (long)Genres.Horror) == (long)Genres.Horror) && me.Genre_Horror)
                            || (((g & (long)Genres.Indie) == (long)Genres.Indie) && me.Genre_Indie)
                            || (((g & (long)Genres.Martial_Arts) == (long)Genres.Martial_Arts) && me.Genre_Martial_Arts)
                            || (((g & (long)Genres.Mini_Series) == (long)Genres.Mini_Series) && me.Genre_Mini_Series)
                            || (((g & (long)Genres.Music) == (long)Genres.Music) && me.Genre_Music)
                            || (((g & (long)Genres.Musical) == (long)Genres.Musical) && me.Genre_Musical)
                            || (((g & (long)Genres.Mystery) == (long)Genres.Mystery) && me.Genre_Mystery)
                            || (((g & (long)Genres.News) == (long)Genres.News) && me.Genre_News)
                            || (((g & (long)Genres.Podcast) == (long)Genres.Podcast) && me.Genre_Podcast)
                            || (((g & (long)Genres.Political) == (long)Genres.Political) && me.Genre_Political)
                            || (((g & (long)Genres.Reality) == (long)Genres.Reality) && me.Genre_Reality)
                            || (((g & (long)Genres.Romance) == (long)Genres.Romance) && me.Genre_Romance)
                            || (((g & (long)Genres.Science_Fiction) == (long)Genres.Science_Fiction) && me.Genre_Science_Fiction)
                            || (((g & (long)Genres.Soap) == (long)Genres.Soap) && me.Genre_Soap)
                            || (((g & (long)Genres.Sports) == (long)Genres.Sports) && me.Genre_Sports)
                            || (((g & (long)Genres.Suspense) == (long)Genres.Suspense) && me.Genre_Suspense)
                            || (((g & (long)Genres.Talk_Show) == (long)Genres.Talk_Show) && me.Genre_Talk_Show)
                            || (((g & (long)Genres.Thriller) == (long)Genres.Thriller) && me.Genre_Thriller)
                            || (((g & (long)Genres.Travel) == (long)Genres.Travel) && me.Genre_Travel)
                            || (((g & (long)Genres.TV_Movie) == (long)Genres.TV_Movie) && me.Genre_TV_Movie)
                            || (((g & (long)Genres.War) == (long)Genres.War) && me.Genre_War)
                            || (((g & (long)Genres.Western) == (long)Genres.Western) && me.Genre_Western)
                        )
                    )
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && UserProfile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                        || ovrride.State == OverrideState.Allow
                    )


                select me;

            var ret = await q
                .AsNoTracking()
                .ApplySortOrder(request.Sort)
                .Skip(request.Start)
                .Take(DEFAULT_LIST_SIZE)
                .ToListAsync();

            return new ResponseWrapper<List<BasicMedia>>(ret.Select(item => item.ToBasicMedia()).ToList());
        }





        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper> RequestAccessOverride(int id)
        {
            if (!UserProfile.IsMain && UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                return CommonResponses.Forbid();



            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
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
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))
                .ThenInclude(item => item.Profile)
                .Include(item => item.TitleOverrides.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))
                .Where(item => item.Id == id)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound(nameof(id));



            //Does this profile alredy have access?
            var existingOverride = mediaEntry.TitleOverrides
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.State == OverrideState.Allow || item.State == OverrideState.Block)
                .FirstOrDefault();

            if (UserProfile.IsMain)
            {
                //Main profile can already access ALL owned media
                if (mediaEntry.Library.AccountId == UserAccount.Id)
                    return CommonResponses.Ok();

                if (mediaEntry.Library.FriendLibraryShares.Any())
                {
                    //For libs shared with the main profile, they can access anything not specifically blocked
                    if (existingOverride == null)
                        return CommonResponses.Ok();

                    if (existingOverride.State == OverrideState.Block)
                        return CommonResponses.Forbid($"Access to this {mediaEntry.EntryType.ToString().ToLower()} has been blocked by the owner");
                    return CommonResponses.Ok();
                }
            }
            else
            {
                if (existingOverride == null)
                {
                    if(mediaEntry.Library.ProfileLibraryShares.Any(item => item.ProfileId == UserProfile.Id))
                    {
                        if (mediaEntry.EntryType == MediaTypes.Movie && mediaEntry.MovieRating <= UserProfile.MaxMovieRating)
                            return CommonResponses.Ok();
                        if (mediaEntry.EntryType == MediaTypes.Series && mediaEntry.TVRating <= UserProfile.MaxTVRating)
                            return CommonResponses.Ok();
                    }
                }
                else
                {
                    if (existingOverride.State == OverrideState.Block)
                        return CommonResponses.Forbid($"Access to this {mediaEntry.EntryType.ToString().ToLower()} has been blocked by the owner");
                }
            }

            //Can this profile view it to make a request?
            existingOverride = mediaEntry.TitleOverrides
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefault();

            if (UserProfile.IsMain)
            {
                if (mediaEntry.Library.FriendLibraryShares.Any())
                    if (existingOverride.Status != OverrideRequestStatus.NotRequested)
                        return new ResponseWrapper($"You have already requested access to this {mediaEntry.EntryType.ToString().ToLower()}");
            }
            else
            {
                if (mediaEntry.Library.ProfileLibraryShares.Any(item => item.ProfileId == UserProfile.Id))
                {
                    if (existingOverride != null)
                    {
                        if (existingOverride.State == OverrideState.Block)
                            return CommonResponses.Forbid($"Access to this {mediaEntry.EntryType.ToString().ToLower()} has been blocked by the owner");
                        if (existingOverride.Status != OverrideRequestStatus.NotRequested)
                            return new ResponseWrapper($"You have already requested access to this {mediaEntry.EntryType.ToString().ToLower()}");
                    }
                }
            }

            if (existingOverride != null)
            {
                existingOverride.Status = OverrideRequestStatus.Requested;
                DB.TitleOverrides.Update(existingOverride);

                DB.Notifications.Add(new Data.Models.Notification
                {
                    MediaEntryId = id,
                    TitleOverrideId = existingOverride.Id,
                    Message = $"{UserProfile.Name} has requsted access to \"{mediaEntry.FormattedTitle()}\"",
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
                    Message = $"{UserProfile.Name} has requsted access to \"{mediaEntry.FormattedTitle()}\"",
                    NotificationType = NotificationType.OverrideRequest,
                    ProfileId = UserAccount.Profiles.First(item => item.IsMain).Id,
                    Title = "Access Request",
                    Timestamp = DateTime.UtcNow
                });
            }

            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        public async Task<ResponseWrapper<TitlePermissionInfo>> GetTitlePermissions(int id)
        {
            if (id < 1)
                return CommonResponses.NotFound<TitlePermissionInfo>(nameof(id));

            //I'm not sure how to optimize this, it requires a LOT of joins
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
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
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))
                .ThenInclude(item => item.Profile)
                .Include(item => item.TitleOverrides.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))
                .Where(item => item.Id == id)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound<TitlePermissionInfo>(nameof(id));

            var ret = new TitlePermissionInfo { TitleId = id };

            var profiles = UserAccount.Profiles.ToList();
            profiles.RemoveAll(item => item.IsMain);
            profiles.Sort((x, y) => x.Name.CompareTo(y.Name));

            var friendProfiles = new List<Profile>();
            if (mediaEntry.Library.AccountId == UserAccount.Id)
                foreach (var fls in mediaEntry.Library.FriendLibraryShares)
                {
                    var friend = fls.Friendship;
                    var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                    var profile = profileLst.FirstOrDefault(item => item.IsMain);
                    profile.Name = friend.GetFriendDisplayNameForAccount(UserAccount.Id);
                    friendProfiles.Add(profile);
                }
            profiles.AddRange(friendProfiles.OrderBy(item => item.Name));

            foreach (var profile in profiles)
            {
                var profInfo = new ProfileTitleOverrideInfo
                {
                    AvatarUrl = profile.AvatarUrl,
                    ProfileId = profile.Id,
                    Name = profile.Name
                };

                var ovrride = mediaEntry
                    .TitleOverrides
                    .Where(item => item.ProfileId == profile.Id)
                    .Where(item => item.State == OverrideState.Allow || item.State == OverrideState.Block)
                    .FirstOrDefault();

                if (ovrride == null)
                {
                    if (mediaEntry.Library.ProfileLibraryShares.Any(item => item.ProfileId == profile.Id))
                    {
                        if (profile.IsMain)
                        {
                            profInfo.State = OverrideState.Allow;
                        }
                        else
                        {
                            if (mediaEntry.EntryType == MediaTypes.Movie)
                                profInfo.State = mediaEntry.MovieRating <= profile.MaxMovieRating ?
                                    OverrideState.Allow :
                                    OverrideState.Block;
                            else
                                profInfo.State = mediaEntry.TVRating <= profile.MaxTVRating ?
                                    OverrideState.Allow :
                                    OverrideState.Block;
                        }
                    }
                    else
                    {
                        profInfo.State = OverrideState.Block;
                    }
                }
                else
                {
                    profInfo.State = ovrride.State;
                }

                ret.Profiles.Add(profInfo);
            }

            return new ResponseWrapper<TitlePermissionInfo>(ret);
        }





        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Set access override for a specific title</remarks>
        [HttpPost]
        [RequireMainProfile]
        public async Task<ResponseWrapper> SetTitlePermissions(API.v3.Models.TitlePermissionInfo info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }


            // Don't try to set for self
            info.Profiles.RemoveAll(item => item.ProfileId == UserProfile.Id);
            if (info.Profiles.Count == 0)
                return CommonResponses.Ok();


            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
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
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))
                .ThenInclude(item => item.Profile)
                .Include(item => item.TitleOverrides.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))
                .Where(item => item.Id == info.TitleId)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound(nameof(info.TitleId));


            //Make sure this profile ownes the movie, or this is the main profile and the movie is shared with it
            if(mediaEntry.Library.AccountId != UserAccount.Id)
                if (!UserProfile.IsMain)
                    return CommonResponses.NotFound(nameof(info.TitleId));

            //Check if each profile is owned or the main profile of a friend
            foreach (var ptoi in info.Profiles)
                if (!UserAccount.Profiles.Any(item => item.Id == ptoi.ProfileId))
                {    
                    bool found = false;
                    foreach (var friend in mediaEntry.Library.FriendLibraryShares.Select(item => item.Friendship))
                    {
                        var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                        var profile = profileLst
                            .Where(item => item.Id == ptoi.ProfileId)
                            .Where(item => item.IsMain)
                            .FirstOrDefault();
                        if (profile != null)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return CommonResponses.NotFound($"Profile: {ptoi.ProfileId}");
                }

            
            foreach (var ptoi in info.Profiles)
            {
                var overrideEntity = mediaEntry.TitleOverrides
                    .Where(item => item.ProfileId == ptoi.ProfileId)
                    .FirstOrDefault();

                if (overrideEntity == null)
                {
                    overrideEntity = new Data.Models.TitleOverride
                    {
                        ProfileId = ptoi.ProfileId,
                        MediaEntryId = info.TitleId,
                        State = ptoi.State,
                        Status = ptoi.State == OverrideState.Allow ? OverrideRequestStatus.Granted : OverrideRequestStatus.Denied
                    };
                    mediaEntry.TitleOverrides.Add(overrideEntity);

                    if (ptoi.State == OverrideState.Allow)
                    {
                        //Notify
                        DB.Notifications.Add(new Data.Models.Notification
                        {
                            MediaEntryId = info.TitleId,
                            TitleOverride = overrideEntity,
                            Message = $"{UserAccount.Profiles.First(item => item.Id == ptoi.ProfileId).Name} has granted access to \"{mediaEntry.FormattedTitle()}\"",
                            NotificationType = NotificationType.OverrideRequest,
                            ProfileId = ptoi.ProfileId,
                            Title = "Access Granted",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        //Title is being blocked, but since no override previously existed, there are no
                        //sub overrides for friends kids to worry about
                    }
                }
                else
                {
                    bool doNotification = overrideEntity.Status == OverrideRequestStatus.Requested || ptoi.State == OverrideState.Allow;

                    overrideEntity.Status = ptoi.State == OverrideState.Allow ? OverrideRequestStatus.Granted : OverrideRequestStatus.Denied;
                    overrideEntity.State = ptoi.State;

                    if(ptoi.State == OverrideState.Block)
                        if(!UserAccount.Profiles.Any(item => item.Id == ptoi.ProfileId))
                        {
                            //This profile is a friend, delete all their sub-profile overrides
                            foreach (var friend in mediaEntry.Library.FriendLibraryShares.Select(item => item.Friendship))
                            {
                                var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                                if (profileLst.Any(item => item.Id == ptoi.ProfileId))
                                {
                                    foreach (var profile in profileLst.Where(item => !item.IsMain))
                                    {
                                        var subOverride = mediaEntry.TitleOverrides.FirstOrDefault(item => item.ProfileId == profile.Id);
                                        if (subOverride != null)
                                            DB.TitleOverrides.Remove(subOverride);
                                    }

                                    break;
                                }
                            }
                        }

                    if (doNotification)
                        DB.Notifications.Add(new Data.Models.Notification
                        {
                            MediaEntryId = info.TitleId,
                            TitleOverrideId = overrideEntity.Id,
                            Message = $"{UserAccount.Profiles.First(item => item.Id == ptoi.ProfileId).Name} has {overrideEntity.Status.ToString().ToLower()} access to \"{mediaEntry.FormattedTitle()}\"",
                            NotificationType = NotificationType.OverrideRequest,
                            ProfileId = ptoi.ProfileId,
                            Title = "Access Request",
                            Timestamp = DateTime.UtcNow
                        });

                }                
            }

            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }










        // *** Helpers ***

        Task<List<MediaEntry>> ContinueWatchingAsync(AppDbContext dbInstance, int skip, int take)
        {
            var seriesQ =
                from maxXid in
                (
                    from me in dbInstance.MediaEntries
                    join pmp in dbInstance.ProfileMediaProgresses
                        on new { MediaEntryId = me.LinkedToId.Value, ProfileId = UserProfile.Id }
                        equals new { pmp.MediaEntryId, pmp.ProfileId }

                    where
                        me.LinkedToId.HasValue
                        && me.Xid.HasValue

                    group me by me.LinkedToId into g

                    select new
                    {
                        SeriesId = g.Key.Value,
                        LastXid = g.Max(x => x.Xid)
                    }
                )

                join meEp in dbInstance.MediaEntries
                    on new { maxXid.SeriesId, Xid = maxXid.LastXid }
                    equals new { SeriesId = meEp.LinkedToId.Value, meEp.Xid }

                join meSeries in dbInstance.MediaEntries on meEp.LinkedToId.Value equals meSeries.Id

                join lib in dbInstance.Libraries on meSeries.LibraryId equals lib.Id

                join pmp in dbInstance.ProfileMediaProgresses
                    on new { SeriesId = meSeries.Id, ProfileId = UserProfile.Id }
                    equals new { SeriesId = pmp.MediaEntryId, pmp.ProfileId }

                join fls in dbInstance.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in dbInstance.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()


                join ovrride in dbInstance.TitleOverrides
                    on new { MediaEntryId = meSeries.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    meSeries.EntryType == MediaTypes.Series
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||                        
                        (
                            pls != null
                            && UserProfile.MaxTVRating >= (meSeries.TVRating ?? TVRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                        || ovrride.State == OverrideState.Allow
                    )

                    //Non equal join conditions
                    &&
                    (
                        pmp.Xid < maxXid.LastXid
                        ||
                        (
                            pmp.Xid == maxXid.LastXid
                            && pmp.Played < (meEp.CreditsStartTime ?? meEp.Length - 30)
                        )
                    )


                select new
                {
                    MediaEntry = meSeries,
                    Timestamp = pmp.Timestamp > meEp.Added ? pmp.Timestamp : meEp.Added.Value
                };




            var movieQ =
                from me in dbInstance.MediaEntries

                join lib in dbInstance.Libraries on me.LibraryId equals lib.Id

                join pmp in dbInstance.ProfileMediaProgresses
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id }
                    equals new { pmp.MediaEntryId, pmp.ProfileId }

                join fls in dbInstance.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in dbInstance.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()


                join ovrride in dbInstance.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    me.EntryType == MediaTypes.Movie
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && UserProfile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                        || ovrride.State == OverrideState.Allow
                    )

                    //non equal join conditions
                    && pmp.Played >= 1
                    && pmp.Played < (me.CreditsStartTime ?? me.Length * 0.9)

                select new
                {
                    MediaEntry = me,
                    pmp.Timestamp
                };


            return seriesQ
                .Union(movieQ)
                .OrderByDescending(item => item.Timestamp)
                .Select(item => item.MediaEntry)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<MediaEntry>> WatchlistAsync(AppDbContext dbInstance, int skip, int take)
        {
            var q =
                from me in dbInstance.MediaEntries
                join lib in dbInstance.Libraries on me.LibraryId equals lib.Id

                join wli in dbInstance.WatchListItems
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id }
                    equals new { wli.MediaEntryId, wli.ProfileId }

                join fls in dbInstance.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in dbInstance.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in dbInstance.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    Constants.TOP_LEVEL_MEDIA_TYPES.Contains(me.EntryType)
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && UserProfile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.NotRated)
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && UserProfile.MaxTVRating >= (me.TVRating ?? TVRatings.NotRated)
                                )
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )

                orderby wli.Added

                select me;

            return q
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        IQueryable<MediaEntry> TopLevelWatchableByProfileQuery(AppDbContext dbInstance)
        {
            return
                from me in dbInstance.MediaEntries
                join lib in dbInstance.Libraries on me.LibraryId equals lib.Id

                join fls in dbInstance.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in dbInstance.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in dbInstance.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    Constants.TOP_LEVEL_MEDIA_TYPES.Contains(me.EntryType)
                    &&
                    (
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )


                select me;

        }

        Task<List<MediaEntry>> RecentlyAddedAsync(AppDbContext dbInstance, int skip, int take)
        {
            return TopLevelWatchableByProfileQuery(dbInstance)
                .AsNoTracking()
                .ApplySortOrder(SortOrder.Added_Descending)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<MediaEntry>> PopularAsync(AppDbContext dbInstance, int skip, int take)
        {
            if (skip == 0)
                take = MAX_DB_lIST_sIZE;

            return TopLevelWatchableByProfileQuery(dbInstance)
                .AsNoTracking()
                .ApplySortOrder(SortOrder.Popularity_Descending)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<MediaEntry>> GenresAsync(AppDbContext dbInstance, Genres genre, int skip, int take, SortOrder orderBy)
        {
            long g = (long)genre;

            var q =
                from me in dbInstance.MediaEntries
                join lib in dbInstance.Libraries on me.LibraryId equals lib.Id

                join fls in dbInstance.FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in dbInstance.ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in dbInstance.TitleOverrides
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    new MediaTypes[] { MediaTypes.Movie, MediaTypes.Series }.Contains(me.EntryType)
                    &&
                    (
                        //So far this is the most optimized I know how to do this using LINQ.
                        //Using this big old if then else expression simplifies the query if only 1 genre is flagged,
                        //and also handles Genres.Unknown.
                        genre == Genres.Unknown ? 
                        (
                            me.Genre_Action == false
                            && me.Genre_Adventure == false
                            && me.Genre_Animation == false
                            && me.Genre_Anime == false
                            && me.Genre_Awards_Show == false
                            && me.Genre_Children == false
                            && me.Genre_Comedy == false
                            && me.Genre_Crime == false
                            && me.Genre_Documentary == false
                            && me.Genre_Drama == false
                            && me.Genre_Family == false
                            && me.Genre_Fantasy == false
                            && me.Genre_Food == false
                            && me.Genre_Game_Show == false
                            && me.Genre_History == false
                            && me.Genre_Home_and_Garden == false
                            && me.Genre_Horror == false
                            && me.Genre_Indie == false
                            && me.Genre_Martial_Arts == false
                            && me.Genre_Mini_Series == false
                            && me.Genre_Music == false
                            && me.Genre_Musical == false
                            && me.Genre_Mystery == false
                            && me.Genre_News == false
                            && me.Genre_Podcast == false
                            && me.Genre_Political == false
                            && me.Genre_Reality == false
                            && me.Genre_Romance == false
                            && me.Genre_Science_Fiction == false
                            && me.Genre_Soap == false
                            && me.Genre_Sports == false
                            && me.Genre_Suspense == false
                            && me.Genre_Talk_Show == false
                            && me.Genre_Thriller == false
                            && me.Genre_Travel == false
                            && me.Genre_TV_Movie == false
                            && me.Genre_War == false
                            && me.Genre_Western == false
                        ) :
                        genre == Genres.Action ? me.Genre_Action :
                        genre == Genres.Adventure ? me.Genre_Adventure :
                        genre == Genres.Animation ? me.Genre_Adventure :
                        genre == Genres.Anime ? me.Genre_Anime :
                        genre == Genres.Awards_Show ? me.Genre_Awards_Show :
                        genre == Genres.Children ? me.Genre_Children :
                        genre == Genres.Comedy ? me.Genre_Comedy :
                        genre == Genres.Crime ? me.Genre_Crime :
                        genre == Genres.Documentary ? me.Genre_Documentary :
                        genre == Genres.Drama ? me.Genre_Drama :
                        genre == Genres.Family ? me.Genre_Family :
                        genre == Genres.Fantasy ? me.Genre_Family :
                        genre == Genres.Food ? me.Genre_Food :
                        genre == Genres.Game_Show ? me.Genre_Game_Show :
                        genre == Genres.History ? me.Genre_History :
                        genre == Genres.Home_and_Garden ? me.Genre_Home_and_Garden :
                        genre == Genres.Horror ? me.Genre_Horror :
                        genre == Genres.Indie ? me.Genre_Indie :
                        genre == Genres.Martial_Arts ? me.Genre_Martial_Arts :
                        genre == Genres.Mini_Series ? me.Genre_Mini_Series :
                        genre == Genres.Music ? me.Genre_Music :
                        genre == Genres.Musical ? me.Genre_Musical :
                        genre == Genres.Mystery ? me.Genre_Mystery :
                        genre == Genres.News ? me.Genre_News :
                        genre == Genres.Podcast ? me.Genre_Podcast :
                        genre == Genres.Political ? me.Genre_Political :
                        genre == Genres.Reality ? me.Genre_Reality :
                        genre == Genres.Romance ? me.Genre_Romance :
                        genre == Genres.Science_Fiction ? me.Genre_Science_Fiction :
                        genre == Genres.Soap ? me.Genre_Soap :
                        genre == Genres.Sports ? me.Genre_Sports :
                        genre == Genres.Suspense ? me.Genre_Suspense :
                        genre == Genres.Talk_Show ? me.Genre_Talk_Show :
                        genre == Genres.Thriller ? me.Genre_Thriller :
                        genre == Genres.Travel ? me.Genre_Travel :
                        genre == Genres.TV_Movie ? me.Genre_TV_Movie :
                        genre == Genres.War ? me.Genre_War :
                        genre == Genres.Western ? me.Genre_Western :
                        (
                            // genre is not a single flag.
                            // If it's a valid combo of flags (like Genres.Action | Genres.Western), this will return the correct rows
                            // If it's an invalid flag (like -1), this will return zero rows
                            (((g & (long)Genres.Action) == (long)Genres.Action) && me.Genre_Action)
                            || (((g & (long)Genres.Adventure) == (long)Genres.Adventure) && me.Genre_Adventure)
                            || (((g & (long)Genres.Animation) == (long)Genres.Animation) && me.Genre_Animation)
                            || (((g & (long)Genres.Anime) == (long)Genres.Anime) && me.Genre_Anime)
                            || (((g & (long)Genres.Awards_Show) == (long)Genres.Awards_Show) && me.Genre_Awards_Show)
                            || (((g & (long)Genres.Children) == (long)Genres.Children) && me.Genre_Children)
                            || (((g & (long)Genres.Comedy) == (long)Genres.Comedy) && me.Genre_Comedy)
                            || (((g & (long)Genres.Crime) == (long)Genres.Crime) && me.Genre_Crime)
                            || (((g & (long)Genres.Documentary) == (long)Genres.Documentary) && me.Genre_Documentary)
                            || (((g & (long)Genres.Drama) == (long)Genres.Drama) && me.Genre_Drama)
                            || (((g & (long)Genres.Family) == (long)Genres.Family) && me.Genre_Family)
                            || (((g & (long)Genres.Fantasy) == (long)Genres.Fantasy) && me.Genre_Fantasy)
                            || (((g & (long)Genres.Food) == (long)Genres.Food) && me.Genre_Food)
                            || (((g & (long)Genres.Game_Show) == (long)Genres.Game_Show) && me.Genre_Game_Show)
                            || (((g & (long)Genres.History) == (long)Genres.History) && me.Genre_History)
                            || (((g & (long)Genres.Home_and_Garden) == (long)Genres.Home_and_Garden) && me.Genre_Home_and_Garden)
                            || (((g & (long)Genres.Horror) == (long)Genres.Horror) && me.Genre_Horror)
                            || (((g & (long)Genres.Indie) == (long)Genres.Indie) && me.Genre_Indie)
                            || (((g & (long)Genres.Martial_Arts) == (long)Genres.Martial_Arts) && me.Genre_Martial_Arts)
                            || (((g & (long)Genres.Mini_Series) == (long)Genres.Mini_Series) && me.Genre_Mini_Series)
                            || (((g & (long)Genres.Music) == (long)Genres.Music) && me.Genre_Music)
                            || (((g & (long)Genres.Musical) == (long)Genres.Musical) && me.Genre_Musical)
                            || (((g & (long)Genres.Mystery) == (long)Genres.Mystery) && me.Genre_Mystery)
                            || (((g & (long)Genres.News) == (long)Genres.News) && me.Genre_News)
                            || (((g & (long)Genres.Podcast) == (long)Genres.Podcast) && me.Genre_Podcast)
                            || (((g & (long)Genres.Political) == (long)Genres.Political) && me.Genre_Political)
                            || (((g & (long)Genres.Reality) == (long)Genres.Reality) && me.Genre_Reality)
                            || (((g & (long)Genres.Romance) == (long)Genres.Romance) && me.Genre_Romance)
                            || (((g & (long)Genres.Science_Fiction) == (long)Genres.Science_Fiction) && me.Genre_Science_Fiction)
                            || (((g & (long)Genres.Soap) == (long)Genres.Soap) && me.Genre_Soap)
                            || (((g & (long)Genres.Sports) == (long)Genres.Sports) && me.Genre_Sports)
                            || (((g & (long)Genres.Suspense) == (long)Genres.Suspense) && me.Genre_Suspense)
                            || (((g & (long)Genres.Talk_Show) == (long)Genres.Talk_Show) && me.Genre_Talk_Show)
                            || (((g & (long)Genres.Thriller) == (long)Genres.Thriller) && me.Genre_Thriller)
                            || (((g & (long)Genres.Travel) == (long)Genres.Travel) && me.Genre_Travel)
                            || (((g & (long)Genres.TV_Movie) == (long)Genres.TV_Movie) && me.Genre_TV_Movie)
                            || (((g & (long)Genres.War) == (long)Genres.War) && me.Genre_War)
                            || (((g & (long)Genres.Western) == (long)Genres.Western) && me.Genre_Western)
                        )
                    )
                    &&
                    (
                        // Watch permissions
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && me.MovieRating <= UserProfile.MaxMovieRating
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Series
                                    && me.TVRating <= UserProfile.MaxTVRating
                                )
                            )
                        )
                        || ovrride.State == OverrideState.Allow
                    )


                select me;

            return q.ApplySortOrder(orderBy)
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<Data.Models.Playlist>> PlaylistsAsync(AppDbContext db, int skip, int take)
        {
            return db.Playlists
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

    }
}
