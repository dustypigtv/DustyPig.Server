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
    [ExceptionLogger(typeof(MoviesController))]
    public class MoviesController : _MediaControllerBase
    {
        public MoviesController(AppDbContext db, TMDBClient tmdbClient, IMemoryCache memoryCache) : base(db, tmdbClient, memoryCache)
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
                    .ThenInclude(item => item.ServiceCredential)
                    .Include(item => item.VideoServiceCredential)
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

                join progress in MediaProgress on mediaEntry.Id equals progress.MediaEntryId into progressLJ
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
            var ret = new DetailedMovie
            {
                ArtworkUrl = data.mediaEntry.ArtworkUrl,
                BifAsset = playable ? Utils.GetAsset(data.mediaEntry.BifServiceCredential, _memoryCache, data.mediaEntry.BifUrl) : null,
                Cast = data.mediaEntry.GetPeople(Roles.Cast),
                CreditsStartTime = data.mediaEntry.CreditsStartTime,
                Date = data.mediaEntry.Date.Value,
                Description = data.mediaEntry.Description,
                Directors = data.mediaEntry.GetPeople(Roles.Director),
                Genres = data.mediaEntry.Genres.Value,
                Id = data.mediaEntry.Id,
                IntroEndTime = data.mediaEntry.IntroEndTime,
                IntroStartTime = data.mediaEntry.IntroStartTime,
                Length = data.mediaEntry.Length.Value,
                LibraryId = data.mediaEntry.LibraryId,
                Producers = data.mediaEntry.GetPeople(Roles.Producer),
                Rated = (data.mediaEntry.Rated ?? Ratings.None),
                Title = data.mediaEntry.Title + $" ({data.mediaEntry.Date.Value.Year})",
                TMDB_Id = data.mediaEntry.TMDB_Id,
                VideoAsset = playable ? Utils.GetAsset(data.mediaEntry.VideoServiceCredential, _memoryCache, data.mediaEntry.VideoUrl) : null,
                Writers = data.mediaEntry.GetPeople(Roles.Writer)
            };


            //Subs
            if (data.mediaEntry.Subtitles != null && data.mediaEntry.Subtitles.Count > 0)
            {
                data.mediaEntry.Subtitles.Sort();
                ret.ExternalSubtitles = new List<ExternalSubtitle>();
                foreach (var item in data.mediaEntry.Subtitles)
                {
                    var asset = playable ? Utils.GetAsset(item.ServiceCredential, _memoryCache, item.Url) : null;
                    var xs = new ExternalSubtitle { Name = item.Name };
                    if (asset != null)
                    {
                        xs.ExpiresUTC = asset.ExpiresUTC;
                        xs.ServiceCredentialId = asset.ServiceCredentialId;
                        xs.Token = asset.Token;
                        xs.Url = asset.Url;
                    }
                    ret.ExternalSubtitles.Add(xs);
                }
            }


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
                .Include(item => item.VideoServiceCredential)
                .Include(item => item.BifServiceCredential)
                .Include(item => item.Subtitles)
                .ThenInclude(item => item.ServiceCredential)
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .SingleOrDefaultAsync();
            



            //Build the response
            var ret = new DetailedMovie
            {
                ArtworkUrl = mediaEntry.ArtworkUrl,
                BifAsset = Utils.GetAsset(mediaEntry.BifServiceCredential, _memoryCache, mediaEntry.BifUrl),
                Cast = mediaEntry.GetPeople(Roles.Cast),
                CreditsStartTime = mediaEntry.CreditsStartTime,
                Date = mediaEntry.Date.Value,
                Description = mediaEntry.Description,
                Directors = mediaEntry.GetPeople(Roles.Director),
                Genres = mediaEntry.Genres.Value,
                Id = mediaEntry.Id,
                IntroEndTime = mediaEntry.IntroEndTime,
                IntroStartTime = mediaEntry.IntroStartTime,
                Length = mediaEntry.Length.Value,
                LibraryId = mediaEntry.LibraryId,
                Producers = mediaEntry.GetPeople(Roles.Producer),
                Rated = (mediaEntry.Rated ?? Ratings.None),
                Title = mediaEntry.Title + $" ({mediaEntry.Date.Value.Year})",
                TMDB_Id = mediaEntry.TMDB_Id,
                VideoAsset = Utils.GetAsset(mediaEntry.VideoServiceCredential, _memoryCache, mediaEntry.VideoUrl),
                Writers = mediaEntry.GetPeople(Roles.Writer)
            };

            if (ret.BifAsset != null)
                ret.BifAsset = new StreamingAsset { AssetType = ret.BifAsset.AssetType, ServiceCredentialId = mediaEntry.BifServiceCredentialId, Url = mediaEntry.BifUrl };
            ret.VideoAsset = new StreamingAsset { AssetType = ret.VideoAsset.AssetType, ServiceCredentialId = mediaEntry.VideoServiceCredentialId, Url = mediaEntry.VideoUrl };
           


            //Subs
            if (mediaEntry.Subtitles != null && mediaEntry.Subtitles.Count > 0)
            {
                mediaEntry.Subtitles.Sort();
                ret.ExternalSubtitles = new List<ExternalSubtitle>();
                foreach (var item in mediaEntry.Subtitles)
                {
                    var asset = Utils.GetAsset(item.ServiceCredential, _memoryCache, item.Url);
                    asset = new StreamingAsset { AssetType = asset.AssetType, ServiceCredentialId = item.ServiceCredentialId, Url = item.Url };
                    var xs = new ExternalSubtitle { Name = item.Name };
                    xs.ExpiresUTC = asset.ExpiresUTC;
                    xs.ServiceCredentialId = asset.ServiceCredentialId;
                    xs.Token = asset.Token;
                    xs.Url = asset.Url;
                    ret.ExternalSubtitles.Add(xs);
                }
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
        public async Task<ActionResult<SimpleValue<int>>> Create(CreateMovie movieInfo)
        {
            // ***** Tons of validation *****
            try { movieInfo.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }




            //Make sure any credential ids are owned
            if (movieInfo.VideoAsset.ServiceCredentialId != null || (movieInfo.BifAsset != null && movieInfo.BifAsset.ServiceCredentialId != null))
            {
                var acctCredentialIds = await DB.EncryptedServiceCredentials
                    .AsNoTracking()
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Select(item => item.Id)
                    .ToListAsync();

                if (movieInfo.BifAsset != null && movieInfo.BifAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(movieInfo.BifAsset.ServiceCredentialId.Value))
                        return NotFound($"{nameof(CreateMovie.BifAsset)}.{nameof(movieInfo.BifAsset.ServiceCredentialId)}");

                if (movieInfo.VideoAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(movieInfo.VideoAsset.ServiceCredentialId.Value))
                        return NotFound(nameof(movieInfo.VideoAsset.ServiceCredentialId));

                if (movieInfo.ExternalSubtitles != null)
                    foreach (var subtitle in movieInfo.ExternalSubtitles)
                        if (subtitle.ServiceCredentialId != null)
                            if (!acctCredentialIds.Contains(subtitle.ServiceCredentialId.Value))
                                return NotFound($"{nameof(CreateExternalSubtitle)}.{nameof(subtitle.ServiceCredentialId)}");
            }

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
                BifUrl = movieInfo.BifAsset?.Url,
                BifServiceCredentialId = movieInfo.BifAsset?.ServiceCredentialId,
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
                VideoServiceCredentialId = movieInfo.VideoAsset.ServiceCredentialId,
                VideoUrl = movieInfo.VideoAsset.Url
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
                        ServiceCredentialId = srt.ServiceCredentialId,
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


            //Make sure any credential ids are owned
            if (movieInfo.VideoAsset.ServiceCredentialId != null || (movieInfo.BifAsset != null && movieInfo.BifAsset.ServiceCredentialId != null))
            {
                var acctCredentialIds = await DB.EncryptedServiceCredentials
                    .AsNoTracking()
                    .Where(item => item.AccountId == UserAccount.Id)
                    .Select(item => item.Id)
                    .ToListAsync();

                if (movieInfo.BifAsset != null && movieInfo.BifAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(movieInfo.BifAsset.ServiceCredentialId.Value))
                        return NotFound($"{nameof(CreateMovie.BifAsset)}.{nameof(movieInfo.BifAsset.ServiceCredentialId)}");

                if (movieInfo.VideoAsset.ServiceCredentialId != null)
                    if (!acctCredentialIds.Contains(movieInfo.VideoAsset.ServiceCredentialId.Value))
                        return NotFound(nameof(movieInfo.VideoAsset.ServiceCredentialId));

                if (movieInfo.ExternalSubtitles != null)
                    foreach (var subtitle in movieInfo.ExternalSubtitles)
                        if (subtitle.ServiceCredentialId != null)
                            if (!acctCredentialIds.Contains(subtitle.ServiceCredentialId.Value))
                                return NotFound($"{nameof(CreateExternalSubtitle)}.{nameof(subtitle.ServiceCredentialId)}");
            }



            //Update info
            bool tmdb_changed = existingItem.TMDB_Id != movieInfo.TMDB_Id;

            existingItem.ArtworkUrl = movieInfo.ArtworkUrl;
            existingItem.BifUrl = movieInfo.BifAsset?.Url;
            existingItem.BifServiceCredentialId = movieInfo.BifAsset?.ServiceCredentialId;
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
            existingItem.VideoServiceCredentialId = movieInfo.VideoAsset.ServiceCredentialId;
            existingItem.VideoUrl = movieInfo.VideoAsset.Url;

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
                        ServiceCredentialId = srt.ServiceCredentialId,
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
