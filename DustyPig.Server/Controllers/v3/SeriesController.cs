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
        /// Returns the next 100 series based on start position and sort order. Designed for admin tools, will return all series owned by the account.
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
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
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
            var ret = new DetailedSeries
            {
                ArtworkUrl = mediaEntry.ArtworkUrl,
                ArtworkSize = mediaEntry.ArtworkSize,
                BackdropUrl = mediaEntry.BackdropUrl,
                BackdropSize = mediaEntry.BackdropSize,
                Credits = mediaEntry.GetPeople(),
                Description = mediaEntry.Description,
                Genres = mediaEntry.ToGenres(),
                Id = id,
                LibraryId = mediaEntry.LibraryId,
                Rated = mediaEntry.TVRating ?? TVRatings.None,
                Title = mediaEntry.Title,
                TMDB_Id = mediaEntry.TMDB_Id,
            };


            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();

            if (dbEps.Count > 0)
            {
                ret.Episodes ??= new();
                foreach (var dbEp in dbEps)
                {
                    var ep = new DetailedEpisode
                    {
                        ArtworkUrl = dbEp.ArtworkUrl,
                        ArtworkSize = dbEp.ArtworkSize,
                        BifUrl = dbEp.BifUrl,
                        BifSize = dbEp.BifSize,
                        CreditsStartTime = dbEp.CreditsStartTime,
                        Date = dbEp.Date.Value,
                        Description = dbEp.Description,
                        EpisodeNumber = (ushort)dbEp.Episode.Value,
                        Id = dbEp.Id,
                        IntroEndTime = dbEp.IntroEndTime,
                        IntroStartTime = dbEp.IntroStartTime,
                        Length = dbEp.Length.Value,
                        SeasonNumber = (ushort)dbEp.Season.Value,
                        SeriesId = id,
                        Title = dbEp.Title,
                        TMDB_Id = dbEp.TMDB_Id,
                        VideoUrl = dbEp.VideoUrl,
                        VideoSize = dbEp.VideoSize,
                    };

                    ep.SRTSubtitles = dbEp.Subtitles.ToSRTSubtitleList();

                    ret.Episodes.Add(ep);
                }
            }

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
            newItem.SetGenreFlags(seriesInfo.Genres);
            newItem.Hash = newItem.ComputeHash();


            //Dup check
            var existingItem = await DB.MediaEntries
                .AsNoTracking()
                .Where(item => item.LibraryId == newItem.LibraryId)
                .Where(item => item.EntryType == MediaTypes.Series)
                .Where(item => item.TMDB_Id == newItem.TMDB_Id)
                .Where(item => item.Hash == newItem.Hash)
                .AnyAsync();

            if (existingItem)
                return $"An series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}";

            //Add the new item
            DB.MediaEntries.Add(newItem);
            await DB.SaveChangesAsync();

            //TMDB
            if (newItem.TMDB_Id.HasValue)
            {
                var info = await DB.TMDB_Entries
                    .AsNoTracking()
                    .Where(item => item.TMDB_Id == newItem.TMDB_Id.Value)
                    .Where(item => item.MediaType == TMDB_MediaTypes.Series)
                    .FirstOrDefaultAsync();

                if (info != null)
                {
                    newItem.TMDB_EntryId = info.Id;
                    newItem.Popularity = info.Popularity;

                    //backdrop
                    if (newItem.TVRating == null || newItem.TVRating == TVRatings.None)
                        newItem.TVRating = info.TVRating;

                    if (string.IsNullOrWhiteSpace(newItem.Description))
                        newItem.Description = info.Description;

                    if (string.IsNullOrWhiteSpace(newItem.BackdropUrl))
                    {
                        newItem.BackdropUrl = info.BackdropUrl;
                        newItem.BackdropSize = info.BackdropSize;
                    }

                    DB.MediaEntries.Update(newItem);
                    await DB.SaveChangesAsync();
                }
            }

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(true, newItem, GetSearchTerms(newItem, seriesInfo.ExtraSearchTerms));

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
            existingItem.SetGenreFlags(seriesInfo.Genres);
            existingItem.LibraryId = seriesInfo.LibraryId;
            existingItem.TVRating = seriesInfo.Rated;
            existingItem.SortTitle = StringUtils.SortTitle(seriesInfo.Title);
            existingItem.Title = seriesInfo.Title;
            existingItem.TMDB_Id = seriesInfo.TMDB_Id;
            existingItem.Hash = existingItem.ComputeHash();


            //Dup check
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


            await DB.SaveChangesAsync();

            //TMDB
            if (existingItem.TMDB_Id.HasValue)
            {
                var info = await DB.TMDB_Entries
                    .AsNoTracking()
                    .Where(item => item.TMDB_Id == existingItem.TMDB_Id.Value)
                    .Where(item => item.MediaType == TMDB_MediaTypes.Series)
                    .FirstOrDefaultAsync();

                if (info != null)
                {
                    existingItem.TMDB_EntryId = info.Id;
                    existingItem.Popularity = info.Popularity;

                    //backdrop
                    if (existingItem.TVRating == null || existingItem.TVRating == TVRatings.None)
                        existingItem.TVRating = info.TVRating;

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
            await MediaEntryLogic.UpdateSearchTerms(false, existingItem, GetSearchTerms(existingItem, seriesInfo.ExtraSearchTerms));

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

    }
}
