﻿using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.HostedServices;
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
            var playableIds = DB.MediaEntriesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Select(item => item.Id);

            var playlistItems = await DB.PlaylistItems
                .Include(item => item.Playlist)

                .Include(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo)

                .Include(item => item.MediaEntry)
                .ThenInclude(item => item.ProfileMediaProgress.Where(itemx => itemx.ProfileId == UserProfile.Id))

                .Where(item => item.Playlist.ProfileId == UserProfile.Id)
                .Where(item => item.PlaylistId == id)

                .Where(item => playableIds.Contains(item.MediaEntryId))

                .ToListAsync();


            if (playlistItems.Count == 0)
            {
                // Get the default playlist
                var playlist = await DB.Playlists
                    .AsNoTracking()
                    .Where(item => item.Id == id)
                    .Where(item => item.ProfileId == UserProfile.Id)
                    .FirstOrDefaultAsync();

                if (playlist == null)
                    return CommonResponses.NotFound<DetailedPlaylist>();

                return new ResponseWrapper<DetailedPlaylist>(new DetailedPlaylist
                {
                    Id = id,
                    ArtworkUrl = Constants.DEFAULT_PLAYLIST_IMAGE,
                    Name = playlist.Name
                });
            }


            if (SortPlaylist(playlistItems))
                await DB.SaveChangesAsync();


            var ret = new DetailedPlaylist
            {
                Id = playlistItems[0].Playlist.Id,
                Name = playlistItems[0].Playlist.Name,
                CurrentIndex = playlistItems[0].Playlist.CurrentIndex,
                ArtworkUrl = playlistItems[0].Playlist.ArtworkUrl,
            };

            foreach (var dbPlaylistItem in playlistItems.OrderBy(item => item.Index))
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
                
                var progress = dbPlaylistItem.MediaEntry.ProfileMediaProgress.FirstOrDefault(item2 => item2.ProfileId == UserProfile.Id);
                if (progress != null)
                {
                    var endTime = dbPlaylistItem.MediaEntry.EntryType == MediaTypes.Movie ? dbPlaylistItem.MediaEntry.Length.Value * 0.9 : dbPlaylistItem.MediaEntry.Length.Value - 30;
                    if (dbPlaylistItem.MediaEntry.CreditsStartTime.HasValue)
                        endTime = dbPlaylistItem.MediaEntry.CreditsStartTime.Value;

                    if (progress.Played >= 1 && progress.Played < endTime)
                        pli.Played = progress.Played;
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
            

            var playlist = await DB.Playlists
                .Where(item => item.Id == info.Id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound();
            
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

            return new ResponseWrapper();
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
                if(!string.IsNullOrWhiteSpace(playlist.ArtworkUrl))
                    DB.S3ArtFilesToDelete.Add(new Data.Models.S3ArtFileToDelete { Url = playlist.ArtworkUrl });
                DB.Playlists.Remove(playlist);
                await DB.SaveChangesAsync();
            }

            return new ResponseWrapper();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Set the currently playing index</remarks>
        [HttpPost]
        public async Task<ResponseWrapper> SetCurrentIndex(SetPlaylistIndex info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return new ResponseWrapper(ex.ToString()); }

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound();

            if (SortPlaylist(playlist.PlaylistItems))
                playlist.ArtworkUpdateNeeded = true;

            playlist.CurrentIndex = Math.Min(info.CurrentIndex, playlist.PlaylistItems.Max(item => item.Index));

            await DB.SaveChangesAsync();

            return new ResponseWrapper();
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
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound<SimpleValue<int>>("Playlist");

            var mediaEntry = await DB.MediaEntriesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.Id == info.MediaId)
                .Where(item => new MediaTypes[] { MediaTypes.Movie, MediaTypes.Episode }.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return CommonResponses.NotFound<SimpleValue<int>>("Media");

            SortPlaylist(playlist.PlaylistItems);
            var entity = DB.PlaylistItems.Add(new Data.Models.PlaylistItem
            {
                Index = playlist.PlaylistItems.Count,
                MediaEntryId = mediaEntry.Id,
                PlaylistId = info.PlaylistId
            }).Entity;

            playlist.ArtworkUpdateNeeded = true;

            await DB.SaveChangesAsync();         
            
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
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return CommonResponses.NotFound("Playlist");


            var series = await DB.SeriesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.Id == info.MediaId)
                .FirstOrDefaultAsync();

            if (series == null)
                return CommonResponses.NotFound("Series");

            var mediaEntries = await DB.EpisodesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.LinkedToId == info.MediaId)
                .ToListAsync();


            SortPlaylist(playlist.PlaylistItems);
            
            int idx = playlist.PlaylistItems.Count - 1;
            foreach (var episode in mediaEntries.OrderBy(item => item.Xid))
            {
                DB.PlaylistItems.Add(new Data.Models.PlaylistItem
                {
                    Index = ++idx,
                    MediaEntryId = episode.Id,
                    PlaylistId = info.PlaylistId
                });
            }

            playlist.ArtworkUpdateNeeded = true;

            await DB.SaveChangesAsync();

            return new ResponseWrapper();
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ResponseWrapper> DeleteItem(int id)
        {
            var data = await DB.PlaylistItems
                .Where(item => item.Id == id)
                .Where(item => item.Playlist.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (data != null)
            {
                var playlist = await DB.Playlists
                    .Where(item => item.Id == data.PlaylistId)
                    .FirstOrDefaultAsync();

                DB.PlaylistItems.Remove(data);
                playlist.ArtworkUpdateNeeded = true;

                await DB.SaveChangesAsync();
            }

            return new ResponseWrapper();
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

            foreach (var item in playlist.PlaylistItems)
                if (item.Index >= info.Index)
                    item.Index++;

            playlist.PlaylistItems.First(item => item.Id == info.MediaId).Index = info.Index;

            SortPlaylist(playlist.PlaylistItems);

            playlist.ArtworkUpdateNeeded = true;

            await DB.SaveChangesAsync();

            return new ResponseWrapper();
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
            
            while(playlist.PlaylistItems.Count > data.MediaIds.Count)
            {
                playlist.PlaylistItems.RemoveAt(playlist.PlaylistItems.Count - 1);
            }

            for (int i = 0; i < data.MediaIds.Count; i++)
            {
                if (playlist.PlaylistItems.Count > i)
                {
                    playlist.PlaylistItems[i].MediaEntryId = data.MediaIds[i];
                    playlist.PlaylistItems[i].Index = i;
                }
                else
                {
                    playlist.PlaylistItems.Add(new Data.Models.PlaylistItem
                    {
                        MediaEntryId = data.MediaIds[i],
                        Index = i,
                        PlaylistId = playlist.Id
                    });
                }
            }

            playlist.ArtworkUpdateNeeded = true;
            await DB.SaveChangesAsync();

            return new ResponseWrapper();
        }

        private static bool SortPlaylist(List<Data.Models.PlaylistItem> playlistItems)
        {
            bool changed = false;

            playlistItems.Sort((x, y) => x.Index.CompareTo(y.Index));
            for (int i = 0; i < playlistItems.Count; i++)
                if (playlistItems[i].Index != i)
                {
                    playlistItems[i].Index = i;
                    changed = true;
                }
            
            return changed;
        }
        
    }
}









