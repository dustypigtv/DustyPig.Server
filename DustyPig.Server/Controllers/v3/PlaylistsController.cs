using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
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
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicPlaylist>>> List()
        {
            var ret = new List<BasicPlaylist>();

            var playableIds = DB.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
               .Select(item => item.Id);


            var playlists = await DB.Playlists
                .AsNoTracking()              
                .Include(item => item.PlaylistItems.Where(item2 => playableIds.Contains(item2.MediaEntryId)))
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo)
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Name)
                .ToListAsync();

            foreach(var pl in playlists)
            {
                var bpl = new BasicPlaylist
                {
                    Id = pl.Id,
                    Name = pl.Name
                };

                SortPlaylist(pl.PlaylistItems);
                GetArtwork(bpl, pl);
                ret.Add(bpl);
            }

            return ret;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedPlaylist>> Details(int id)
        {
            var playableIds = DB.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
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
                    return NotFound();

                return new DetailedPlaylist
                {
                    Id = id,
                    ArtworkUrl1 = Constants.DEFAULT_PLAYLIST_IMAGE,
                    Name = playlist.Name
                };
            }


            if (SortPlaylist(playlistItems))
                await DB.SaveChangesAsync();


            var ret = new DetailedPlaylist
            {
                Id = playlistItems[0].Playlist.Id,
                Name = playlistItems[0].Playlist.Name,
                CurrentIndex = playlistItems[0].Playlist.CurrentIndex
            };


            foreach (var dbPlaylistItem in playlistItems.OrderBy(item => item.Index))
            {
                var pli = new PlaylistItem
                {
                    ArtworkUrl = dbPlaylistItem.MediaEntry.ArtworkUrl,
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

                        if (string.IsNullOrWhiteSpace(ret.ArtworkUrl1))
                            ret.ArtworkUrl1 = dbPlaylistItem.MediaEntry.LinkedTo.ArtworkUrl;
                        else if (string.IsNullOrWhiteSpace(ret.ArtworkUrl2))
                            ret.ArtworkUrl2 = dbPlaylistItem.MediaEntry.LinkedTo.ArtworkUrl;
                        else if (string.IsNullOrWhiteSpace(ret.ArtworkUrl3))
                            ret.ArtworkUrl3 = dbPlaylistItem.MediaEntry.LinkedTo.ArtworkUrl;
                        else if (string.IsNullOrWhiteSpace(ret.ArtworkUrl4))
                            ret.ArtworkUrl4 = dbPlaylistItem.MediaEntry.LinkedTo.ArtworkUrl;
                        
                        break;



                    case MediaTypes.Movie:
                        pli.Title = dbPlaylistItem.MediaEntry.Title + $" ({dbPlaylistItem.MediaEntry.Date.Value.Year})";

                        if (string.IsNullOrWhiteSpace(ret.ArtworkUrl1))
                            ret.ArtworkUrl1 = dbPlaylistItem.MediaEntry.ArtworkUrl;
                        else if (string.IsNullOrWhiteSpace(ret.ArtworkUrl2))
                            ret.ArtworkUrl2 = dbPlaylistItem.MediaEntry.ArtworkUrl;
                        else if (string.IsNullOrWhiteSpace(ret.ArtworkUrl3))
                            ret.ArtworkUrl3 = dbPlaylistItem.MediaEntry.ArtworkUrl;
                        else if (string.IsNullOrWhiteSpace(ret.ArtworkUrl4))
                            ret.ArtworkUrl4 = dbPlaylistItem.MediaEntry.ArtworkUrl;

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

            if (string.IsNullOrWhiteSpace(ret.ArtworkUrl1))
                ret.ArtworkUrl1 = Constants.DEFAULT_PLAYLIST_IMAGE;

            return ret;
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.Created)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SimpleValue<int>>> Create(CreatePlaylist info)
        {
            //Validate object
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var playlist = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Name == info.Name)
                .FirstOrDefaultAsync();

            if (playlist != null)
                return BadRequest("You already have a playlist with the specified name");

            playlist = new Data.Models.Playlist
            {
                Name = info.Name,
                ProfileId = UserProfile.Id
            };

            DB.Playlists.Add(playlist);
            await DB.SaveChangesAsync();

            return CommonResponses.CreatedObject(new SimpleValue<int>(playlist.Id));
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> Update(UpdatePlaylist info)
        {
            //Validate object
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }
            

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.Id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound();

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
                    return BadRequest("Another playlist with the specified name already exists");
            }


            playlist.Name = info.Name;

            SortPlaylist(playlist.PlaylistItems);

            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> Delete(int id)
        {
            var playlist = await DB.Playlists
                .Where(item => item.Id == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist != null)
            {
                DB.Playlists.Remove(playlist);
                await DB.SaveChangesAsync();
            }

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Set the currently playing index</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> SetCurrentIndex(SetPlaylistIndex info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound();

            SortPlaylist(playlist.PlaylistItems);

            playlist.CurrentIndex = Math.Min(info.CurrentIndex, playlist.PlaylistItems.Max(item => item.Index));

            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<SimpleValue<int>>> AddItem(AddPlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound("Playlist not found");

            var mediaEntry = await DB.MediaEntriesPlayableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Include(item => item.LinkedTo)
                .Where(item => item.Id == info.MediaId)
                .Where(item => new MediaTypes[] { MediaTypes.Movie, MediaTypes.Episode }.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return NotFound("Media not found");

            SortPlaylist(playlist.PlaylistItems);
            
            var entity = DB.PlaylistItems.Add(new Data.Models.PlaylistItem
            {
                Index = playlist.PlaylistItems.Count,
                MediaEntryId = info.MediaId,
                PlaylistId = info.PlaylistId
            }).Entity;

            await DB.SaveChangesAsync();

            return Ok(new SimpleValue<int> { Value = entity.Id });
        }

        /// <summary>
        /// Level 2
        /// </summary>
        /// <remarks>Add all episodes from a series to a playlist</remarks>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> AddSeries(AddPlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound("Playlist not found");


            var series = await DB.SeriesPlayableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Where(item => item.Id == info.MediaId)
                .FirstOrDefaultAsync();

            if (series == null)
                return NotFound("Series not found");

            var mediaEntries = await DB.EpisodesPlayableByProfile(UserAccount, UserProfile)
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

            await DB.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> DeleteItem(int id)
        {
            var data = await DB.PlaylistItems
                .Include(item => item.Playlist)
                .Where(item => item.Id == id)
                .Where(item => item.Playlist.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (data != null)
            {
                int playlistId = data.PlaylistId;

                DB.PlaylistItems.Remove(data);
                await DB.SaveChangesAsync();                
            }

            return Ok();
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> MoveItemToNewIndex(ManagePlaylistItem info)
        {
            //Validate
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == info.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound("Playlist not found");

            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

            foreach (var item in playlist.PlaylistItems)
                if (item.Index >= info.Index)
                    item.Index++;

            playlist.PlaylistItems.First(item => item.Id == info.MediaId).Index = info.Index;

            SortPlaylist(playlist.PlaylistItems);

            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> UpdatePlaylistItems(UpdatePlaylistItemsData data)
        {
            //Validate
            try { data.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.ProfileId == UserProfile.Id)
                .Where(item => item.Id == data.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound("Playlist not found");

            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));
            
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


            await DB.SaveChangesAsync();

            return Ok();
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

        private void GetArtwork(BasicPlaylist ret, Data.Models.Playlist pl)
        {
            //pl must have:
            /*
                .Include(item => item.PlaylistItems.Where(item2 => playableIds.Contains(item2.MediaEntryId)))
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo) 
            */


            List<string> art = new List<string>();

            for (int i = 0; i < pl.PlaylistItems.Count; i++)
            {
                if (pl.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Movie)
                {
                    if (!art.Contains(pl.PlaylistItems[i].MediaEntry.ArtworkUrl))
                    {
                        art.Add(pl.PlaylistItems[i].MediaEntry.ArtworkUrl);
                        if (art.Count > 3)
                            break;
                    }
                }
                else if (pl.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Episode)
                {
                    if (!art.Contains(pl.PlaylistItems[i].MediaEntry.LinkedTo.ArtworkUrl))
                    {
                        art.Add(pl.PlaylistItems[i].MediaEntry.LinkedTo.ArtworkUrl);
                        if (art.Count > 3)
                            break;
                    }
                }
            }

            if (art.Count == 0)
                art.Add(Constants.DEFAULT_PLAYLIST_IMAGE);

            /*
                Clint Grid:
                    1   2
                    3   4
             */
            ret.ArtworkUrl1 = art[0];

            if (art.Count == 2)
            {
                ret.ArtworkUrl2 = art[1];
                ret.ArtworkUrl3 = art[1];
                ret.ArtworkUrl4 = art[0];
            }

            if (art.Count == 3)
            {
                ret.ArtworkUrl2 = art[1];
                ret.ArtworkUrl3 = art[2];
                ret.ArtworkUrl4 = art[0];
            }

            if (art.Count == 4)
            {
                ret.ArtworkUrl2 = art[1];
                ret.ArtworkUrl3 = art[2];
                ret.ArtworkUrl4 = art[3];
            }
        }
    }
}









