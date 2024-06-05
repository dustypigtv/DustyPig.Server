using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Returns the next 100 movies based on start position and sort order. Designed for admin tools, will return all mvoies owned by the account.
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
                    if (progress.Played >= 60 && progress.Played < (media.CreditsStartTime ?? media.Length.Value * 0.9))
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
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
                
                //.Include(item => item.People)
                //.ThenInclude(item => item.Person)
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

            //Extra Search Terms
            var allTerms = mediaEntry.MediaSearchBridges.Select(item => item.SearchTerm.Term).ToList();
            var coreTerms = mediaEntry.Title.NormalizedQueryString().Tokenize();
            allTerms.RemoveAll(item => coreTerms.Contains(item));
            ret.ExtraSearchTerms = allTerms;

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
                ArtworkSize = movieInfo.ArtworkSize,
                BackdropUrl = movieInfo.BackdropUrl,
                BackdropSize = movieInfo.BackdropSize,
                BifUrl = movieInfo.BifUrl,
                BifSize = movieInfo.BifSize,
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
                VideoUrl = movieInfo.VideoUrl,
                VideoSize = movieInfo.VideoSize
            };
            newItem.SetGenreFlags(movieInfo.Genres);
            newItem.Hash = newItem.ComputeHash();


            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return $"An movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}";
            
            //Add the new item
            DB.MediaEntries.Add(newItem);

            await DB.SaveChangesAsync();

            //TMDB
            if (newItem.TMDB_Id.HasValue)
            {
                var info = await DB.TMDB_Entries
                    .AsNoTracking()
                    .Where(item => item.TMDB_Id == newItem.TMDB_Id.Value)
                    .Where(item => item.MediaType == TMDB_MediaTypes.Movie)
                    .FirstOrDefaultAsync();

                if (info != null)
                {
                    newItem.TMDB_EntryId = info.Id;
                    newItem.Popularity = info.Popularity;

                    //backdrop
                    if (newItem.MovieRating == null || newItem.MovieRating == MovieRatings.None)
                        newItem.MovieRating = info.MovieRating;

                    if (string.IsNullOrWhiteSpace(newItem.Description))
                        newItem.Description = info.Description;

                    if(string.IsNullOrWhiteSpace(newItem.BackdropUrl))
                    {
                        newItem.BackdropUrl = info.BackdropUrl;
                        newItem.BackdropSize = info.BackdropSize;
                    }

                    DB.MediaEntries.Update(newItem);
                    await DB.SaveChangesAsync();
                }
            }


            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(true, newItem, GetSearchTerms(newItem, movieInfo.ExtraSearchTerms));


            //Add Subtitles
            bool save = false;
            if (movieInfo.SRTSubtitles != null && movieInfo.SRTSubtitles.Count > 0)
            {
                foreach (var srt in movieInfo.SRTSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntry = newItem,
                        Name = srt.Name,
                        Url = srt.Url,
                        FileSize = srt.FileSize
                    });
                save = true;
            }

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
                        save = true;
                    }
                }
            }

            if (save)
                await DB.SaveChangesAsync();

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
            bool artwork_changed = existingItem.ArtworkUrl != movieInfo.ArtworkUrl;

            //Don't update Added
            existingItem.ArtworkUrl = movieInfo.ArtworkUrl;
            existingItem.ArtworkSize = movieInfo.ArtworkSize;
            existingItem.BackdropUrl = movieInfo.BackdropUrl;
            existingItem.BackdropSize = movieInfo.BackdropSize;
            existingItem.BifUrl = movieInfo.BifUrl;
            existingItem.BifSize = movieInfo.BifSize;
            existingItem.CreditsStartTime = movieInfo.CreditsStartTime;
            existingItem.Date = movieInfo.Date;
            existingItem.Description = movieInfo.Description;
            existingItem.SetGenreFlags(movieInfo.Genres);
            existingItem.IntroEndTime = movieInfo.IntroEndTime;
            existingItem.IntroStartTime = movieInfo.IntroStartTime;
            existingItem.Length = movieInfo.Length;
            existingItem.LibraryId = movieInfo.LibraryId;
            existingItem.MovieRating = movieInfo.Rated;
            existingItem.SortTitle = StringUtils.SortTitle(movieInfo.Title);
            existingItem.Title = movieInfo.Title;
            existingItem.TMDB_Id = movieInfo.TMDB_Id;
            existingItem.VideoUrl = movieInfo.VideoUrl;
            existingItem.VideoSize = movieInfo.VideoSize;

            existingItem.Hash = existingItem.ComputeHash();

            //Dup check
            var dup = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id != existingItem.Id)
                .Where(item => item.LibraryId == existingItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .Where(item => item.TMDB_Id == existingItem.TMDB_Id)
                .Where(item => item.Hash == existingItem.Hash)
                .AnyAsync();

            if (dup)
                return $"An movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}";

            
            List<int> playlistIds = null;
            if (artwork_changed)
            {
                playlistIds = await DB.PlaylistItems
                    .AsNoTracking()
                    .Where(item => item.MediaEntryId == movieInfo.Id)
                    .Select(item => item.PlaylistId)
                    .Distinct()
                    .ToListAsync();
            }

            await DB.SaveChangesAsync();

            //TMDB
            if (existingItem.TMDB_Id.HasValue)
            {
                var info = await DB.TMDB_Entries
                    .AsNoTracking()
                    .Where(item => item.TMDB_Id == existingItem.TMDB_Id.Value)
                    .Where(item => item.MediaType == TMDB_MediaTypes.Movie)
                    .FirstOrDefaultAsync();

                if (info != null)
                {
                    existingItem.TMDB_EntryId = info.Id;
                    existingItem.Popularity = info.Popularity;

                    if (existingItem.MovieRating == null || existingItem.MovieRating == MovieRatings.None)
                        existingItem.MovieRating = info.MovieRating;

                    if (string.IsNullOrWhiteSpace(existingItem.Description))
                        existingItem.Description = info.Description;

                    if (string.IsNullOrWhiteSpace(existingItem.BackdropUrl))
                    {
                        existingItem.BackdropUrl = info.BackdropUrl;
                        existingItem.BackdropSize = info.BackdropSize;
                    }

                    DB.MediaEntries.Update(existingItem);
                    await DB.SaveChangesAsync();
                }
            }


            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(false, existingItem, GetSearchTerms(existingItem, movieInfo.ExtraSearchTerms));

            //Playlists
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            //Redo Subtitles
            var existingSubtitles = await DB.Subtitles
                .Where(item => item.MediaEntryId == existingItem.Id)
                .ToListAsync();

            if (existingSubtitles.Count > 0)
            {
                DB.Subtitles.RemoveRange(existingSubtitles);
                await DB.SaveChangesAsync();
            }

            if (movieInfo.SRTSubtitles != null && movieInfo.SRTSubtitles.Count > 0)
            {
                foreach (var srt in movieInfo.SRTSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntryId = existingItem.Id,
                        Name = srt.Name,
                        Url = srt.Url,
                        FileSize = srt.FileSize
                    });

                await DB.SaveChangesAsync();
            }

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
                .AsNoTracking()
                .Include(e => e.ProfileMediaProgress.Where(p => p.ProfileId == UserProfile.Id))
                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .FirstOrDefaultAsync();

            if (movie == null || !movie.ProfileMediaProgress.Any())
                return CommonResponses.ValueNotFound(nameof(id));

            DB.ProfileMediaProgresses.Remove(movie.ProfileMediaProgress.First());
            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }

    }
}
