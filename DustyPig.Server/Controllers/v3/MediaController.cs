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
using System.Threading;
using System.Threading.Tasks;
using Enum = System.Enum;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(MediaController))]
    public class MediaController : _MediaControllerBase
    {

        public MediaController(AppDbContext db) : base(db)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<HomeScreen>))]
        public async Task<Result<HomeScreen>> HomeScreen()
        {
            var ret = new HomeScreen();

            var taskDict = new Dictionary<KeyValuePair<long, string>, Task>();


            taskDict.Add
                (
                    new KeyValuePair<long, string>
                    (
                        DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING,
                        DustyPig.API.v3.Clients.MediaClient.ID_CONTINUE_WATCHING_TITLE
                    ),
                    ContinueWatchingAsync(new AppDbContext(), 0, DEFAULT_LIST_SIZE)
               );


            taskDict.Add
                (
                    new KeyValuePair<long, string>
                    (
                        DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST,
                        DustyPig.API.v3.Clients.MediaClient.ID_WATCHLIST_TITLE
                    ),
                    WatchlistAsync(new AppDbContext(), 0, DEFAULT_LIST_SIZE)
                );


            taskDict.Add
                (
                    new KeyValuePair<long, string>
                    (
                        DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS,
                        DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS_TITLE
                    ),
                    PlaylistsAsync(new AppDbContext(), 0, DEFAULT_LIST_SIZE)
                );

            taskDict.Add
               (
                   new KeyValuePair<long, string>
                   (
                       DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED,
                       DustyPig.API.v3.Clients.MediaClient.ID_RECENTLY_ADDED_TITLE
                   ),
                   RecentlyAddedAsync(new AppDbContext(), 0, DEFAULT_LIST_SIZE)
               );

            taskDict.Add
               (
                   new KeyValuePair<long, string>
                   (
                       DustyPig.API.v3.Clients.MediaClient.ID_POPULAR,
                       DustyPig.API.v3.Clients.MediaClient.ID_POPULAR_TITLE
                   ),
                   PopularAsync(new AppDbContext(), 0, MAX_DB_LIST_SIZE)
               );


            await Task.WhenAll(taskDict.Values);


            foreach (var query in taskDict)
                if (query.Key.Key == DustyPig.API.v3.Clients.MediaClient.ID_PLAYLISTS)
                {
                    var result = (query.Value as Task<List<Data.Models.Playlist>>).Result;
                    if (result.Count > 0)
                    {
                        ret.Sections ??= new();
                        ret.Sections.Add(new HomeScreenList
                        {
                            ListId = query.Key.Key,
                            Title = query.Key.Value,
                            Items = result.Select(item => item.ToBasicMedia()).ToList()
                        });
                    }
                }
                else if (query.Key.Key == DustyPig.API.v3.Clients.MediaClient.ID_POPULAR)
                {
                    var result = (query.Value as Task<List<MediaEntry>>).Result;
                    if (result.Count > 0)
                    {
                        ret.Sections ??= new();
                        ret.Sections.Add(new HomeScreenList
                        {
                            ListId = query.Key.Key,
                            Title = query.Key.Value,
                            Items = result.Take(DEFAULT_LIST_SIZE).Select(item => item.ToBasicMedia()).ToList()
                        });
                    }

                    var gd = new Dictionary<Genres, List<BasicMedia>>();
                    foreach (var me in result)
                    {
                        if (me.Genre_Action && ForceGenre(gd, Genres.Action).Count < DEFAULT_LIST_SIZE) gd[Genres.Action].Add(me.ToBasicMedia());
                        if (me.Genre_Adventure && ForceGenre(gd, Genres.Adventure).Count < DEFAULT_LIST_SIZE) gd[Genres.Adventure].Add(me.ToBasicMedia());
                        if (me.Genre_Animation && ForceGenre(gd, Genres.Animation).Count < DEFAULT_LIST_SIZE) gd[Genres.Animation].Add(me.ToBasicMedia());
                        if (me.Genre_Anime && ForceGenre(gd, Genres.Anime).Count < DEFAULT_LIST_SIZE) gd[Genres.Anime].Add(me.ToBasicMedia());
                        if (me.Genre_Awards_Show && ForceGenre(gd, Genres.Awards_Show).Count < DEFAULT_LIST_SIZE) gd[Genres.Awards_Show].Add(me.ToBasicMedia());
                        if (me.Genre_Children && ForceGenre(gd, Genres.Children).Count < DEFAULT_LIST_SIZE) gd[Genres.Children].Add(me.ToBasicMedia());
                        if (me.Genre_Comedy && ForceGenre(gd, Genres.Comedy).Count < DEFAULT_LIST_SIZE) gd[Genres.Comedy].Add(me.ToBasicMedia());
                        if (me.Genre_Crime && ForceGenre(gd, Genres.Crime).Count < DEFAULT_LIST_SIZE) gd[Genres.Crime].Add(me.ToBasicMedia());
                        if (me.Genre_Documentary && ForceGenre(gd, Genres.Documentary).Count < DEFAULT_LIST_SIZE) gd[Genres.Documentary].Add(me.ToBasicMedia());
                        if (me.Genre_Drama && ForceGenre(gd, Genres.Drama).Count < DEFAULT_LIST_SIZE) gd[Genres.Drama].Add(me.ToBasicMedia());
                        if (me.Genre_Family && ForceGenre(gd, Genres.Family).Count < DEFAULT_LIST_SIZE) gd[Genres.Family].Add(me.ToBasicMedia());
                        if (me.Genre_Fantasy && ForceGenre(gd, Genres.Fantasy).Count < DEFAULT_LIST_SIZE) gd[Genres.Fantasy].Add(me.ToBasicMedia());
                        if (me.Genre_Food && ForceGenre(gd, Genres.Food).Count < DEFAULT_LIST_SIZE) gd[Genres.Food].Add(me.ToBasicMedia());
                        if (me.Genre_Game_Show && ForceGenre(gd, Genres.Game_Show).Count < DEFAULT_LIST_SIZE) gd[Genres.Game_Show].Add(me.ToBasicMedia());
                        if (me.Genre_History && ForceGenre(gd, Genres.History).Count < DEFAULT_LIST_SIZE) gd[Genres.History].Add(me.ToBasicMedia());
                        if (me.Genre_Home_and_Garden && ForceGenre(gd, Genres.Home_and_Garden).Count < DEFAULT_LIST_SIZE) gd[Genres.Home_and_Garden].Add(me.ToBasicMedia());
                        if (me.Genre_Horror && ForceGenre(gd, Genres.Horror).Count < DEFAULT_LIST_SIZE) gd[Genres.Horror].Add(me.ToBasicMedia());
                        if (me.Genre_Indie && ForceGenre(gd, Genres.Indie).Count < DEFAULT_LIST_SIZE) gd[Genres.Indie].Add(me.ToBasicMedia());
                        if (me.Genre_Martial_Arts && ForceGenre(gd, Genres.Martial_Arts).Count < DEFAULT_LIST_SIZE) gd[Genres.Martial_Arts].Add(me.ToBasicMedia());
                        if (me.Genre_Mini_Series && ForceGenre(gd, Genres.Mini_Series).Count < DEFAULT_LIST_SIZE) gd[Genres.Mini_Series].Add(me.ToBasicMedia());
                        if (me.Genre_Music && ForceGenre(gd, Genres.Music).Count < DEFAULT_LIST_SIZE) gd[Genres.Music].Add(me.ToBasicMedia());
                        if (me.Genre_Musical && ForceGenre(gd, Genres.Musical).Count < DEFAULT_LIST_SIZE) gd[Genres.Musical].Add(me.ToBasicMedia());
                        if (me.Genre_Mystery && ForceGenre(gd, Genres.Mystery).Count < DEFAULT_LIST_SIZE) gd[Genres.Mystery].Add(me.ToBasicMedia());
                        if (me.Genre_News && ForceGenre(gd, Genres.News).Count < DEFAULT_LIST_SIZE) gd[Genres.News].Add(me.ToBasicMedia());
                        if (me.Genre_Podcast && ForceGenre(gd, Genres.Podcast).Count < DEFAULT_LIST_SIZE) gd[Genres.Podcast].Add(me.ToBasicMedia());
                        if (me.Genre_Political && ForceGenre(gd, Genres.Political).Count < DEFAULT_LIST_SIZE) gd[Genres.Political].Add(me.ToBasicMedia());
                        if (me.Genre_Reality && ForceGenre(gd, Genres.Reality).Count < DEFAULT_LIST_SIZE) gd[Genres.Reality].Add(me.ToBasicMedia());
                        if (me.Genre_Romance && ForceGenre(gd, Genres.Romance).Count < DEFAULT_LIST_SIZE) gd[Genres.Romance].Add(me.ToBasicMedia());
                        if (me.Genre_Science_Fiction && ForceGenre(gd, Genres.Science_Fiction).Count < DEFAULT_LIST_SIZE) gd[Genres.Science_Fiction].Add(me.ToBasicMedia());
                        if (me.Genre_Soap && ForceGenre(gd, Genres.Soap).Count < DEFAULT_LIST_SIZE) gd[Genres.Soap].Add(me.ToBasicMedia());
                        if (me.Genre_Sports && ForceGenre(gd, Genres.Sports).Count < DEFAULT_LIST_SIZE) gd[Genres.Sports].Add(me.ToBasicMedia());
                        if (me.Genre_Suspense && ForceGenre(gd, Genres.Suspense).Count < DEFAULT_LIST_SIZE) gd[Genres.Suspense].Add(me.ToBasicMedia());
                        if (me.Genre_Talk_Show && ForceGenre(gd, Genres.Talk_Show).Count < DEFAULT_LIST_SIZE) gd[Genres.Talk_Show].Add(me.ToBasicMedia());
                        if (me.Genre_Thriller && ForceGenre(gd, Genres.Thriller).Count < DEFAULT_LIST_SIZE) gd[Genres.Thriller].Add(me.ToBasicMedia());
                        if (me.Genre_Travel && ForceGenre(gd, Genres.Travel).Count < DEFAULT_LIST_SIZE) gd[Genres.Travel].Add(me.ToBasicMedia());
                        if (me.Genre_TV_Movie && ForceGenre(gd, Genres.TV_Movie).Count < DEFAULT_LIST_SIZE) gd[Genres.TV_Movie].Add(me.ToBasicMedia());
                        if (me.Genre_War && ForceGenre(gd, Genres.War).Count < DEFAULT_LIST_SIZE) gd[Genres.War].Add(me.ToBasicMedia());
                        if (me.Genre_Western && ForceGenre(gd, Genres.Western).Count < DEFAULT_LIST_SIZE) gd[Genres.Western].Add(me.ToBasicMedia());
                    }

                    foreach (Genres g in gd.Keys)
                        if (gd[g].Count >= MIN_GENRE_LIST_SIZE)
                        {
                            ret.Sections ??= new();
                            ret.Sections.Add(new HomeScreenList
                            {
                                ListId = (long)g,
                                Title = g.AsString(),
                                Items = gd[g].Take(DEFAULT_LIST_SIZE).ToList()
                            });
                        }
                }
                else
                {
                    var result = (query.Value as Task<List<MediaEntry>>).Result;
                    if (result.Count > 0)
                    {
                        ret.Sections ??= new();
                        ret.Sections.Add(new HomeScreenList
                        {
                            ListId = query.Key.Key,
                            Title = query.Key.Value,
                            Items = result.Select(item => item.ToBasicMedia()).ToList()
                        });
                    }
                }




            ret.Sections ??= new();
            ret.Sections.Sort();

            return ret;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns more items for the specified home screen list based on start position</remarks>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> LoadMoreHomeScreenItems(HomeScreenListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return ex; }

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
                results = (await GenresAsync(DB, (Genres)request.ListId, request.Start, DEFAULT_LIST_SIZE)).Select(item => item.ToBasicMedia()).ToList();

            return results;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<SearchResults>))]
        public async Task<Result<SearchResults>> Search(SearchRequest request, CancellationToken cancellationToken)
        {
            var ret = new SearchResults();

            if (string.IsNullOrWhiteSpace(request.Query))
                return ret;

            request.Query = StringUtils.NormalizedQueryString(request.Query);
            if (string.IsNullOrWhiteSpace(request.Query))
                return ret;

            var terms = request.Query.Tokenize();




            /****************************************
             * Get available items
             ****************************************/
            var q = DB.TopLevelWatchableMediaByProfileQuery(UserProfile);



            //foreach (var term in terms)
            //    q = q.Where(m => m.MediaSearchBridges.Any(b => b.SearchTerm.Term.Contains(term)));


            //The above method generates sql that is literally 500 times slower than this inner join method
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
                .Take(MAX_DB_LIST_SIZE)
                .ToListAsync(cancellationToken);


            //Search sort
            if (mediaEntries.Count > 0)
            {
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

                ret.Available ??= new();
                ret.Available.AddRange(mediaEntries.Select(item => item.ToBasicMedia()).Take(DEFAULT_LIST_SIZE));
            }

            /****************************************
             * Search online databases
             ****************************************/
            ret.OtherTitlesAllowed = UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled;

            var searchOtherTitles = request.SearchTMDB && UserAccount.Id != TestAccount.AccountId && ret.OtherTitlesAllowed;
            if (request.SearchPeople || searchOtherTitles)
            {
                var response = await _tmdbClient.Endpoints.Search.GetMultiAsync(request.Query, cancellationToken: cancellationToken);
                if (response.Success && response.Data.Results.Count > 0)
                {
                    if (searchOtherTitles)
                    {
                        ret.OtherTitles ??= [];
                        ret.OtherTitles.AddRange
                            (
                                response.Data.Results
                                    .Where(item => item.MediaType != "person")
                                    .Select(item => item.ToBasicTMDBInfo())
                                    .Take(DEFAULT_LIST_SIZE)
                            );
                    }

                    if (request.SearchPeople)
                    {
                        var apiPeopleIds = response.Data.Results
                            .Where(item => item.MediaType == "person")
                            .Select(item => item.Id)
                            .ToList();

                        var dbPeopleIds = await DB.TMDB_People
                            .AsNoTracking()
                            .Where(item => apiPeopleIds.Contains(item.TMDB_Id))
                            .Select(item => item.TMDB_Id)
                            .ToListAsync();

                        if(dbPeopleIds.Count > 0)
                        {
                            //Keep the sort order of the api request
                            ret.AvailablePeople ??= [];
                            ret.AvailablePeople.AddRange
                                (
                                    response.Data.Results
                                        .Where(item => item.MediaType == "person")
                                        .Where(item => dbPeopleIds.Contains(item.Id))
                                        .Select(item => item.ToTMDBPerson())
                                        .Take(DEFAULT_LIST_SIZE)
                                );
                        }


                        if (searchOtherTitles)
                        {
                            ret.OtherPeople ??= [];
                            ret.OtherPeople.AddRange
                                (
                                    response.Data.Results
                                        .Where(item => item.MediaType == "person")
                                        .Select(item => item.ToTMDBPerson())
                                        .Take(DEFAULT_LIST_SIZE)
                                );
                        }
                    }
                }
            }

            return ret;
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> AddToWatchlist(int id)
        {
            var mediaEntry = await DB.TopLevelWatchableMediaByProfileQuery(UserProfile)
                .AsNoTracking()
                .Include(m => m.WatchlistItems.Where(w => w.ProfileId == UserProfile.Id))
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            if (!mediaEntry.WatchlistItems.Any())
            {
                DB.WatchListItems.Add(new WatchlistItem
                {
                    Added = DateTime.UtcNow,
                    MediaEntryId = id,
                    ProfileId = UserProfile.Id
                });

                await DB.SaveChangesAsync();
            }

            return Result.BuildSuccess();
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> DeleteFromWatchlist(int id)
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

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> UpdatePlaybackProgress(PlaybackProgress hist)
        {
            try { hist.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            var maybeEpisode = await DB.MediaEntries
                .AsNoTracking()
                .Where(m => m.Id == hist.Id)
                .FirstOrDefaultAsync();

            if (maybeEpisode == null)
                return CommonResponses.ValueNotFound(nameof(hist.Id));

            if (!Constants.PLAYABLE_MEDIA_TYPES.Contains(maybeEpisode.EntryType))
                return CommonResponses.ValueNotFound(nameof(hist.Id));

            if (maybeEpisode.EntryType == MediaTypes.Episode)
                hist.Id = maybeEpisode.LinkedToId.Value;


            var mediaEntry = await DB.TopLevelWatchableMediaByProfileQuery(UserProfile)
                .AsNoTracking()
                .Include(m => m.ProfileMediaProgress.Where(p => p.ProfileId == UserProfile.Id))
                .Where(m => m.Id == hist.Id)
                .FirstOrDefaultAsync();


            if (mediaEntry == null)
                return CommonResponses.ValueNotFound(nameof(hist.Id));

            var progress = mediaEntry.ProfileMediaProgress.FirstOrDefault();
            if (progress == null)
            {
                if (mediaEntry.EntryType == MediaTypes.Movie)
                    if (hist.Seconds < 1 || hist.Seconds > (mediaEntry.CreditsStartTime ?? (mediaEntry.Length * 0.9)))
                        return Result.BuildSuccess();

                //Add
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = mediaEntry.Id,
                    ProfileId = UserProfile.Id,
                    Played = Math.Max(0, hist.Seconds),
                    Timestamp = DateTime.UtcNow,
                    Xid = maybeEpisode.Xid
                });
            }
            else
            {
                if (mediaEntry.EntryType == MediaTypes.Movie && (hist.Seconds < 1 || hist.Seconds > (mediaEntry.CreditsStartTime ?? (mediaEntry.Length * 0.9))))
                {
                    DB.ProfileMediaProgresses.Remove(progress);
                }
                else
                {
                    //Update
                    progress.Played = Math.Max(0, hist.Seconds);
                    progress.Timestamp = DateTime.UtcNow;
                    progress.Xid = maybeEpisode.Xid;
                    DB.ProfileMediaProgresses.Update(progress);
                }
            }


            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }




        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> RequestAccessOverride(int id)
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
                return CommonResponses.ValueNotFound(nameof(id));



            //Does this profile alredy have access?
            var existingOverride = mediaEntry.TitleOverrides
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.State == OverrideState.Allow || item.State == OverrideState.Block)
                .FirstOrDefault();

            if (UserProfile.IsMain)
            {
                //Main profile can already access ALL owned media
                if (mediaEntry.Library.AccountId == UserAccount.Id)
                    return Result.BuildSuccess();

                if (mediaEntry.Library.FriendLibraryShares.Any())
                {
                    //For libs shared with the main profile, they can access anything not specifically blocked
                    if (existingOverride == null)
                        return Result.BuildSuccess();

                    if (existingOverride.State == OverrideState.Block)
                        return $"Access to this {mediaEntry.EntryType.ToString().ToLower()} has been blocked by the owner";
                    return Result.BuildSuccess();
                }
            }
            else
            {
                if (existingOverride == null)
                {
                    if (mediaEntry.Library.ProfileLibraryShares.Any(item => item.ProfileId == UserProfile.Id))
                    {
                        if (mediaEntry.EntryType == MediaTypes.Movie && UserProfile.MaxMovieRating >= (mediaEntry.MovieRating ?? MovieRatings.Unrated))
                            return Result.BuildSuccess();
                        if (mediaEntry.EntryType == MediaTypes.Series && UserProfile.MaxTVRating >= (mediaEntry.TVRating ?? TVRatings.NotRated))
                            return Result.BuildSuccess();
                    }
                }
                else
                {
                    if (existingOverride.State == OverrideState.Block)
                        return $"Access to this {mediaEntry.EntryType.ToString().ToLower()} has been blocked by the owner";
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
                        return Result.BuildSuccess();
            }
            else
            {
                if (mediaEntry.Library.ProfileLibraryShares.Any(item => item.ProfileId == UserProfile.Id))
                {
                    if (existingOverride != null)
                    {
                        if (existingOverride.State == OverrideState.Block)
                            return $"Access to this {mediaEntry.EntryType.ToString().ToLower()} has been blocked by the owner";
                        if (existingOverride.Status != OverrideRequestStatus.NotRequested)
                            return Result.BuildSuccess();
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
                    NotificationType = NotificationTypes.OverrideMediaRequested,
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
                    NotificationType = NotificationTypes.OverrideMediaRequested,
                    ProfileId = UserAccount.Profiles.First(item => item.IsMain).Id,
                    Title = "Access Request",
                    Timestamp = DateTime.UtcNow
                });
            }

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<TitlePermissions>))]
        public async Task<Result<TitlePermissions>> GetTitlePermissions(int id)
        {
            if (id < 1)
                return CommonResponses.ValueNotFound(nameof(id));

            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(fls => fls.Friendship.Account1Id == UserAccount.Id || fls.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(fls => fls.Friendship.Account1Id == UserAccount.Id || fls.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.AccountId == UserAccount.Id))
                .ThenInclude(item => item.Profile)
                .Include(item => item.TitleOverrides.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))

                .Where(item => item.Id == id)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var ret = new TitlePermissions { MediaId = id };


            // Sub Profiles
            var profiles = UserAccount.Profiles.ToList();
            profiles.RemoveAll(item => item.IsMain);
            if (profiles.Count > 0)
            {
                ret.SubProfiles ??= new();
                foreach (var profile in profiles)
                {
                    var profInfo = new ProfileTitleOverride
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
                                profInfo.OverrideState = OverrideState.Allow;
                            }
                            else
                            {
                                if (mediaEntry.EntryType == MediaTypes.Movie)
                                    profInfo.OverrideState = profile.MaxMovieRating >= (mediaEntry.MovieRating ?? MovieRatings.Unrated) ?
                                        OverrideState.Allow :
                                        OverrideState.Block;
                                else
                                    profInfo.OverrideState = profile.MaxTVRating >= (mediaEntry.TVRating ?? TVRatings.NotRated) ?
                                        OverrideState.Allow :
                                        OverrideState.Block;
                            }
                        }
                        else
                        {
                            profInfo.OverrideState = OverrideState.Block;
                        }
                    }
                    else
                    {
                        profInfo.OverrideState = ovrride.State;
                    }

                    ret.SubProfiles.Add(profInfo);
                }
            }


            //Friend Profiles
            var friendProfiles = new List<Profile>();
            if (mediaEntry.Library.AccountId == UserAccount.Id)
            {
                var myFriends = await DB.Friendships
                    .AsNoTracking()
                    .Include(item => item.Account1)
                    .ThenInclude(item => item.Profiles.Where(p => p.IsMain))
                    .Include(item => item.Account2)
                    .ThenInclude(item => item.Profiles.Where(p => p.IsMain))
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .Where(item => item.Accepted)
                    .ToListAsync();

                foreach (var friend in myFriends)
                {
                    var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                    var profile = profileLst.FirstOrDefault(item => item.IsMain);
                    if (profile.Id != UserProfile.Id)
                    {
                        profile.Name = friend.GetFriendDisplayNameForAccount(UserAccount.Id);

                        var profInfo = new ProfileTitleOverride
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
                            bool shared = false;
                            foreach (var fls in mediaEntry.Library.FriendLibraryShares)
                            {
                                var sharedProfileLst = fls.Friendship.Account1Id == UserAccount.Id ? fls.Friendship.Account2.Profiles : fls.Friendship.Account1.Profiles;
                                shared = sharedProfileLst
                                    .Where(item => item.Id == profile.Id)
                                    .Where(item => item.IsMain)
                                    .Any();

                                if (shared)
                                    break;
                            }

                            if (shared)
                                profInfo.OverrideState = OverrideState.Allow;
                            else
                                profInfo.OverrideState = OverrideState.Block;
                        }
                        else
                        {
                            profInfo.OverrideState = ovrride.State;
                        }

                        ret.FriendProfiles ??= new();
                        ret.FriendProfiles.Add(profInfo);
                    }
                }
            }

            if (ret.SubProfiles != null)
                ret.SubProfiles.Sort();

            if (ret.FriendProfiles != null)
                ret.FriendProfiles.Sort();

            return ret;
        }





        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Set access override for a specific title</remarks>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> SetTitlePermissions(SetTitlePermission info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            // Don't try to set for self
            if (info.ProfileId == UserProfile.Id)
                return Result.BuildSuccess();


            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(fls => fls.Friendship.Account1Id == UserAccount.Id || fls.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.FriendLibraryShares.Where(fls => fls.Friendship.Account1Id == UserAccount.Id || fls.Friendship.Account2Id == UserAccount.Id))
                .ThenInclude(item => item.Friendship)
                .ThenInclude(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Library)
                .ThenInclude(item => item.ProfileLibraryShares)
                .ThenInclude(item => item.Profile)

                .Include(item => item.TitleOverrides.Where(item2 => item2.Profile.AccountId == UserAccount.Id || item2.Profile.IsMain))

                .Where(item => item.Id == info.MediaId)
                .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.ValueNotFound(nameof(info.MediaId));

            List<Friendship> myFriends = null;


            //Check if current user can edit permissions
            if (mediaEntry.Library.AccountId == UserAccount.Id)
            {
                //User owns media
                //Can edit sub profiles
                //Can edit for main profiles of friends
                if (!UserAccount.Profiles.Any(item => item.Id == info.ProfileId))
                {
                    bool found = false;
                    if (myFriends == null)
                        myFriends = await DB.Friendships
                            .AsNoTracking()
                            .Include(item => item.Account1)
                            .ThenInclude(item => item.Profiles)
                            .Include(item => item.Account2)
                            .ThenInclude(item => item.Profiles)
                            .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                            .Where(item => item.Accepted)
                            .ToListAsync();

                    foreach (var friend in myFriends)
                    {
                        var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                        found = profileLst
                            .Where(item => item.Id == info.ProfileId)
                            .Where(item => item.IsMain)
                            .Any();
                        if (found)
                            break;
                    }


                    //If still not found, return an error
                    if (!found)
                        return CommonResponses.ValueNotFound(nameof(info.ProfileId));
                }
            }
            else
            {
                //User does not own media
                //Can only edit for sub profiles
                if (!UserAccount.Profiles.Any(item => item.Id == info.ProfileId))
                    return CommonResponses.ValueNotFound(nameof(info.ProfileId));
            }





            // *** Check if user can view by default ***
            bool allowByDefault = false;
            var profile = UserAccount.Profiles.FirstOrDefault(item => item.Id == info.ProfileId);
            if (profile != null)
            {
                // A sub profile if the current account, check if the library is shared with sub profile
                if (mediaEntry.Library.ProfileLibraryShares.Any(item => item.ProfileId == info.ProfileId))
                {
                    //Check rating
                    switch (mediaEntry.EntryType)
                    {
                        case MediaTypes.Movie:
                            allowByDefault = profile.MaxMovieRating >= (mediaEntry.MovieRating ?? MovieRatings.Unrated);
                            break;
                        case MediaTypes.Series:
                            allowByDefault = profile.MaxTVRating >= (mediaEntry.TVRating ?? TVRatings.NotRated);
                            break;
                    }
                }
            }
            else
            {
                // The main profile of a friend, check if the library is shared with friend
                foreach (var friend in mediaEntry.Library.FriendLibraryShares.Select(item => item.Friendship))
                {
                    var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                    profile = profileLst
                        .Where(item => item.Id == info.ProfileId)
                        .Where(item => item.IsMain)
                        .FirstOrDefault();
                    if (profile != null)
                    {
                        allowByDefault = true;
                        break;
                    }
                }
            }





            //Check for an existing override
            var overrideEntity = mediaEntry.TitleOverrides
                .Where(item => item.ProfileId == info.ProfileId)
                .FirstOrDefault();



            if (overrideEntity == null)
            {
                bool addOverride = false;
                switch (info.OverrideState)
                {
                    case OverrideState.Allow:
                        addOverride = !allowByDefault;
                        break;

                    case OverrideState.Block:
                        addOverride = allowByDefault;
                        break;

                    case OverrideState.Default:
                        addOverride = false;
                        break;
                }

                if (addOverride)
                {
                    overrideEntity = new Data.Models.TitleOverride
                    {
                        ProfileId = info.ProfileId,
                        MediaEntryId = info.MediaId,
                        State = info.OverrideState,
                        Status = info.OverrideState == OverrideState.Allow ? OverrideRequestStatus.Granted : OverrideRequestStatus.Denied
                    };
                    DB.TitleOverrides.Add(overrideEntity);
                }
            }
            else
            {

                //Check if should delete
                bool deleteOverride = false;
                switch (info.OverrideState)
                {
                    case OverrideState.Allow:
                        deleteOverride = allowByDefault;
                        break;

                    case OverrideState.Block:
                        deleteOverride = !allowByDefault;
                        break;

                    case OverrideState.Default:
                        deleteOverride = true;
                        break;
                }

                if (deleteOverride)
                {
                    DB.TitleOverrides.Remove(overrideEntity);
                }
                else
                {
                    bool doNotification = overrideEntity.Status == OverrideRequestStatus.Requested;

                    overrideEntity.Status = info.OverrideState == OverrideState.Allow ? OverrideRequestStatus.Granted : OverrideRequestStatus.Denied;
                    overrideEntity.State = info.OverrideState;
                    DB.TitleOverrides.Update(overrideEntity);

                    if (info.OverrideState == OverrideState.Block)
                    {
                        if (mediaEntry.Library.AccountId == UserAccount.Id && !UserAccount.Profiles.Any(item => item.Id == info.ProfileId))
                        {
                            //User owns this media && the profile is a friend, delete all their sub-profile overrides
                            if (myFriends == null)
                                myFriends = await DB.Friendships
                                    .AsNoTracking()
                                    .Include(item => item.Account1)
                                    .ThenInclude(item => item.Profiles)
                                    .Include(item => item.Account2)
                                    .ThenInclude(item => item.Profiles)
                                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                                    .Where(item => item.Accepted)
                                    .ToListAsync();
                            foreach (var friend in myFriends)
                            {
                                var profileLst = friend.Account1Id == UserAccount.Id ? friend.Account2.Profiles : friend.Account1.Profiles;
                                profile = profileLst
                                   .Where(item => item.Id == info.ProfileId)
                                   .Where(item => item.IsMain)
                                   .FirstOrDefault();
                                if (profile != null)
                                {
                                    foreach (var subProfile in profileLst)
                                    {
                                        var subOverride = mediaEntry.TitleOverrides.FirstOrDefault(item => item.ProfileId == subProfile.Id);
                                        if (subOverride != null)
                                            DB.TitleOverrides.Remove(subOverride);
                                    }
                                    break;
                                }
                            }
                        }
                    }

                    if (doNotification)
                    {
                        DB.Notifications.Add(new Data.Models.Notification
                        {
                            MediaEntryId = info.MediaId,
                            TitleOverrideId = overrideEntity.Id,
                            Message = $"{UserAccount.Profiles.First(item => item.Id == info.ProfileId).Name} has {overrideEntity.Status.ToString().ToLower()} access to \"{mediaEntry.FormattedTitle()}\"",
                            NotificationType = overrideEntity.Status == OverrideRequestStatus.Granted ? NotificationTypes.OverrideMediaGranted : NotificationTypes.OverrideMediaRejected,
                            ProfileId = info.ProfileId,
                            Title = "Access Request",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
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
                            && UserProfile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.Unrated)
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
                .Distinct()
                .OrderByDescending(item => item.Timestamp)
                .Select(item => item.MediaEntry)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        Task<List<MediaEntry>> WatchlistAsync(AppDbContext dbInstance, int skip, int take)
        {
            return
                dbInstance.WatchListItems
                    .AsNoTracking()
                    .Where(w => w.ProfileId == UserProfile.Id)
                    .Where(w => dbInstance.TopLevelWatchableMediaByProfileQuery(UserProfile).Any(m => m.Id == w.MediaEntryId))
                    .OrderBy(w => w.Added)
                    .Skip(skip)
                    .Take(take)
                    .Select(w => w.MediaEntry)
                    .ToListAsync();
        }

        Task<List<MediaEntry>> RecentlyAddedAsync(AppDbContext dbInstance, int skip, int take)
        {
            return dbInstance.TopLevelWatchableMediaByProfileQuery(UserProfile)
                .AsNoTracking()
                .ApplySortOrder(SortOrder.Added_Descending)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<MediaEntry>> PopularAsync(AppDbContext dbInstance, int skip, int take)
        {
            return dbInstance.TopLevelWatchableMediaByProfileQuery(UserProfile)
                .AsNoTracking()
                .Where(m => m.Popularity.HasValue)
                .Where(m => m.Popularity > 0)
                .ApplySortOrder(SortOrder.Popularity_Descending)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<MediaEntry>> GenresAsync(AppDbContext dbInstance, Genres genre, int skip, int take)
        {
            long g = (long)genre;


            return dbInstance.TopLevelWatchableMediaByProfileQuery(UserProfile)
                .AsNoTracking()
                .Where(me =>
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
                .ApplySortOrder(SortOrder.Popularity_Descending)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        Task<List<Data.Models.Playlist>> PlaylistsAsync(AppDbContext db, int skip, int take)
        {
            return db.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }



        static List<BasicMedia> ForceGenre(Dictionary<Genres, List<BasicMedia>> dict, Genres g)
        {
            if (!dict.TryGetValue(g, out var list))
            {
                list = new();
                dict.Add(g, list);
            }
            return list;
        }


    }
}
