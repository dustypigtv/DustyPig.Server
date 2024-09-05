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
using SixLabors.ImageSharp;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(SeriesController))]
    public class SeriesController : _MediaControllerBase
    {
        public SeriesController(AppDbContext db) : base(db)
        {
        }


        /// <summary>
        /// Requires profile
        /// </summary>
        /// <remarks>Returns the next 100 series based on start position and sort order</remarks>
        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> List(ListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            var series = await DB.WatchableSeriesByProfileQuery(UserProfile)
                .AsNoTracking()
                .ApplySortOrder(request.Sort)
                .Skip(request.Start)
                .Take(DEFAULT_LIST_SIZE)
                .ToListAsync();

            return series.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>
        /// Returns the next 100 series based on start position. Designed for admin tools, will return all series owned by the account.
        /// If you specify libId > 0, this will filter on series in that library
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
                .Where(item => item.EntryType == MediaTypes.Series);

            if (libId > 0)
                q = q.Where(item => item.LibraryId == libId);

            var series = await q
                 .AsNoTracking()
                 .ApplySortOrder(SortOrder.Alphabetical)
                 .Skip(start)
                 .Take(ADMIN_LIST_SIZE)
                 .ToListAsync();

            return series.Select(item => item.ToBasicMedia()).ToList();
        }





        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>
        /// Returns the next 100 series based on start position. Designed for admin tools, will return all series owned by the account that have never been played.
        /// If you specify libId > 0, this will filter on series in that library
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
                .Where(item => item.EntryType == MediaTypes.Series);

            if (libId > 0)
                q = q.Where(item => item.LibraryId == libId);

            var series = await q
                 .AsNoTracking()
                 .ApplySortOrder(SortOrder.Alphabetical)
                 .Skip(start)
                 .Take(ADMIN_LIST_SIZE)
                 .ToListAsync();

            return series.Select(item => item.ToBasicMedia()).ToList();
        }




        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>
        /// Returns the next 100 series based on start position. Designed for admin tools, will return all series owned by the account that have ever been played.
        /// If you specify libId > 0, this will filter on series in that library
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
                .Where(item => item.EntryType == MediaTypes.Series);

            if (libId > 0)
                q = q.Where(item => item.LibraryId == libId);

            var series = await q
                 .AsNoTracking()
                 .ApplySortOrder(SortOrder.Alphabetical)
                 .Skip(start)
                 .Take(ADMIN_LIST_SIZE)
                 .ToListAsync();

            return series.Select(item => item.ToBasicMedia()).ToList();
        }





        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedSeries>))]
        public async Task<Result<DetailedSeries>> Details(int id)
        {
            var ret = await GetSeriesDetailsAsync(id);
            if (ret == null)
                return CommonResponses.ValueNotFound(nameof(id));
            return ret;
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any series owned by the account</remarks>
        [HttpGet("{id}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedSeries>))]
        public async Task<Result<DetailedSeries>> AdminDetails(int id)
        {
            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Include(Item => Item.Library)
                .Include(item => item.ExtraSearchTerms)
                .Include(item => item.TMDB_Entry)
                .ThenInclude(item => item.People)
                .ThenInclude(item => item.TMDB_Person)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.ValueNotFound(nameof(id));

            //Build the response
            var ret = mediaEntry.ToAdminDetailedSeries();

            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();

            ret.Episodes = dbEps.ToAdminDetailedEpisodeList();

            return ret;
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<int>))]
        public async Task<Result<int>> Create(CreateSeries seriesInfo)
        {
            // ***** Tons of validation *****
            try { seriesInfo.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            //Make sure the library is owned
            var ownedLib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == seriesInfo.LibraryId)
                .AnyAsync();
            if (!ownedLib)
                return CommonResponses.ValueNotFound(nameof(seriesInfo.LibraryId));


            var newItem = new MediaEntry
            {
                Added = DateTime.UtcNow,
                ArtworkUrl = seriesInfo.ArtworkUrl,
                BackdropUrl = seriesInfo.BackdropUrl,
                Description = seriesInfo.Description,
                EntryType = MediaTypes.Series,
                LibraryId = seriesInfo.LibraryId,
                TVRating = seriesInfo.Rated,
                SortTitle = StringUtils.SortTitle(seriesInfo.Title),
                Title = seriesInfo.Title,
                TMDB_Id = seriesInfo.TMDB_Id
            };


            //Dup check
            newItem.ComputeHash();
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return $"An series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}";


            var tmdbInfo = await GetTMDBInfoAsync(newItem.TMDB_Id, TMDB_MediaTypes.Series);
            newItem.SetOtherInfo(seriesInfo.ExtraSearchTerms, null, seriesInfo.Genres, tmdbInfo);


            //Add the new item
            DB.MediaEntries.Add(newItem);
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
        public async Task<Result> Update(UpdateSeries seriesInfo)
        {
            // ***** Tons of validation *****
            try { seriesInfo.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            var existingItem = await DB.MediaEntries
                .Where(item => item.Id == seriesInfo.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (existingItem == null)
                return CommonResponses.ValueNotFound(nameof(seriesInfo.Id));

            //Make sure this item is owned
            var ownedLibs = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Select(item => item.Id)
                .ToListAsync();

            if (!ownedLibs.Contains(existingItem.LibraryId))
                return "This account does not own this series";

            if (!ownedLibs.Contains(seriesInfo.LibraryId))
                return CommonResponses.ValueNotFound(nameof(seriesInfo.LibraryId));


            //Update info
            bool library_changed = existingItem.LibraryId != seriesInfo.LibraryId;
            bool rated_changed = existingItem.TVRating != seriesInfo.Rated;
            bool artwork_changed = existingItem.ArtworkUrl != seriesInfo.ArtworkUrl;

            existingItem.ArtworkUrl = seriesInfo.ArtworkUrl;
            existingItem.BackdropUrl = seriesInfo.BackdropUrl;
            existingItem.Description = seriesInfo.Description;
            existingItem.LibraryId = seriesInfo.LibraryId;
            existingItem.TVRating = seriesInfo.Rated;
            existingItem.SortTitle = StringUtils.SortTitle(seriesInfo.Title);
            existingItem.Title = seriesInfo.Title;
            existingItem.TMDB_Id = seriesInfo.TMDB_Id;


            //Dup check
            existingItem.ComputeHash();
            var dup = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.Id != existingItem.Id)
                .Where(item => item.LibraryId == existingItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => item.TMDB_Id == existingItem.TMDB_Id)
                .Where(item => item.Hash == existingItem.Hash)
                .AnyAsync();

            if (dup)
                return $"A series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}";


            var tmdbInfo = await GetTMDBInfoAsync(existingItem.TMDB_Id, TMDB_MediaTypes.Series);
            existingItem.SetOtherInfo(seriesInfo.ExtraSearchTerms, null, seriesInfo.Genres, tmdbInfo);

            //Update library/rated for episodes
            List<int> playlistIds = null;
            if (library_changed || rated_changed || artwork_changed)
            {
                var episodes = await DB.MediaEntries
                    .Where(item => item.LinkedToId == existingItem.Id)
                    .ToListAsync();

                if (library_changed || rated_changed)
                    episodes.ForEach(item =>
                    {
                        item.LibraryId = existingItem.LibraryId;
                        item.TVRating = existingItem.TVRating;
                    });

                var episodeIds = episodes.Select(item => item.Id).Distinct().ToList();
                playlistIds = await DB.PlaylistItems
                    .AsNoTracking()
                    .Where(item => item.MediaEntry.LinkedToId == existingItem.Id)
                    .Include(item => item.Playlist)
                    .Select(item => item.PlaylistId)
                    .Distinct()
                    .ToListAsync();
            }


            //Save
            await DB.SaveChangesAsync();


            //Playlists
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Warning! For series, this will also delete all episodes.  For videos, this will delete all linked subtitles. It will also delete all subscriptions, overrides, and watch progess, and remove the media from any watchlists and playlists</remarks>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> Delete(int id) => DeleteMedia(id);


        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> ListSubscriptions()
        {
            var series = await DB.WatchableSeriesByProfileQuery(UserProfile)
                .AsNoTracking()
                .Where(m => m.Subscriptions.Any(s => s.ProfileId == UserProfile.Id))
                .ApplySortOrder(SortOrder.Alphabetical)
                .ToListAsync();

            return series.Select(item => item.ToBasicMedia()).ToList();
        }


        /// <summary>
        /// Requires profile
        /// </summary>
        /// <remarks>Subscribe to notificaitons when new episodes are added to a series</remarks>
        [HttpGet("{id}")]
        [ProhibitTestUser]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Subscribe(int id)
        {
            //Get the series
            var series = await DB.WatchableSeriesByProfileQuery(UserProfile)
                .AsNoTracking()
                .Include(m => m.Subscriptions.Where(s => s.ProfileId == UserProfile.Id))
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();

            if (series == null)
                return CommonResponses.ValueNotFound(nameof(id));

            if (series.Subscriptions.FirstOrDefault() == null)
            {
                DB.Subscriptions.Add(new Subscription
                {
                    MediaEntryId = id,
                    ProfileId = UserProfile.Id
                });

                await DB.SaveChangesAsync();
            }

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires profile
        /// </summary>
        /// <remarks>Unsubcribe from notifications when new episodes are added to a series</remarks>
        [HttpDelete("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Unsubscribe(int id)
        {
            if (id < 0)
                return Result.BuildSuccess();

            var rec = await DB.Subscriptions
                .Where(item => item.MediaEntryId == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (rec != null)
            {
                DB.Subscriptions.Remove(rec);
                await DB.SaveChangesAsync();
            }

            return Result.BuildSuccess();
        }


        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> RemoveFromContinueWatching(int id)
        {
            if (id <= 0)
                return Result.BuildSuccess();

            var prog = await DB.ProfileMediaProgresses
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.MediaEntryId == id)
                .Where(item => item.MediaEntry.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (prog != null)
            {
                DB.ProfileMediaProgresses.Remove(prog);
                await DB.SaveChangesAsync();
            }

            return Result.BuildSuccess();
        }

        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> MarkWatched(int id)
        {
            if (id <= 0)
                return Result.BuildSuccess();

            var lastEpisode = await DB.MediaEntries
                .AsNoTracking()

                .Include(m => m.LinkedTo)
                .ThenInclude(m => m.ProfileMediaProgress.Where(p => p.ProfileId == UserProfile.Id))

                .Where(m => m.LinkedTo.EntryType == MediaTypes.Episode)
                .Where(m => m.LinkedTo.Id == id)
                .Where(m =>
                    m.LinkedTo.TitleOverrides
                        .Where(t => t.ProfileId == UserProfile.Id)
                        .Where(t => t.State == OverrideState.Allow)
                        .Any()
                    ||
                    (
                        UserProfile.IsMain
                        &&
                        (
                            m.LinkedTo.Library.AccountId == UserAccount.Id
                            ||
                            (
                                m.LinkedTo.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == UserAccount.Id || f.Friendship.Account2Id == UserAccount.Id)
                                && !m.TitleOverrides
                                    .Where(t => t.ProfileId == UserProfile.Id)
                                    .Where(t => t.State == OverrideState.Block)
                                    .Any()
                            )
                        )
                    )
                    ||
                    (
                        m.LinkedTo.Library.ProfileLibraryShares.Any(p => p.ProfileId == UserProfile.Id)
                        && UserProfile.MaxTVRating >= (m.LinkedTo.TVRating ?? TVRatings.NotRated)
                        && !m.LinkedTo.TitleOverrides
                            .Where(t => t.ProfileId == UserProfile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                )

                .OrderByDescending(m => m.Xid)
                .FirstOrDefaultAsync();


            if (lastEpisode == null)
                return Result.BuildSuccess();

            DB.MediaEntries.Update(lastEpisode.LinkedTo);
            lastEpisode.LinkedTo.EverPlayed = true;

            var progress = lastEpisode.LinkedTo.ProfileMediaProgress.FirstOrDefault();

            if (progress == null)
            {
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = id,
                    Played = lastEpisode.Length ?? 0,
                    ProfileId = UserProfile.Id,
                    Timestamp = DateTime.UtcNow,
                    Xid = lastEpisode.Xid
                });
            }
            else
            {
                progress.Xid = lastEpisode.Xid;
                progress.Played = lastEpisode.Length ?? 0;
                progress.Timestamp = DateTime.UtcNow;
            }

            await DB.SaveChangesAsync();

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Designed for admin tools, this will search for any series owned by the account</remarks>
        [HttpPost]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> AdminSearch([FromQuery] int libraryId, [FromBody] SearchRequest request)
        {
            var ret = new List<BasicMedia>();

            request.Query = request.Query.NormalizedQueryString();
            string boolQuery = MediaEntry.BuildSearchQuery(request.Query);
            if (string.IsNullOrWhiteSpace(boolQuery))
                return ret;

            var libQ = DB.Libraries
                .AsNoTracking()
                .Where(lib => lib.AccountId == UserAccount.Id)
                .Where(lib => lib.IsTV);
            if (libraryId > 0)
                libQ = libQ.Where(lib => lib.Id == libraryId);
            var libIds = await libQ.Select(lib => lib.Id).ToListAsync();


            var mediaEntries = await DB.MediaEntries
                .AsNoTracking()

                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => libIds.Contains(item.LibraryId))
                .Where(item => EF.Functions.IsMatch(item.SearchTitle, boolQuery, MySqlMatchSearchMode.Boolean))

                .Distinct()
                .Take(MAX_DB_LIST_SIZE)
                .ToListAsync();

            mediaEntries.Sort((x, y) =>
            {
                int ret = x.SortTitle.CompareTo(y.SortTitle);
                if (ret == 0)
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                return ret;
            });

            ret.AddRange(mediaEntries.Select(me => me.ToBasicMedia()));

            return ret;
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any series owned by the account with the specified tmdb id</remarks>
        [HttpGet]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicMedia>>))]
        public async Task<Result<List<BasicMedia>>> AdminSearchByTmdbId([FromQuery] int libraryId, [FromQuery] int tmdbId)
        {
            var ret = new List<BasicMedia>();

            if (tmdbId <= 0)
                return ret;

            var libQ = DB.Libraries
                .AsNoTracking()
                .Where(lib => lib.AccountId == UserAccount.Id)
                .Where(lib => lib.IsTV);
            if (libraryId > 0)
                libQ = libQ.Where(lib => lib.Id == libraryId);
            var libIds = await libQ.Select(lib => lib.Id).ToListAsync();


            var mediaEntries = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Library)
                .Include(item => item.ExtraSearchTerms)

                .Include(item => item.TMDB_Entry)
                .ThenInclude(item => item.People)
                .ThenInclude(item => item.TMDB_Person)

                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => libIds.Contains(item.LibraryId))
                .Where(item => item.TMDB_Id == tmdbId)

                .Distinct()
                .Take(MAX_DB_LIST_SIZE)
                .ToListAsync();


            mediaEntries.Sort((x, y) =>
            {
                int ret = x.SortTitle.CompareTo(y.SortTitle);
                if (ret == 0)
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                return ret;
            });

            ret.AddRange(mediaEntries.Select(me => me.ToBasicMedia()));

            return ret;
        }
    }
}
