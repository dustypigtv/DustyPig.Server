using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// These are kinda slow - multiple DB calls.
// But Since playlists can easily become out of 
// sync, I don't see a better way to keep them
// accurate.  Revisit this soon

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(PlaylistsController))]
    public class PlaylistsController : _MediaControllerBase
    {
        public PlaylistsController(AppDbContext db, TMDBClient tmdbClient) : base(db, tmdbClient) { }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        public async Task<ResponseWrapper<List<BasicPlaylist>>> List()
        {
            var ret = new List<BasicPlaylist>();

            var playlists = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Name)
                .ToListAsync();

            foreach (var pl in playlists)
            {
                var bpl = new BasicPlaylist
                {
                    Id = pl.Id,
                    Name = pl.Name,
                    ArtworkUrl = pl.ArtworkUrl
                };

                ret.Add(bpl);
            }

            return new ResponseWrapper<List<BasicPlaylist>>(ret);
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ResponseWrapper<DetailedPlaylist>> Details(int id)
        {
            if (id <= 0)
                return CommonResponses.NotFound<DetailedPlaylist>(nameof(id));

            //I can't figure out a fast/accurate way to do this in 1 call
            var playlist = await DB.Playlists
                .AsNoTracking()
                .Include(item => item.PlaylistItems)
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo)
                .Include(item => item.PlaylistItems)
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.Subtitles)
                .Where(item => item.Id == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound<DetailedPlaylist>("Playlist");


            var allowedTopLevelIds = await DB.MediaEntries
                .AsNoTracking()
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
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
                        && 
                        (
                            (
                                m.EntryType == MediaTypes.Series
                                && UserProfile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                            )
                            ||
                            (
                                m.EntryType == MediaTypes.Movie
                                && UserProfile.MaxMovieRating >= (m.MovieRating ?? MovieRatings.Unrated)
                            )
                        )
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == UserProfile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                )
                .Select(m => m.Id)
                .Distinct()
                .ToListAsync();


            

            playlist.PlaylistItems.RemoveAll(item => item.MediaEntry.EntryType != MediaTypes.Series && !allowedTopLevelIds.Contains(item.Id));
            playlist.PlaylistItems.RemoveAll(item => item.MediaEntry.EntryType == MediaTypes.Series && !allowedTopLevelIds.Contains(item.MediaEntry.LinkedToId.Value));


            var ret = new DetailedPlaylist
            {
                Id = id,
                Name = playlist.Name,
                CurrentIndex = playlist.CurrentIndex,
                ArtworkUrl = playlist.ArtworkUrl
            };

            foreach (var dbPlaylistItem in playlist.PlaylistItems.OrderBy(p => p.Index))
            {
                var pli = new API.v3.Models.PlaylistItem
                {
                    CreditsStartTime = dbPlaylistItem.MediaEntry.CreditsStartTime,
                    Description = dbPlaylistItem.MediaEntry.Description,
                    Id = dbPlaylistItem.Id,
                    Index = dbPlaylistItem.Index,
                    IntroEndTime = dbPlaylistItem.MediaEntry.IntroEndTime,
                    IntroStartTime = dbPlaylistItem.MediaEntry.IntroStartTime,
                    Length = dbPlaylistItem.MediaEntry.Length ?? 0,
                    Played = dbPlaylistItem.Index == ret.CurrentIndex ? playlist.CurrentProgress : 0,
                    MediaId = dbPlaylistItem.MediaEntryId,
                    MediaType = dbPlaylistItem.MediaEntry.EntryType,
                    BifUrl = dbPlaylistItem.MediaEntry.BifUrl,
                    VideoUrl = dbPlaylistItem.MediaEntry.VideoUrl
                };


                switch (dbPlaylistItem.MediaEntry.EntryType)
                {
                    case MediaTypes.Episode:

                        pli.Title = $"{dbPlaylistItem.MediaEntry.LinkedTo.Title} - s{dbPlaylistItem.MediaEntry.Season:00}e{dbPlaylistItem.MediaEntry.Episode:00} - {dbPlaylistItem.MediaEntry.Title}";
                        pli.SeriesId = dbPlaylistItem.MediaEntry.LinkedToId;
                        pli.ArtworkUrl = dbPlaylistItem.MediaEntry.ArtworkUrl;
                        break;

                    case MediaTypes.Movie:

                        pli.Title = dbPlaylistItem.MediaEntry.Title + $" ({dbPlaylistItem.MediaEntry.Date.Value.Year})";
                        pli.ArtworkUrl = StringUtils.Coalesce(dbPlaylistItem.MediaEntry.BackdropUrl, dbPlaylistItem.MediaEntry.ArtworkUrl);
                        break;
                }

                pli.ExternalSubtitles = dbPlaylistItem.MediaEntry.Subtitles.ToExternalSubtitleList();

                ret.Items.Add(pli);
            }

            if (ret.Items.Count > 0)
            {
                var upNext = ret.Items.FirstOrDefault(p => p.Index == ret.CurrentIndex);
                if (upNext == null)
                {
                    ret.CurrentIndex = ret.Items.First().Index;
                    ret.Items.ForEach(p => p.Played = 0);
                }
                else
                {
                    var cst = upNext.CreditsStartTime ?? -1;
                    if (cst < 0)
                    {
                        if (upNext.MediaType == MediaTypes.Episode)
                            cst = upNext.Length - 30;
                        else if (upNext.MediaType == MediaTypes.Movie)
                            cst = upNext.Length * 0.9;
                    }

                    if (upNext.Length > 0 && (upNext.Played ?? 0) >= cst)
                    {
                        upNext.Played = 0;
                        var lastId = -1;
                        foreach (var item in ret.Items)
                        {
                            if (lastId == upNext.Id)
                            {
                                ret.CurrentIndex = item.Index;
                                break;
                            }
                            lastId = item.Id;
                        }
                    }
                }
            }


            return new ResponseWrapper<DetailedPlaylist>(ret);
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper<SimpleValue<int>>> Create(CreatePlaylist info)
        {
            //Validate object
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }

            var playlist = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Name == info.Name)
                .FirstOrDefaultAsync();

            if (playlist != null)
                return new ResponseWrapper<SimpleValue<int>>("You already have a playlist with the specified name");

            playlist = new Data.Models.Playlist
            {
                Name = info.Name,
                ProfileId = UserProfile.Id,
                ArtworkUrl = Constants.DEFAULT_PLAYLIST_IMAGE
            };

            DB.Playlists.Add(playlist);
            await DB.SaveChangesAsync();

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int>(playlist.Id));
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper> Update(UpdatePlaylist info)
        {
            //Validate object
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .Where(item => item.Id == info.Id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound();

            info.Name = info.Name.Trim();
            if (playlist.Name == info.Name)
                return CommonResponses.Ok();

            //Make sure name is unique
            if (playlist.Name != info.Name)
            {
                var exists = await DB.Playlists
                    .AsNoTracking()
                    .Where(item => item.Id != info.Id)
                    .Where(item => item.ProfileId == UserProfile.Id)
                    .Where(item => item.Name == info.Name)
                    .AnyAsync();

                if (exists)
                    return new ResponseWrapper("Another playlist with the specified name already exists");
            }

            playlist.Name = info.Name;
            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ResponseWrapper> Delete(int id)
        {
            var playlist = await DB.Playlists
                .Where(item => item.Id == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist != null)
            {
                if (!string.IsNullOrWhiteSpace(playlist.ArtworkUrl))
                    DB.S3ArtFilesToDelete.Add(new Data.Models.S3ArtFileToDelete { Url = playlist.ArtworkUrl });
                DB.Playlists.Remove(playlist);
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Set the currently playing index</remarks>
        [HttpPost]
        public async Task<ResponseWrapper> SetPlaylistProgress(SetPlaylistProgress info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound();

            if (playlist.CurrentIndex != info.NewIndex || playlist.CurrentProgress != info.NewProgress)
            {
                playlist.CurrentIndex = info.NewIndex;
                playlist.CurrentProgress = info.NewProgress;
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper<SimpleValue<int>>> AddItem(AddPlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper<SimpleValue<int>>(ex.ToString()); }

            var playlist = await DB.Playlists
                 .AsNoTracking()
                 .Include(item => item.PlaylistItems)
                 .Where(item => item.Id == info.PlaylistId)
                 .Where(item => item.ProfileId == UserProfile.Id)
                 .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(info.PlaylistId));

            int maxIndex = playlist.PlaylistItems.Count > 0 ? playlist.PlaylistItems.Max(p => p.Index) : -1;


            var mediaEntry = await DB.MediaEntries
                .AsNoTracking()
                .Where(m => m.Id == info.MediaId)
                .FirstOrDefaultAsync();


            if (mediaEntry == null)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(info.MediaId));

            var topLevelId = mediaEntry.EntryType == MediaTypes.Episode ? mediaEntry.LinkedToId.Value : mediaEntry.Id;

            var playable = await DB.MediaEntries
                .AsNoTracking()
                .Where(m => m.Id == topLevelId)
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
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
                        &&
                        (
                            (
                                m.EntryType == MediaTypes.Series
                                && UserProfile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                            )
                            ||
                            (
                                m.EntryType == MediaTypes.Movie
                                && UserProfile.MaxMovieRating >= (m.MovieRating ?? MovieRatings.Unrated)
                            )
                        )
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == UserProfile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                )
                .AnyAsync();

            if (!playable)
                return CommonResponses.NotFound<SimpleValue<int>>(nameof(info.MediaId));


            var entity = DB.PlaylistItems.Add(new Data.Models.PlaylistItem
            {
                Index = maxIndex + 1,
                MediaEntryId = info.MediaId,
                PlaylistId = info.PlaylistId
            }).Entity;

            await DB.SaveChangesAsync();
            await ArtworkUpdater.SetNeedsUpdateAsync(info.PlaylistId);

            return new ResponseWrapper<SimpleValue<int>>(new SimpleValue<int> { Value = entity.Id });
        }



        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Add all episodes from a series to a playlist</remarks>
        [HttpPost]
        public async Task<ResponseWrapper> AddSeries(AddPlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .AsNoTracking()
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");

            int maxIndex = playlist.PlaylistItems.Count > 0 ? playlist.PlaylistItems.Max(p => p.Index) : -1;


            var series = await DB.MediaEntries
                .AsNoTracking()

                .Where(m => m.Id == info.MediaId)
                .Where(m => m.EntryType == MediaTypes.Series)

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
                        && UserProfile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == UserProfile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                )
                .FirstOrDefaultAsync();

            if (series == null)
                return CommonResponses.NotFound("Series");


            var episodes = await DB.MediaEntries
                .AsNoTracking()
                .Where(m => m.LinkedToId == series.Id)
                .Where(m => m.EntryType == MediaTypes.Episode)
                .OrderBy(m => m.Xid)
                .ToListAsync();

            int idx = maxIndex;
            foreach (var episode in episodes)
                DB.PlaylistItems.Add(new Data.Models.PlaylistItem
                {
                    Index = ++idx,
                    MediaEntryId = episode.Id,
                    PlaylistId = info.PlaylistId
                });

            await DB.SaveChangesAsync();
            await ArtworkUpdater.SetNeedsUpdateAsync(info.PlaylistId);

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ResponseWrapper> DeleteItem(int id)
        {
            var item = await DB.PlaylistItems
                .Include(p => p.PlaylistId)
                .Where(p => p.Id == id)
                .Where(p => p.Playlist.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (item != null)
            {
                DB.PlaylistItems.Remove(item);
                item.Playlist.ArtworkUpdateNeeded = true;
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper> MoveItemToNewIndex(MovePlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.PlaylistItems.Select(p => p.Id).Contains(info.Id))
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");

            var pli = playlist.PlaylistItems.FirstOrDefault(item => item.Id == info.Id);
            if (pli == null)
                return CommonResponses.NotFound(nameof(info.Id));

            if (pli.Index == info.NewIndex)
                return CommonResponses.Ok();



            /*
                Just can't see this in my head. So... notes

                If 3 wants to move to 5 (oldIndex < newIndex), then moving down the list
                    4 & 5 (> oldIndex, <= newIndex) move to 3 & 4, then set orig 3 to 5

                if 5 wants to move to 3 (oldIndex > newIndex), then moving up the list
                    3 & 4 (< oldIndex, >= newIndex) move to 4 & 5, then set orig 5 to 3
              
            */

            int oldIndex = pli.Index;
            int newIndex = info.NewIndex;
            foreach (var p in playlist.PlaylistItems)
                if (oldIndex < newIndex && p.Index > oldIndex && p.Index <= newIndex)
                    p.Index--;
                else if (oldIndex > newIndex && p.Index < oldIndex && p.Index >= newIndex)
                    p.Index++;

            //Set the orig to the new index
            pli.Index = newIndex;
            playlist.ArtworkUpdateNeeded = true;

            await DB.SaveChangesAsync();

            return CommonResponses.Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper> UpdatePlaylistItems(UpdatePlaylistItemsData data)
        {
            //Validate
            try { data.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == data.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");

            //First remove
            bool changed = false;
            var toDel = new List<int>();
            for (int i = 0; i < playlist.PlaylistItems.Count; i++)
            {
                var pli = playlist.PlaylistItems[i];
                if (!data.MediaIds.Contains(pli.MediaEntryId))
                {
                    DB.PlaylistItems.Remove(pli);
                    toDel.Add(pli.Id);
                    changed = true;
                }
            }
            toDel.ForEach(td => playlist.PlaylistItems.RemoveAll(pli => pli.Id == td));


            //Now add/update
            //Don't worry about filtering on playable here, that happens when reading the playlist
            for (int i = 0; i < data.MediaIds.Count; i++)
            {
                var pli = playlist.PlaylistItems.FirstOrDefault(item => item.MediaEntryId == data.MediaIds[i]);
                if (pli == null)
                {
                    DB.PlaylistItems.Add(new Data.Models.PlaylistItem
                    {
                        PlaylistId = playlist.Id,
                        MediaEntryId = data.MediaIds[i],
                        Index = i
                    });
                    changed = true;
                }
                else
                {
                    if (pli.Index != i)
                    {
                        pli.Index = i;
                        DB.PlaylistItems.Update(pli);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                DB.Playlists.Update(playlist);
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }
    }
}
