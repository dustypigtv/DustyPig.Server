using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;

namespace DustyPig.Server.HostedServices
{
    public class ArtworkUpdater : IHostedService, IDisposable
    {
        //Poster should be 266x400
        //ImageMagick makes each quadrant the specified size, so set to 1/2
        const int POSTER_WIDTH = 133;
        const int POSTER_HEIGHT = 200;

        //15 Seconds
        const int MILLISECONDS_DELAY = 1000 * 15;


        readonly ILogger<ArtworkUpdater> _logger;
        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;

        public ArtworkUpdater(ILogger<ArtworkUpdater> logger)
        {
            _logger = logger;
            _timer = new Timer(new TimerCallback(DoWork), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_DELAY, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return Task.CompletedTask;
        }




        private async void DoWork(object state)
        {
            try
            {
                await ProcessNextAsync();
                await DeleteNextAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_DELAY, Timeout.Infinite);
        }


        private async Task ProcessNextAsync()
        {
            try
            {
                using var db = new AppDbContext();
              
                var playlist = await db.Playlists
                    .Include(item => item.Profile)
                    .ThenInclude(item => item.Account)
                    .Include(item => item.PlaylistItems)
                    .ThenInclude(item => item.MediaEntry)
                    .ThenInclude(item => item.LinkedTo)
                    .Where(item => item.ArtworkUpdateNeeded)
                    .FirstOrDefaultAsync(_cancellationToken);

                if (playlist == null)
                    return;

                var mediaIds = playlist.PlaylistItems.Select(item => item.MediaEntryId).ToList();
                var playable = await db.MediaEntriesPlayableByProfile(playlist.Profile)
                    .Where(item => mediaIds.Contains(item.Id))
                    .Select(item => item.Id)
                    .ToListAsync(_cancellationToken);


                playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

                var art = new Dictionary<int, string>();
                foreach(var playlistItem in playlist.PlaylistItems.Where(item => playable.Contains(item.MediaEntryId)))
                {
                    if (playlistItem.MediaEntry.EntryType == MediaTypes.Movie)
                    {
                        if (!art.ContainsKey(playlistItem.MediaEntryId))
                        {
                            art.Add(playlistItem.MediaEntryId, playlistItem.MediaEntry.ArtworkUrl);
                            if (art.Count > 3)
                                break;
                        }
                    }
                    else if (playlistItem.MediaEntry.EntryType == MediaTypes.Episode)
                    {
                        if (playlistItem.MediaEntry.LinkedToId.HasValue)
                            if (!art.ContainsKey(playlistItem.MediaEntry.LinkedToId.Value))
                            {
                                art.Add(playlistItem.MediaEntry.LinkedToId.Value, playlistItem.MediaEntry.LinkedTo.ArtworkUrl);
                                if (art.Count > 3)
                                    break;
                            }
                    }
                }

                if (art.Count == 0)
                {
                    playlist.ArtworkUrl = Constants.DEFAULT_PLAYLIST_IMAGE;
                }
                else
                {
                    string artId = $"{playlist.Id}." + string.Join('.', art.Keys);
                    string calcArt = Constants.DEFAULT_PLAYLIST_URL_ROOT + artId + ".jpg";
                    if (playlist.ArtworkUrl != calcArt)
                    {
                        var dataLst = new List<byte[]>();
                        foreach (var key in art.Keys)
                            dataLst.Add(await SimpleDownloader.DownloadDataAsync(art[key], _cancellationToken));

                        /*
                            tl  tr
                            bl  br
                        */
                        int idx_tl = 0;
                        int idx_tr = 0;
                        int idx_bl = 0;
                        int idx_br = 0;

                        if (art.Count > 1)
                        {
                            idx_tr = 1;
                            idx_bl = 1;
                        }

                        if (art.Count > 2)
                            idx_bl = 2;

                        if (art.Count > 3)
                            idx_br = 3;

                        using MemoryStream ms = new();

                        using (Image<Rgba32> img1 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_tl])))
                        using (Image<Rgba32> img2 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_tr])))
                        using (Image<Rgba32> img3 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_bl])))
                        using (Image<Rgba32> img4 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_br])))
                        using (Image<Rgba32> outputImage = new Image<Rgba32>(POSTER_WIDTH * 2, POSTER_HEIGHT * 2))
                        {
                            img1.Mutate(o => o.Resize(POSTER_WIDTH, POSTER_HEIGHT));
                            img2.Mutate(o => o.Resize(POSTER_WIDTH, POSTER_HEIGHT));
                            img3.Mutate(o => o.Resize(POSTER_WIDTH, POSTER_HEIGHT));
                            img4.Mutate(o => o.Resize(POSTER_WIDTH, POSTER_HEIGHT));

                            outputImage.Mutate(o => o
                                .DrawImage(img1, new Point(0, 0), 1f)
                                .DrawImage(img2, new Point(POSTER_WIDTH, 0), 1f)
                                .DrawImage(img3, new Point(0, POSTER_HEIGHT), 1f)
                                .DrawImage(img4, new Point(POSTER_WIDTH, POSTER_HEIGHT), 1f)
                            );

                            outputImage.SaveAsJpeg(ms);
                        }

                        await S3.UploadFileAsync(ms, $"{Constants.DEFAULT_PLAYLIST_PATH}/{artId}.jpg", _cancellationToken);

                        //Do this first
                        if (!string.IsNullOrWhiteSpace(playlist.ArtworkUrl))
                            db.S3ArtFilesToDelete.Add(new Data.Models.S3ArtFileToDelete { Url = playlist.ArtworkUrl });

                        //Then this
                        playlist.ArtworkUrl = calcArt;
                    }

                }

                playlist.ArtworkUpdateNeeded = false;
                await db.SaveChangesAsync(_cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
   
        private async Task DeleteNextAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var entry = await db.S3ArtFilesToDelete.FirstOrDefaultAsync();
                if (entry == null)
                    return;

                if (!string.IsNullOrWhiteSpace(entry.Url))
                {
                    bool delete = false;
                    if (entry.Url.ICStartsWith(Constants.DEFAULT_PLAYLIST_URL_ROOT))
                    {
                        if (!entry.Url.ICEndsWith("/default.png"))
                            if (!entry.Url.ICEndsWith("/default.jpg"))
                                delete = true;
                    }
                    else if (entry.Url.ICStartsWith(Constants.DEFAULT_PROFILE_URL_ROOT))
                    {
                        var lst = Constants.DefaultProfileImages();
                        delete = true;
                        foreach (string defaultImg in lst)
                            if (entry.Url.ICEquals(defaultImg))
                            {
                                delete = false;
                                break;
                            }
                    }

                    if (delete)
                        try
                        {
                            await S3.DeleteFileAsync(new Uri(entry.Url).LocalPath.Trim('/'), _cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                }

                db.S3ArtFilesToDelete.Remove(entry);
                await db.SaveChangesAsync(_cancellationToken);
            }
            catch { }
        }



        /// <summary>
        /// To avoid timing bugs, call this AFTER other changes to the database
        /// </summary>
        public static async Task SetNeedsUpdateAsync(int id)
        {
            using var db = new AppDbContext();
            string query = $"UPDATE {nameof(db.Playlists)} SET {nameof(Data.Models.Playlist.ArtworkUpdateNeeded)} = 1 WHERE {nameof(Data.Models.Playlist.Id)} = {id}";
            await db.Database.ExecuteSqlRawAsync(query);
        }


        /// <summary>
        /// To avoid timing bugs, call this AFTER other changes to the database
        /// </summary>
        public static async Task SetNeedsUpdateAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            using var db = new AppDbContext();

            while (ids.Count > 100)
            {
                string idStr = string.Join(',', ids.Take(100));
                ids = ids.Skip(100).ToList();
                string query = $"UPDATE {nameof(db.Playlists)} SET {nameof(Data.Models.Playlist.ArtworkUpdateNeeded)} = 1 WHERE {nameof(Data.Models.Playlist.Id)} IN ({idStr})";
                await db.Database.ExecuteSqlRawAsync(query);
            }
        }

    }
}
