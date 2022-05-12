using DustyPig.API.v3;
using DustyPig.API.v3.Models;
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
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> List(ListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var movieQ =
                from mediaEntry in DB.MoviesPlayableByProfile(UserProfile)
                select mediaEntry;

            var sortedQ = ApplySortOrder(movieQ, request.Sort);

            var movies = await sortedQ
                .Skip(request.Start)
                .Take(LIST_SIZE)
                .ToListAsync();

            return movies.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Returns the next 100 movies based on start position and sort order. Designed for admin tools, will return all mvoies owned by the account</remarks>
        [HttpGet("{start}")]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicMedia>>> AdminList(int start)
        {
            var movies = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Movie)
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
        public async Task<ActionResult<DetailedMovie>> Details(int id)
        {
            //Get the media entry
            var meQ =
                from mediaEntry in DB.MediaEntries
                    .AsNoTracking()
                    .Include(item => item.Subtitles)
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

                join progress in DB.MediaProgress(UserProfile) on mediaEntry.Id equals progress.MediaEntryId into progressLJ
                from progress in progressLJ.DefaultIfEmpty()

                where mediaEntry.Id == id && mediaEntry.EntryType == MediaTypes.Movie
                select new { mediaEntry, progress };

            var data = await meQ.SingleOrDefaultAsync();
            if (data == null || data.mediaEntry == null)
                return NotFound();


            //See if the movie is searchable and playable by profile
            bool searchable = await DB.MoviesSearchableByProfile(UserAccount, UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();

            if (!searchable)
                return NotFound();

            bool playable = await DB.MoviesPlayableByProfile(UserProfile)
                .Where(item => item.Id == id)
                .AnyAsync();




            //Build the response
            var ret = data.mediaEntry.ToDetailedMovie(playable);

            if (playable)
                ret.InWatchlist = await DB.WatchListItems
                    .AsNoTracking()
                    .Where(item => item.MediaEntryId == id)
                    .Where(item => item.ProfileId == UserProfile.Id)
                    .AnyAsync();

            //Get the media owner
            if (data.mediaEntry.Library.AccountId == UserAccount.Id)
            {
                ret.Owner = data.mediaEntry.Library.Account.Profiles.Single(item => item.IsMain).Name;
            }
            else
            {
                ret.Owner = data.mediaEntry.Library.FriendLibraryShares
                    .Select(item => item.Friendship)
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .First()
                    .GetFriendDisplayNameForAccount(UserAccount.Id);
            }


            // If playable
            if (playable)
                if (data.progress != null)
                    if (data.progress.Played > 1000 && data.progress.Played < (data.mediaEntry.CreditsStartTime ?? data.mediaEntry.Length.Value * 0.9))
                        ret.Played = data.progress.Played;

            return ret;
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any movie owned by the account</remarks>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedMovie>> AdminDetails(int id)
        {
            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .Include(item => item.Subtitles)
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .SingleOrDefaultAsync();



            var ret = mediaEntry.ToDetailedMovie(true);

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
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            //Make sure the library is owned
            var ownedLib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == movieInfo.LibraryId)
                .AnyAsync();
            if (!ownedLib)
                return NotFound(nameof(movieInfo.LibraryId));


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
                Genres = movieInfo.Genres,
                IntroEndTime = movieInfo.IntroEndTime,
                IntroStartTime = movieInfo.IntroStartTime,
                Length = movieInfo.Length,
                LibraryId = movieInfo.LibraryId,
                Rated = movieInfo.Rated,
                SortTitle = StringUtils.SortTitle(movieInfo.Title),
                Title = movieInfo.Title,
                TMDB_Id = movieInfo.TMDB_Id,
                VideoUrl = movieInfo.VideoUrl
            };

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
                return BadRequest($"An movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}");

            //Get popularity
            await UpdatePopularity(newItem);

            //Add the new item
            DB.MediaEntries.Add(newItem);


            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(DB, newItem, GetSearchTerms(newItem, movieInfo.ExtraSearchTerms));

            //People
            await MediaEntryLogic.UpdatePeople(DB, newItem, movieInfo.Cast, movieInfo.Directors, movieInfo.Producers, movieInfo.Writers);


            //Add Subtitles
            if (movieInfo.ExternalSubtitles != null)
                foreach (var srt in movieInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntry = newItem,
                        Name = srt.Name,
                        Url = srt.Url
                    });

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
        public async Task<ActionResult> Update(UpdateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }




            var existingItem = await DB.MediaEntries
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
                .Where(item => item.Id == movieInfo.Id)
                .Where(item => item.EntryType == MediaTypes.Movie)
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
                return CommonResponses.ForbidObject("This account does not own this movie");

            if (!ownedLibs.Contains(movieInfo.LibraryId))
                return NotFound(nameof(movieInfo.LibraryId));



            //Update info
            bool tmdb_changed = existingItem.TMDB_Id != movieInfo.TMDB_Id;

            existingItem.ArtworkUrl = movieInfo.ArtworkUrl;
            existingItem.BackdropUrl = movieInfo.BackdropUrl;
            existingItem.BifUrl = movieInfo.BifUrl;
            existingItem.CreditsStartTime = movieInfo.CreditsStartTime;
            existingItem.Date = movieInfo.Date;
            existingItem.Description = movieInfo.Description;
            existingItem.EntryType = MediaTypes.Movie;
            existingItem.Genres = movieInfo.Genres;
            existingItem.IntroEndTime = movieInfo.IntroEndTime;
            existingItem.IntroStartTime = movieInfo.IntroStartTime;
            existingItem.Length = movieInfo.Length;
            existingItem.LibraryId = movieInfo.LibraryId;
            existingItem.Rated = movieInfo.Rated;
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
                return BadRequest($"An movie already exists with the following parameters: {nameof(movieInfo.LibraryId)}, {nameof(movieInfo.TMDB_Id)}, {nameof(movieInfo.Title)}, {nameof(movieInfo.Date)}");

            //Get popularity
            if (tmdb_changed)
                await UpdatePopularity(existingItem);

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(DB, existingItem, GetSearchTerms(existingItem, movieInfo.ExtraSearchTerms));

            //People
            await MediaEntryLogic.UpdatePeople(DB, existingItem, movieInfo.Cast, movieInfo.Directors, movieInfo.Producers, movieInfo.Writers);


            //Redo Subtitles
            var existingSubtitles = await DB.Subtitles
                .Where(item => item.MediaEntryId == existingItem.Id)
                .ToListAsync();

            if (movieInfo.ExternalSubtitles != null)
                foreach (var srt in movieInfo.ExternalSubtitles)
                    DB.Subtitles.Add(new Subtitle
                    {
                        MediaEntryId = existingItem.Id,
                        Name = srt.Name,
                        Url = srt.Url
                    });


            //Moment of truth!
            await DB.SaveChangesAsync();


            return Ok();
        }


        /// <summary>
        /// Level 3
        /// </summary>
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
        /// <remarks>Request override access to an existing movie</remarks>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.Forbidden)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> RequestAccessOverride(int id) => RequestAccessOverride(id, MediaTypes.Movie);


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
        public Task<ActionResult> SetAccessOverride(API.v3.Models.TitleOverride info) => SetAccessOverride(info, MediaTypes.Movie);



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> UpdatePlaybackProgress(PlaybackProgress hist) => UpdateMediaPlaybackProgress(hist, DB.MoviesPlayableByProfile(UserProfile));

    }
}
