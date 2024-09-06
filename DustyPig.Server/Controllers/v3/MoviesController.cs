using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(MoviesController))]
    public class MoviesController : _MediaControllerBase
    {
        public MoviesController(AppDbContext db) : base(db)
        {
        }

        /// <summary>
        /// Requires profile
        /// </summary>
        /// <remarks>Returns the next 25 movies based on start position and sort order</remarks>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> List(ListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            var movies = await DB.WatchableMoviesByProfileQuery(UserProfile)
                .AsNoTracking()
                .ApplySortOrder(request.Sort)
                .Skip(request.Start)
                .Take(DEFAULT_LIST_SIZE)
                .ToListAsync();



            return movies.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>
        /// Returns the next 100 movies based on start position. Designed for admin tools, will return all mvoies owned by the account.
        /// If you specify a value > 0 for libId, it will filter on movies only in the specified library
        /// </remarks>
        [HttpGet("{start}/{libId}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> AdminList(int start, int libId)
        {
            if (start < 0)
                return CommonResponses.InvalidValue(nameof(start));

            var q = DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Movie);

            if (libId > 0)
                q = q.Where(item => item.LibraryId == libId);

            var movies = await q
                 .AsNoTracking()
                 .ApplySortOrder(SortOrder.Alphabetical)
                 .Skip(start)
                 .Take(ADMIN_LIST_SIZE)
                 .ToListAsync();

            return movies.Select(item => item.ToBasicMedia()).ToList();
        }






        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>
        /// Returns the next 100 movies based on start position. Designed for admin tools, will return all mvoies owned by the account that have never been played.
        /// If you specify a value > 0 for libId, it will filter on movies only in the specified library
        /// </remarks>
        [HttpGet("{start}/{libId}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> GetNeverPlayed(int start, int libId)
        {
            if (start < 0)
                return CommonResponses.InvalidValue(nameof(start));

            var q = DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.EverPlayed == false)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Movie);

            if (libId > 0)
                q = q.Where(item => item.LibraryId == libId);

            var movies = await q
                 .AsNoTracking()
                 .ApplySortOrder(SortOrder.Alphabetical)
                 .Skip(start)
                 .Take(ADMIN_LIST_SIZE)
                 .ToListAsync();

            return movies.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>
        /// Returns the next 100 movies based on start position. Designed for admin tools, will return all mvoies owned by the account that have ever been played.
        /// If you specify a value > 0 for libId, it will filter on movies only in the specified library
        /// </remarks>
        [HttpGet("{start}/{libId}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> GetEverPlayed(int start, int libId)
        {
            if (start < 0)
                return CommonResponses.InvalidValue(nameof(start));

            var q = DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.EverPlayed == true)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Movie);

            if (libId > 0)
                q = q.Where(item => item.LibraryId == libId);

            var movies = await q
                 .AsNoTracking()
                 .ApplySortOrder(SortOrder.Alphabetical)
                 .Skip(start)
                 .Take(ADMIN_LIST_SIZE)
                 .ToListAsync();

            return movies.Select(item => item.ToBasicMedia()).ToList();
        }






        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedMovie>))]
        public async Task<Result<DetailedMovie>> Details(int id)
        {
            var media = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .ThenInclude(item => item.Account)
                .ThenInclude(item => item.Profiles)

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
                .ThenInclude(item => item.ProfileLibraryShares.Where(item2 => item2.Profile.Id == UserProfile.Id))

                .Include(item => item.TitleOverrides.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.TMDB_Entry)
                .ThenInclude(item => item.People)
                .ThenInclude(item => item.TMDB_Person)


                .Include(item => item.WatchlistItems.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.ProfileMediaProgress.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.Subtitles)

                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .FirstOrDefaultAsync();

            if (media == null)
                return CommonResponses.ValueNotFound(nameof(id));

            if (media.Library.AccountId != UserAccount.Id)
                if (!media.Library.FriendLibraryShares.Any())
                    if (!media.TitleOverrides.Any())
                        if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                            return CommonResponses.ValueNotFound(nameof(id));

            bool playable = (UserProfile.IsMain)
                || media.TitleOverrides.Any(item => item.State == OverrideState.Allow)
                ||
                (
                    !media.TitleOverrides.Any(item => item.State == OverrideState.Block)
                    &&
                    (
                        media.Library.ProfileLibraryShares.Any()
                        && UserProfile.MaxMovieRating >= (media.MovieRating ?? MovieRatings.Unrated)
                    )
                );



            //Build the response
            var ret = media.ToDetailedMovie(playable);
            ret.TitleRequestPermission = TitleRequestLogic.GetTitleRequestPermissions(UserAccount, UserProfile, media.Library.FriendLibraryShares.Any());



            if (playable)
                ret.InWatchlist = media.WatchlistItems.Any();

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

            // If playable
            if (playable)
            {
                var progress = media.ProfileMediaProgress.FirstOrDefault();
                if (progress != null)
                    if (progress.Played >= 1 && progress.Played < (media.CreditsStartTime ?? media.Length.Value * 0.9))
                        ret.Played = progress.Played;
            }
            else
            {
                var overrideRequest = media.TitleOverrides.FirstOrDefault(item => item.ProfileId == UserProfile.Id);
                if (overrideRequest != null)
                    ret.AccessRequestedStatus = overrideRequest.Status;
            }

            ret.CanManage = UserProfile.IsMain
                &&
                (
                    UserAccount.Profiles.Count > 1
                    ||
                    (
                        media.Library.AccountId == UserAccount.Id
                        && media.Library.FriendLibraryShares.Count > 0
                    )
                );

            return ret;
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any movie owned by the account</remarks>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedMovie>))]
        public async Task<Result<DetailedMovie>> AdminDetails(int id)
        {
            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Include(Item => Item.Library)
                .Include(item => item.ExtraSearchTerms)

                .Include(item => item.TMDB_Entry)
                .ThenInclude(item => item.People)
                .ThenInclude(item => item.TMDB_Person)

                .Include(item => item.Subtitles)

                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var ret = mediaEntry.ToDetailedMovie(true);
            ret.CanManage = true;

            return ret;
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<int>))]
        public async Task<Result<int>> Create(CreateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            //Make sure the library is owned
            var ownedLib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == movieInfo.LibraryId)
                .AnyAsync();
            if (!ownedLib)
                return CommonResponses.ValueNotFound(nameof(movieInfo.LibraryId));


            // ***** Ok at this point the mediaInfo has all required data, build the new entries *****
            var newItem = new MediaEntry
            {
                Added = DateTime.UtcNow,
                ArtworkUrl = movieInfo.ArtworkUrl,
                BackdropUrl = movieInfo.BackdropUrl,
                BifUrl = movieInfo.BifUrl,
                CreditsStartTime = movieInfo.CreditsStartTime,
                Date = movieInfo.Date,
                Description = movieInfo.Description,
                EntryType = MediaTypes.Movie,
                IntroEndTime = movieInfo.IntroEndTime,
                IntroStartTime = movieInfo.IntroStartTime,
                Length = movieInfo.Length,
                LibraryId = movieInfo.LibraryId,
                MovieRating = movieInfo.Rated,
                SortTitle = StringUtils.SortTitle(movieInfo.Title),
                Title = movieInfo.Title,
                TMDB_Id = movieInfo.TMDB_Id,
                VideoUrl = movieInfo.VideoUrl
            };


            //Dup check
            newItem.ComputeHash();
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return $"A movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}";


            var tmdbInfo = await GetTMDBInfoAsync(newItem.TMDB_Id, TMDB_MediaTypes.Movie);
            newItem.SetOtherInfo(movieInfo.ExtraSearchTerms, movieInfo.SRTSubtitles, movieInfo.Genres, tmdbInfo);


            //Save
            DB.MediaEntries.Add(newItem);
            await DB.SaveChangesAsync();


            //Notifications
            if (newItem.TMDB_Id > 0)
            {
                var getRequest = await DB.GetRequests
                    .Include(item => item.NotificationSubscriptions)
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                    .Where(item => item.EntryType == TMDB_MediaTypes.Movie)
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
                            Message = "\"" + newItem.Title + "\" is now availble!",
                            NotificationType = NotificationTypes.NewMediaFulfilled,
                            ProfileId = sub.ProfileId,
                            Timestamp = DateTime.UtcNow,
                            Title = "Your Movie Is Now Available"
                        });

                        DB.GetRequestSubscriptions.Remove(sub);
                        await DB.SaveChangesAsync();
                    }
                }
            }


            return newItem.Id;
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Update(UpdateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            var existingItem = await DB.MediaEntries
                .Include(item => item.ExtraSearchTerms)
                .Include(item => item.Subtitles)
                .Where(item => item.Id == movieInfo.Id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .FirstOrDefaultAsync();

            if (existingItem == null)
                return CommonResponses.ValueNotFound(nameof(movieInfo.Id));

            //Make sure this item is owned
            var ownedLibs = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Select(item => item.Id)
                .ToListAsync();

            if (!ownedLibs.Contains(existingItem.LibraryId))
                return CommonResponses.ValueNotFound(nameof(movieInfo.Id));

            if (!ownedLibs.Contains(movieInfo.LibraryId))
                return CommonResponses.ValueNotFound(nameof(movieInfo.LibraryId));



            //Update info
            bool library_changed = existingItem.LibraryId != movieInfo.LibraryId;
            bool rated_changed = existingItem.MovieRating != movieInfo.Rated;
            bool artwork_changed = existingItem.ArtworkUrl != movieInfo.ArtworkUrl;

            //Don't update Added
            existingItem.ArtworkUrl = movieInfo.ArtworkUrl;
            existingItem.BackdropUrl = movieInfo.BackdropUrl;
            existingItem.BifUrl = movieInfo.BifUrl;
            existingItem.CreditsStartTime = movieInfo.CreditsStartTime;
            existingItem.Date = movieInfo.Date;
            existingItem.Description = movieInfo.Description;
            existingItem.IntroEndTime = movieInfo.IntroEndTime;
            existingItem.IntroStartTime = movieInfo.IntroStartTime;
            existingItem.Length = movieInfo.Length;
            existingItem.LibraryId = movieInfo.LibraryId;
            existingItem.MovieRating = movieInfo.Rated;
            existingItem.SortTitle = StringUtils.SortTitle(movieInfo.Title);
            existingItem.Title = movieInfo.Title;
            existingItem.TMDB_Id = movieInfo.TMDB_Id;
            existingItem.VideoUrl = movieInfo.VideoUrl;


            //Dup check
            existingItem.ComputeHash();
            var dup = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id != existingItem.Id)
                .Where(item => item.LibraryId == existingItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .Where(item => item.TMDB_Id == existingItem.TMDB_Id)
                .Where(item => item.Hash == existingItem.Hash)
                .AnyAsync();

            if (dup)
                return $"A movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}";


            var tmdbInfo = await GetTMDBInfoAsync(existingItem.TMDB_Id, TMDB_MediaTypes.Movie);
            existingItem.SetOtherInfo(movieInfo.ExtraSearchTerms, movieInfo.SRTSubtitles, movieInfo.Genres, tmdbInfo);


            //Save
            await DB.SaveChangesAsync();


            //Playlists
            List<int> playlistIds = null;
            if (library_changed || rated_changed || artwork_changed)
                playlistIds = await DB.PlaylistItems
                    .AsNoTracking()
                    .Where(item => item.MediaEntryId == movieInfo.Id)
                    .Select(item => item.PlaylistId)
                    .Distinct()
                    .ToListAsync();
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);


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


        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> MarkWatched(int id)
        {
            var movie = await DB.TopLevelWatchableMediaByProfileQuery(UserProfile)
                .Include(e => e.ProfileMediaProgress.Where(p => p.ProfileId == UserProfile.Id))
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .FirstOrDefaultAsync();

            if (movie == null || !movie.ProfileMediaProgress.Any())
                return CommonResponses.ValueNotFound(nameof(id));

            movie.EverPlayed = true;
            DB.ProfileMediaProgresses.Remove(movie.ProfileMediaProgress.First());
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }





        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Designed for admin tools, this will search for any movie owned by the account</remarks>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public Task<Result<List<BasicMedia>>> AdminSearch([FromQuery] int libraryId, [FromBody] SearchRequest request) =>
            AdminSearchAsync(libraryId, request.Query, MediaTypes.Movie);


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any movie owned by the account with the specified tmdb id</remarks>
        [HttpGet]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public Task<Result<List<BasicMedia>>> AdminSearchByTmdbId([FromQuery] int libraryId, [FromQuery] int tmdbId) =>
            AdminSearchByTmdbIdAsync(libraryId, tmdbId, MediaTypes.Movie);

    }
}
