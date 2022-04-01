using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
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
    [ExceptionLogger(typeof(PlaylistsController))]
    public class PlaylistsController : _MediaControllerBase
    {
        public PlaylistsController(AppDbContext db, TMDBClient tmdbClient, IMemoryCache memoryCache) : base(db, tmdbClient, memoryCache) { }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicPlaylist>>> List()
        {
            var ret = await DB.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == UserProfile.Id)
                .OrderBy(item => item.Name)
                .ToListAsync();

            return ret.Select(item => new BasicPlaylist
            {
                Id = item.Id,
                Name = item.Name,
                ArtworkUrl = item.ArtworkUrl
            }).ToList();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedPlaylist>> Details(int id)
        {
            var playableIds = DB.MediaEntriesPlayableByProfile(UserProfile).Select(item => item.Id);

            var playlistItems = await DB.PlaylistItems
                .Include(item => item.Playlist)

                .Include(item => item.MediaEntry)
                .ThenInclude(item => item.BifServiceCredential)

                .Include(item => item.MediaEntry)
                .ThenInclude(item => item.VideoServiceCredential)

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
                    ArtworkUrl = playlist.ArtworkUrl,
                    Name = playlist.Name
                };
            }


            playlistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

            int idx = 0;
            foreach (var item in playlistItems)
                item.Index = ++idx;
            await DB.SaveChangesAsync();


            var ret = new DetailedPlaylist
            {
                ArtworkUrl = playlistItems[0].Playlist.ArtworkUrl,
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
                    MediaId = dbPlaylistItem.MediaEntryId,
                    MediaType = dbPlaylistItem.MediaEntry.EntryType,
                    BifAsset = Utils.GetAsset(dbPlaylistItem.MediaEntry.BifServiceCredential, _memoryCache, dbPlaylistItem.MediaEntry.BifUrl),
                    VideoAsset = Utils.GetAsset(dbPlaylistItem.MediaEntry.VideoServiceCredential, _memoryCache, dbPlaylistItem.MediaEntry.VideoUrl)
                };

                switch (dbPlaylistItem.MediaEntry.EntryType)
                {
                    case MediaTypes.Episode:
                        pli.Title = $"{dbPlaylistItem.MediaEntry.LinkedTo.Title} - s{dbPlaylistItem.MediaEntry.LinkedTo.Season:00}e{dbPlaylistItem.MediaEntry.LinkedTo.Episode:00} - {dbPlaylistItem.MediaEntry.Title}";
                        pli.SeriesId = dbPlaylistItem.MediaEntry.LinkedToId;
                        break;

                    case MediaTypes.Movie:
                        pli.Title = dbPlaylistItem.MediaEntry.Title + $" ({dbPlaylistItem.MediaEntry.Date.Value.Year})";
                        break;
                }

                var progress = dbPlaylistItem.MediaEntry.ProfileMediaProgress.FirstOrDefault(item2 => item2.ProfileId == UserProfile.Id);
                if (progress != null)
                {
                    var endTime = dbPlaylistItem.MediaEntry.EntryType == MediaTypes.Movie ? dbPlaylistItem.MediaEntry.Length.Value * 0.9 : dbPlaylistItem.MediaEntry.Length.Value - 30;
                    if (dbPlaylistItem.MediaEntry.CreditsStartTime.HasValue)
                        endTime = dbPlaylistItem.MediaEntry.CreditsStartTime.Value;

                    if (progress.Played > 1000 && progress.Played < endTime)
                        pli.Played = progress.Played;
                }


                if (dbPlaylistItem.MediaEntry.Subtitles != null && dbPlaylistItem.MediaEntry.Subtitles.Count > 0)
                {
                    dbPlaylistItem.MediaEntry.Subtitles.Sort();
                    pli.ExternalSubtitles = new List<ExternalSubtitle>();
                    foreach (var sub in dbPlaylistItem.MediaEntry.Subtitles)
                    {
                        var asset = Utils.GetAsset(sub.ServiceCredential, _memoryCache, sub.Url);
                        var xs = new ExternalSubtitle { Name = sub.Name };
                        if (asset != null)
                        {
                            xs.ExpiresUTC = asset.ExpiresUTC;
                            xs.ServiceCredentialId = asset.ServiceCredentialId;
                            xs.Token = asset.Token;
                            xs.Url = asset.Url;
                        }
                        pli.ExternalSubtitles.Add(xs);
                    }
                }

                ret.Items.Add(pli);
            }

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
                ArtworkUrl = info.ArtworkUrl,
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


            playlist.ArtworkUrl = info.ArtworkUrl;
            playlist.Name = info.Name;

            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));
            int idx = 0;
            foreach (var item in playlist.PlaylistItems)
                item.Index = ++idx;

            await DB.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            var playlist = await DB.Playlists
                .Where(item => item.Id == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound();

            DB.Playlists.Remove(playlist);
            await DB.SaveChangesAsync();

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
            if (info.CurrentIndex <= 0)
                return BadRequest("Index must be > 0");

            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound();

            int idx = 0;
            foreach (var item in playlist.PlaylistItems.OrderBy(item => item.Index))
                item.Index = ++idx;

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
            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound("Playlist not found");

            var mediaEntry = await DB.MediaEntriesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.Id == info.MediaId)
                .Where(item => new MediaTypes[] { MediaTypes.Movie, MediaTypes.Episode }.Contains(item.EntryType))
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return NotFound("Media not found");

            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

            int idx = 0;
            foreach (var item in playlist.PlaylistItems)
                item.Index = ++idx;

            var entity = DB.PlaylistItems.Add(new Data.Models.PlaylistItem
            {
                Index = ++idx,
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
            var playlist = await DB.Playlists
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == info.PlaylistId)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null)
                return NotFound("Playlist not found");

            var seriesAllowed = await DB.SeriesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.Id == info.MediaId)
                .AnyAsync();

            if (!seriesAllowed)
                return NotFound("Series not found");

            var mediaEntries = await DB.EpisodesPlayableByProfile(UserProfile)
                .AsNoTracking()
                .Where(item => item.LinkedToId == info.MediaId)
                .ToListAsync();


            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

            int idx = 0;
            foreach (var item in playlist.PlaylistItems)
                item.Index = ++idx;

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

                var playlistItems = await DB.PlaylistItems
                    .Where(item => item.PlaylistId == playlistId)
                    .OrderBy(item => item.Index)
                    .ToListAsync();

                int idx = 0;
                foreach (var item in playlistItems)
                    item.Index = ++idx;

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
            var data = await DB.PlaylistItems
                .Include(item => item.Playlist)
                .ThenInclude(item => item.PlaylistItems)
                .Where(item => item.Id == info.Id)
                .Where(item => item.Playlist.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (data == null)
                return NotFound();

            var playlist = data.Playlist;
            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

            foreach (var item in playlist.PlaylistItems)
                if (item.Index >= info.Index)
                    item.Index++;

            playlist.PlaylistItems.First(item => item.Id == info.Id).Index = info.Index;
            playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

            int idx = 0;
            foreach (var item in playlist.PlaylistItems)
                item.Index = ++idx;

            await DB.SaveChangesAsync();

            return Ok();
        }
    }
}









