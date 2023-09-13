﻿using DustyPig.API.v3;
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
using SixLabors.ImageSharp;
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
        public SeriesController(AppDbContext db, TMDBClient tmdbClient) : base(db, tmdbClient)
        {
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Returns the next 100 series based on start position and sort order</remarks>
        [HttpPost]
        public async Task<ResponseWrapper<List<BasicMedia>>> List(ListRequest request)
        {
            //Validate
            try { request.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<List<BasicMedia>>(ex.ToString()); }

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
                     me.EntryType == MediaTypes.Series
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
                             && UserProfile.MaxTVRating >= (me.TVRating ?? TVRatings.NotRated)
                             && ovrride.State != OverrideState.Block
                         )
                     )

                 select me;

            var series = await q
                .AsNoTracking()
                .ApplySortOrder(SortOrder.Alphabetical)
                .Skip(request.Start)
                .Take(DEFAULT_LIST_SIZE)
                .ToListAsync();

            return new ResponseWrapper<List<BasicMedia>>(series.Select(item => item.ToBasicMedia()).ToList());
        }


        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Returns the next 100 series based on start position and sort order. Designed for admin tools, will return all series owned by the account</remarks>
        [HttpGet("{start}/{libId}")]
        [RequireMainProfile]
        public async Task<ResponseWrapper<List<BasicMedia>>> AdminList(int start, int libId)
        {
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

            return new ResponseWrapper<List<BasicMedia>>(series.Select(item => item.ToBasicMedia()).ToList());
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper<DetailedSeries>> Details(int id)
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

                .Where(item => item.Id == id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (media == null)
                return CommonResponses.NotFound<DetailedSeries>();

            if (media.Library.AccountId != UserAccount.Id)
                if (!media.Library.FriendLibraryShares.Any())
                    if (!media.TitleOverrides.Any())
                        if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                            return CommonResponses.NotFound<DetailedSeries>();


            bool playable = (UserProfile.IsMain && media.Library.AccountId == UserAccount.Id)
                || media.TitleOverrides.Any(item => item.State == OverrideState.Allow)
                ||
                (
                    !media.TitleOverrides.Any(item => item.State == OverrideState.Block)
                    &&
                    (
                        UserProfile.IsMain
                        && media.Library.FriendLibraryShares.Any()
                    )
                    &&
                    (
                        media.Library.ProfileLibraryShares.Any()
                        && UserProfile.MaxTVRating >= (media.TVRating ?? TVRatings.NotRated)
                    )
                );



            //Build the response
            var ret = new DetailedSeries
            {
                Id = id,
                ArtworkUrl = media.ArtworkUrl,
                BackdropUrl = media.BackdropUrl,
                CanManage = UserProfile.IsMain && UserAccount.Profiles.Count > 1,
                CanPlay = playable,
                Cast = media.GetPeople(Roles.Cast),
                Description = media.Description,
                Directors = media.GetPeople(Roles.Director),
                Genres = media.ToGenres(),
                LibraryId = media.LibraryId,
                Producers = media.GetPeople(Roles.Producer),
                Rated = media.TVRating.ToRatings(),
                Title = media.Title,
                TMDB_Id = media.TMDB_Id,
                Writers = media.GetPeople(Roles.Writer)
            };


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
                    .First()
                    .GetFriendDisplayNameForAccount(UserAccount.Id);
            }

            var progress = media.ProfileMediaProgress.FirstOrDefault(item => item.ProfileId == UserProfile.Id);

            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();


            foreach (var dbEp in dbEps)
            {
                var ep = new DetailedEpisode
                {
                    ArtworkUrl = dbEp.ArtworkUrl,
                    BifUrl = playable ? dbEp.BifUrl : null,
                    CreditsStartTime = dbEp.CreditsStartTime,
                    Date = dbEp.Date.Value,
                    Description = dbEp.Description,
                    EpisodeNumber = (ushort)dbEp.Episode.Value,
                    ExternalSubtitles = playable ? dbEp.Subtitles.ToExternalSubtitleList() : null,
                    Id = dbEp.Id,
                    IntroEndTime = dbEp.IntroEndTime,
                    IntroStartTime = dbEp.IntroStartTime,
                    Length = dbEp.Length.Value,
                    SeasonNumber = (ushort)dbEp.Season.Value,
                    SeriesId = id,
                    SeriesTitle = media.Title,
                    Title = dbEp.Title,
                    TMDB_Id = dbEp.TMDB_Id,
                    VideoUrl = playable ? dbEp.VideoUrl : null
                };

                ret.Episodes.Add(ep);
            }


            if (playable)
            {
                if (ret.Episodes.Count > 0)
                {
                    if (progress != null)
                    {
                        var dbEp = dbEps.FirstOrDefault(item => item.Xid == progress.Xid);
                        if (dbEp != null)
                        {
                            var upNextEp = ret.Episodes.First(item => item.Id == dbEp.Id);
                            if (progress.Played < (upNextEp.CreditsStartTime ?? dbEp.Length.Value - 30))
                            {
                                //Partially played episode
                                upNextEp.UpNext = true;
                                upNextEp.Played = progress.Played;
                            }
                            else
                            {
                                //Fully played episode, find the next one
                                var nextDBEp = dbEps.FirstOrDefault(item => item.Xid > dbEp.Xid);
                                if (nextDBEp == null)
                                {
                                    //Progress was on last episode
                                    upNextEp.UpNext = true;
                                    upNextEp.Played = progress.Played;
                                }
                                else
                                {
                                    //Next episode after progress
                                    upNextEp = ret.Episodes.First(item => item.Id == nextDBEp.Id);
                                    upNextEp.UpNext = true;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var overrideRequest = media.TitleOverrides.FirstOrDefault(item => item.ProfileId == UserProfile.Id);
                if (overrideRequest != null)
                    ret.AccessRequestedStatus = overrideRequest.Status;
            }

            return new ResponseWrapper<DetailedSeries>(ret);
        }



        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Designed for admin tools, this will return info on any series owned by the account</remarks>
        [HttpGet("{id}")]
        [RequireMainProfile]
        public async Task<ResponseWrapper<DetailedSeries>> AdminDetails(int id)
        {
            //Get the media entry
            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Include(Item => Item.Library)
                .Include(item => item.MediaSearchBridges)
                .ThenInclude(item => item.SearchTerm)
                .Include(item => item.People)
                .ThenInclude(item => item.Person)
                .Where(item => item.Id == id)
                .Where(item => item.Library.AccountId == UserAccount.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .SingleOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound<DetailedSeries>();

            //Build the response
            var ret = new DetailedSeries
            {
                ArtworkUrl = mediaEntry.ArtworkUrl,
                BackdropUrl = mediaEntry.BackdropUrl,
                Cast = mediaEntry.GetPeople(Roles.Cast),
                Description = mediaEntry.Description,
                Directors = mediaEntry.GetPeople(Roles.Director),
                Genres = mediaEntry.ToGenres(),
                Id = id,
                LibraryId = mediaEntry.LibraryId,
                Producers = mediaEntry.GetPeople(Roles.Producer),
                Rated = mediaEntry.TVRating.ToRatings(),
                Title = mediaEntry.Title,
                TMDB_Id = mediaEntry.TMDB_Id,
                Writers = mediaEntry.GetPeople(Roles.Writer)
            };


            //Get the episodes
            var dbEps = await DB.MediaEntries
                .AsNoTracking()
                .Include(item => item.Subtitles)
                .Where(item => item.LinkedToId == id)
                .OrderBy(item => item.Xid)
                .ToListAsync();


            foreach (var dbEp in dbEps)
            {
                var ep = new DetailedEpisode
                {
                    ArtworkUrl = dbEp.ArtworkUrl,
                    BifUrl = dbEp.BifUrl,
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
                    VideoUrl = dbEp.VideoUrl
                };

                ep.ExternalSubtitles = dbEp.Subtitles.ToExternalSubtitleList();

                ret.Episodes.Add(ep);
            }

            //Extra Search Terms
            var allTerms = mediaEntry.MediaSearchBridges.Select(item => item.SearchTerm.Term).ToList();
            var coreTerms = mediaEntry.Title.NormalizedQueryString().Tokenize();
            allTerms.RemoveAll(item => coreTerms.Contains(item));
            ret.ExtraSearchTerms = allTerms;
            ret.CanManage = true;

            return new ResponseWrapper<DetailedSeries>(ret);
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper<SimpleValue<int>>> Create(CreateSeries seriesInfo)
        {
            // ***** Tons of validation *****
            try { seriesInfo.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }


            //Make sure the library is owned
            var ownedLib = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Where(item => item.Id == seriesInfo.LibraryId)
                .AnyAsync();
            if (!ownedLib)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(seriesInfo.LibraryId));


            var newItem = new MediaEntry
            {
                Added = DateTime.UtcNow,
                ArtworkUrl = seriesInfo.ArtworkUrl,
                BackdropUrl = seriesInfo.BackdropUrl,
                Description = seriesInfo.Description,
                EntryType = MediaTypes.Series,
                LibraryId = seriesInfo.LibraryId,
                TVRating = seriesInfo.Rated.ToTVRatings(),
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
                return CommonResponses.BadRequest<SimpleValue<int>>($"An series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}");

            //Get popularity
            await UpdatePopularity(newItem);


            //Add the new item
            DB.MediaEntries.Add(newItem);
            await DB.SaveChangesAsync();

            //People
            await MediaEntryLogic.UpdatePeople(true, newItem, seriesInfo.Cast, seriesInfo.Directors, seriesInfo.Producers, seriesInfo.Writers);

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(true, newItem, GetSearchTerms(newItem, seriesInfo.ExtraSearchTerms));

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int>(newItem.Id));
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [RequireMainProfile]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Update(UpdateSeries seriesInfo)
        {
            // ***** Tons of validation *****
            try { seriesInfo.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }


            var existingItem = await DB.MediaEntries
                .Where(item => item.Id == seriesInfo.Id)
                .Where(item => item.EntryType == MediaTypes.Series)
                .FirstOrDefaultAsync();

            if (existingItem == null)
                return CommonResponses.NotFound("series");

            //Make sure this item is owned
            var ownedLibs = await DB.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == UserAccount.Id)
                .Select(item => item.Id)
                .ToListAsync();

            if (!ownedLibs.Contains(existingItem.LibraryId))
                return CommonResponses.BadRequest("This account does not own this series");

            if (!ownedLibs.Contains(seriesInfo.LibraryId))
                return CommonResponses.NotFound(nameof(seriesInfo.LibraryId));


            //Update info
            bool tmdb_changed = existingItem.TMDB_Id != seriesInfo.TMDB_Id;
            bool library_changed = existingItem.LibraryId != seriesInfo.LibraryId;
            bool rated_changed = existingItem.TVRating != seriesInfo.Rated.ToTVRatings();
            bool artwork_changed = existingItem.ArtworkUrl != seriesInfo.ArtworkUrl;

            existingItem.ArtworkUrl = seriesInfo.ArtworkUrl;
            existingItem.BackdropUrl = seriesInfo.BackdropUrl;
            existingItem.Description = seriesInfo.Description;
            existingItem.SetGenreFlags(seriesInfo.Genres);
            existingItem.LibraryId = seriesInfo.LibraryId;
            existingItem.TVRating = seriesInfo.Rated.ToTVRatings();
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
                return CommonResponses.BadRequest($"A series already exists with the following parameters: {nameof(seriesInfo.LibraryId)}, {nameof(seriesInfo.TMDB_Id)}, {nameof(seriesInfo.Title)}");

            //Get popularity
            if (tmdb_changed)
                await UpdatePopularity(existingItem);

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

            //People
            await MediaEntryLogic.UpdatePeople(false, existingItem, seriesInfo.Cast, seriesInfo.Directors, seriesInfo.Producers, seriesInfo.Writers);

            //Search Terms
            await MediaEntryLogic.UpdateSearchTerms(false, existingItem, GetSearchTerms(existingItem, seriesInfo.ExtraSearchTerms));

            //Playlists
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Warning! For series, this will also delete all episodes.  For videos, this will delete all linked subtitles. It will also delete all subscriptions, overrides, and watch progess, and remove the media from any watchlists and playlists</remarks>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [ProhibitTestUser]
        public Task<ResponseWrapper> Delete(int id) => DeleteMedia(id);


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        public async Task<ResponseWrapper<List<BasicMedia>>> ListSubscriptions()
        {
            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id
                join sub in DB.Subscriptions
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id }
                    equals new { sub.MediaEntryId, sub.ProfileId }

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
                    me.EntryType == MediaTypes.Series
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
                            && UserProfile.MaxTVRating >= (me.TVRating ?? TVRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                    )

                orderby me.SortTitle

                select me;


            var ret = await q
                .AsNoTracking()
                .ToListAsync();

            return new ResponseWrapper<List<BasicMedia>>(ret.Select(item => item.ToBasicMedia()).ToList());
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Subscribe to notificaitons when new episodes are added to a series</remarks>
        [HttpGet("{id}")]
        [ProhibitTestUser]
        public async Task<ResponseWrapper> Subscribe(int id)
        {
            //Get the series
            var q =
                from me in DB.MediaEntries
                join lib in DB.Libraries on me.LibraryId equals lib.Id

                join sub in DB.Subscriptions
                    on new { MediaEntryId = me.Id, ProfileId = UserProfile.Id }
                    equals new { sub.MediaEntryId, sub.ProfileId }
                    into sub_lj
                from sub in sub_lj.DefaultIfEmpty()

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
                    me.Id == id
                    && me.EntryType == MediaTypes.Series
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
                            && UserProfile.MaxTVRating >= (me.TVRating ?? TVRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                    )

                orderby me.SortTitle

                select new
                {
                    me,
                    sub
                };

            var ret = await q
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (ret == null)
                return CommonResponses.NotFound(nameof(id));

            if (ret.sub == null)
            {
                DB.Subscriptions.Add(new Subscription
                {
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
        /// <remarks>Unsubcribe from notifications when new episodes are added to a series</remarks>
        [HttpDelete("{id}")]
        public async Task<ResponseWrapper> Unsubscribe(int id)
        {
            var rec = await DB.Subscriptions
                .Where(item => item.MediaEntryId == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (rec != null)
            {
                DB.Subscriptions.Remove(rec);
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper> RemoveFromContinueWatching(int id)
        {
            if (id <= 0)
                return CommonResponses.NotFound();

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

            return CommonResponses.Ok();
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper> MarkSeriesWatched(int id)
        {
            if (id <= 0)
                return CommonResponses.NotFound();

            var q =
                from series in DB.MediaEntries
                join lib in DB.Libraries on series.LibraryId equals lib.Id

                join maxXid in
                    (
                        from episode in DB.MediaEntries
                        where
                            episode.LinkedToId.HasValue
                            && episode.LinkedToId == id
                            && episode.EntryType == MediaTypes.Episode
                            && episode.Xid.HasValue
                            && episode.Length.HasValue
                        group episode by episode.LinkedToId into g
                        select new
                        {
                            SeriesId = g.Key.Value,
                            MaxXid = g.Max(item => item.Xid.Value),
                            MaxLength = g.Max(item => item.Length.Value)
                        }
                    ) on series.Id equals maxXid.SeriesId


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
                    on new { MediaEntryId = series.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                join pmp in DB.ProfileMediaProgresses
                    on new { MediaEntryId = series.Id, ProfileId = UserProfile.Id }
                    equals new { pmp.MediaEntryId, pmp.ProfileId }
                    into pmp_lj
                from pmp in pmp_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    series.Id == id
                    && series.EntryType == MediaTypes.Series
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
                            && UserProfile.MaxTVRating >= (series.TVRating ?? TVRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                    )

                select new
                {
                    SeriesId = series.Id,
                    maxXid.MaxXid,
                    maxXid.MaxLength,
                    Progress = pmp
                };


            var ret = await q
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (ret == null)
                return CommonResponses.NotFound(nameof(id));

            if (ret.Progress == null)
            {
                DB.ProfileMediaProgresses.Add(new ProfileMediaProgress
                {
                    MediaEntryId = id,
                    Played = ret.MaxLength,
                    ProfileId = UserProfile.Id,
                    Timestamp = DateTime.UtcNow,
                    Xid = ret.MaxXid
                });
            }
            else
            {
                ret.Progress.Xid = ret.MaxXid;
                ret.Progress.Played = ret.MaxLength;
                ret.Progress.Timestamp = DateTime.UtcNow;
            }


            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }

    }
}
