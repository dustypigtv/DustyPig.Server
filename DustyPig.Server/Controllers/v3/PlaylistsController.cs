using DustyPig.API.v3;
using DustyPig.API.v3.Models;
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

            foreach(var pl in playlists)
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

            try { await DB.UpdatePlaylist(UserAccount, UserProfile, id); }
            catch { }

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
                return CommonResponses.NotFound<DetailedPlaylist>();

            var ret = new DetailedPlaylist
            {
                Id = id,
                Name = playlist.Name,
                CurrentIndex = playlist.CurrentIndex,
                ArtworkUrl = playlist.ArtworkUrl
            };

            foreach (var dbPlaylistItem in playlist.PlaylistItems)
            {
                var pli = new API.v3.Models.PlaylistItem
                {
                    Description = dbPlaylistItem.MediaEntry.Description,
                    Id = dbPlaylistItem.Id,
                    Index = dbPlaylistItem.Index,
                    Length = dbPlaylistItem.MediaEntry.Length ?? 0,
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

            try { await DB.UpdatePlaylist(UserAccount, UserProfile, info.Id, info.Name); }
            catch (ModelValidationException ex) { return CommonResponses.BadRequest(ex.Errors[0]); }
            catch (Exception ex) { return CommonResponses.BadRequest(ex.Message); }

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

            //Don't call UpdatePlaylist here: the logic get's really insane.
            //The info passed in is based on the client's most recent understanding
            //of the data after the last Details call, if UpdatePlaylist changes
            //the data here, the client will be out of sync.  It's ok if this
            //info here is wrong, it self corrects on the next Details call

            var playlist = await DB.Playlists
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound();

            if (playlist.CurrentIndex != info.NewIndex && playlist.CurrentProgress != info.NewProgress)
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

            try { await DB.UpdatePlaylist(UserAccount, UserProfile, info.PlaylistId); }
            catch { }

            var qMax =
                from pli in DB.PlaylistItems
                where pli.PlaylistId == info.PlaylistId
                group pli by pli.PlaylistId into g
                select new
                {
                    Id = g.Key,
                    MaxIndex = g.Max(item => item.Index)
                };

            var playlist = await qMax
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound<SimpleValue<int>>("Playlist");

            var qME =
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

                    me.Id == info.MediaId

                    //Allow to play filters
                    && Constants.PLAYABLE_MEDIA_TYPES.Contains(me.EntryType)
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
                            && ovrride.State != OverrideState.Block
                            && 
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && UserProfile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.NotRated)
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Episode
                                    && UserProfile.MaxTVRating >= (me.TVRating ?? TVRatings.NotRated)
                                )
                            )
                        )
                    )

                select me;

            var mediaEntry = await qME
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound<SimpleValue<int>>("Media");
            
            var entity = DB.PlaylistItems.Add(new Data.Models.PlaylistItem
            {
                Index = playlist.MaxIndex + 1,
                MediaEntryId = mediaEntry.Id,
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

            try { await DB.UpdatePlaylist(UserAccount, UserProfile, info.PlaylistId); }
            catch { }

            var qMax =
                from pli in DB.PlaylistItems
                where pli.PlaylistId == info.PlaylistId
                group pli by pli.PlaylistId into g
                select new
                {
                    Id = g.Key,
                    MaxIndex = g.Max(item => item.Index)
                };

            var playlist = await qMax
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");

            var q =
                from meSeries in DB.MediaEntries
                join meEpisode in DB.MediaEntries on meSeries.Id equals meEpisode.LinkedToId
                join lib in DB.Libraries on meSeries.LibraryId equals lib.Id

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
                    on new { MediaEntryId = meSeries.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    meSeries.Id == info.MediaId
                    && meSeries.EntryType == MediaTypes.Series
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
                            && UserProfile.MaxTVRating >= (meSeries.TVRating ?? TVRatings.NotRated)
                            && ovrride.State != OverrideState.Block
                        )
                    )

                select new
                {
                    meEpisode.Id,
                    meEpisode.Xid
                };

            var response = await q.AsNoTracking().ToListAsync();
            if (response.Count == 0)
                return CommonResponses.NotFound("Series");

            int idx = playlist.MaxIndex;
            foreach (var episode in response.OrderBy(item => item.Xid))
            {
                DB.PlaylistItems.Add(new Data.Models.PlaylistItem
                {
                    Index = ++idx,
                    MediaEntryId = episode.Id,
                    PlaylistId = info.PlaylistId
                });
            }

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
            var q =
                from playlist in DB.Playlists
                join playlistItem in DB.PlaylistItems on playlist.Id equals playlistItem.PlaylistId
                where
                    playlistItem.Id == id
                    && playlist.ProfileId == UserProfile.Id
                select new
                {
                    playlist,
                    playlistItem
                };

            var data = await q.FirstOrDefaultAsync();
            if (data != null)
            {
                DB.PlaylistItems.Remove(data.playlistItem);
                data.playlist.ArtworkUpdateNeeded = true;
                await DB.SaveChangesAsync();
            }

            return CommonResponses.Ok();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ResponseWrapper> MoveItemToNewIndex(ManagePlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == info.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");

            var pli = playlist.PlaylistItems.FirstOrDefault(item => item.Id == info.MediaId);
            if (pli == null)
                return CommonResponses.NotFound(nameof(info.MediaId));

            foreach (var item in playlist.PlaylistItems)
                if (item.Index >= info.Index)
                    item.Index++;
          
            pli.Index = info.Index;
            playlist.ArtworkUpdateNeeded = true;

            await DB.SaveChangesAsync();

            try { await DB.UpdatePlaylist(UserAccount, UserProfile, info.Id); }
            catch { }

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
                .AsNoTracking()
                .Include(item => item.PlaylistItems)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == data.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");

            //First remove
            bool changed = false;
            var toDel = new List<int>();
            for(int i = 0; i <  playlist.PlaylistItems.Count; i++)
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
            for (int i = 0; i < data.MediaIds.Count; i++)
            {
                var pli = playlist.PlaylistItems.FirstOrDefault(item => item.Id == data.MediaIds[i]);
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
                    if(pli.Index != i)
                    {
                        pli.Index = i;
                        DB.PlaylistItems.Update(pli);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                //Not tracking, so use stub
                DB.Playlists.Update(new Data.Models.Playlist
                {
                    Id = playlist.Id,
                    ArtworkUpdateNeeded = true,
                    ArtworkUrl = playlist.ArtworkUrl,
                    CurrentIndex = playlist.CurrentIndex,
                    CurrentProgress = playlist.CurrentProgress,
                    Name = playlist.Name,
                    ProfileId = playlist.ProfileId,
                });

                await DB.SaveChangesAsync();

                try { await DB.UpdatePlaylist(UserAccount, UserProfile, playlist.Id); }
                catch { }
            }

            return CommonResponses.Ok();
        }



        

    }
}
