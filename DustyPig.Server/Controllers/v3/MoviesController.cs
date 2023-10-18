using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public MoviesController(AppDbContext db, TMDBClient tmdbClient) : base(db, tmdbClient)
        {
        }

        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 movies based on start position and sort order</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<List<BasicMedia>>> List(ListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<List<BasicMedia>>(ex.ToString()); }
                       

            var movies = await DB.MediaEntries
                .AsNoTracking()

                .Where(m => m.EntryType == MediaTypes.Movie)
                .Where(m =>

                    m.TitleOverrides
                        .Where(t => t.ProfileId == UserProfile.Id)
                        .Where(t => t.State == OverrideState.Allow)
                        .Any()
                    ||
                    (
                        UserProfile.IsMain
                        &&
                        (
                            m.Library.AccountId == UserAccount.Id
                            ||
                            (
                                m.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == UserAccount.Id || f.Friendship.Account2Id == UserAccount.Id)
                                && !m.TitleOverrides
                                    .Where(t => t.ProfileId == UserProfile.Id)
                                    .Where(t => t.State == OverrideState.Block)
                                    .Any()
                            )
                        )
                    )
                    ||
                    (
                        m.Library.ProfileLibraryShares.Any(p => p.ProfileId == UserProfile.Id)
                        && UserProfile.MaxMovieRating >= (m.MovieRating ?? MovieRatings.Unrated)
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == UserProfile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                )

                .ApplySortOrder(request.Sort)
                .Skip(request.Start)
                .Take(DEFAULT_LIST_SIZE)
                .ToListAsync();

            

            return new ResponseWrapper<List<BasicMedia>>(movies.Select(item => item.ToBasicMedia()).ToList());
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Returns the next 100 movies based on start position and sort order. Designed for admin tools, will return all mvoies owned by the account</remarks>
        [HttpGet("{start}/{libId}")]
        [RequireMainProfile]
        public async Task<ResponseWrapper<List<BasicMedia>>> AdminList(int start, int libId)
        {
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

            return new ResponseWrapper<List<BasicMedia>>(movies.Select(item => item.ToBasicMedia()).ToList());
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper<DetailedMovie>> Details(int id)
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

                .Include(item => item.People)
                .ThenInclude(item => item.Person)

                .Include(item => item.WatchlistItems.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.ProfileMediaProgress.Where(item2 => item2.ProfileId == UserProfile.Id))

                .Include(item => item.Subtitles)

                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .FirstOrDefaultAsync();

            if (media == null)
                return CommonResponses.NotFound<DetailedMovie>();

            if (media.Library.AccountId != UserAccount.Id)
                if (!media.Library.FriendLibraryShares.Any())
                    if (!media.TitleOverrides.Any())
                        if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                            return CommonResponses.NotFound<DetailedMovie>();

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

            return new ResponseWrapper<DetailedMovie>(ret);
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any movie owned by the account</remarks>
        [HttpGet("{id}")]
        [RequireMainProfile]
        public async Task<ResponseWrapper<DetailedMovie>> AdminDetails(int id)
        {
            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Include(Item => Item.Library)
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Include(item => item.Subtitles)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound<DetailedMovie>();

            var ret = mediaEntry.ToDetailedMovie(true);

            //Extra Search Terms
            var allTerms = mediaEntry.MediaSearchBridges.Select(item => item.SearchTerm.Term).ToList();
            var coreTerms = mediaEntry.Title.NormalizedQueryString().Tokenize();
            allTerms.RemoveAll(item => coreTerms.Contains(item));
            ret.ExtraSearchTerms = allTerms;

            ret.CanManage = true;

            return new ResponseWrapper<DetailedMovie>(ret);
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper<SimpleValue<int>>> Create(CreateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }


            //Make sure the library is owned
            var ownedLib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == movieInfo.LibraryId)
                .AnyAsync();
            if (!ownedLib)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(movieInfo.LibraryId));


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
                return new ResponseWrapper<SimpleValue<int>>($"An movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}");

            //Get popularity
            await UpdatePopularity(newItem);

            //Add the new item
            DB.MediaEntries.Add(newItem);

            await DB.SaveChangesAsync();

            //People
            await MediaEntryLogic.UpdatePeople(true, newItem, movieInfo.Cast, movieInfo.Directors, movieInfo.Producers, movieInfo.Writers);

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(true, newItem, GetSearchTerms(newItem, movieInfo.ExtraSearchTerms));


            //Add Subtitles
            bool save = false;
            if (movieInfo.ExternalSubtitles != null && movieInfo.ExternalSubtitles.Count > 0)
            {
                foreach (var srt in movieInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntry = newItem,
                        Name = srt.Name,
                        Url = srt.Url
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
                            NotificationType = NotificationType.GetRequest,
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

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int>(newItem.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Update(UpdateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }


            var existingItem = await DB.MediaEntries
                .Where(item => item.Id == movieInfo.Id)
                .Where(item => item.EntryType == MediaTypes.Movie)
                .FirstOrDefaultAsync();

            if (existingItem == null)
                return CommonResponses.NotFound();

            //Make sure this item is owned
            var ownedLibs = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Select(item => item.Id)
                .ToListAsync();

            if (!ownedLibs.Contains(existingItem.LibraryId))
                return CommonResponses.NotFound(nameof(movieInfo.Id));

            if (!ownedLibs.Contains(movieInfo.LibraryId))
                return CommonResponses.NotFound(nameof(movieInfo.LibraryId));



            //Update info
            bool tmdb_changed = existingItem.TMDB_Id != movieInfo.TMDB_Id;
            bool artwork_changed = existingItem.ArtworkUrl != movieInfo.ArtworkUrl;

            //Don't update Added

            existingItem.ArtworkUrl = movieInfo.ArtworkUrl;
            existingItem.BackdropUrl = movieInfo.BackdropUrl;
            existingItem.BifUrl = movieInfo.BifUrl;
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
                return new ResponseWrapper($"An movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}");

            //Get popularity
            if (tmdb_changed)
                await UpdatePopularity(existingItem);

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

            //People
            await MediaEntryLogic.UpdatePeople(false, existingItem, movieInfo.Cast, movieInfo.Directors, movieInfo.Producers, movieInfo.Writers);

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

            if (movieInfo.ExternalSubtitles != null && movieInfo.ExternalSubtitles.Count > 0)
            {
                foreach (var srt in movieInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntryId = existingItem.Id,
                        Name = srt.Name,
                        Url = srt.Url
                    });

                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        public Task<ResponseWrapper> Delete(int id) => DeleteMedia(id);
    }
}
