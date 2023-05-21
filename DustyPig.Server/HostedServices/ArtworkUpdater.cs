﻿using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Services;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class ArtworkUpdater : BackgroundService
    {
        //Poster should be 266x400
        //ImageMagick makes each quadrant the specified size, so set to 1/2
        const int POSTER_WIDTH = 133;
        const int POSTER_HEIGHT = 200;

        readonly ILogger<ArtworkUpdater> _logger;
        static readonly ConcurrentQueue<string> _toDelete = new();

        public static void DeletePlaylistArt(string key) => _toDelete.Enqueue(key);

        public ArtworkUpdater(ILogger<ArtworkUpdater> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ProcessNext(stoppingToken);
                    await DeleteNext(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task ProcessNext(CancellationToken cancellationToken)
        {
            try
            {
                using var db = new AppDbContext();

                var playlist = await db.Playlists
                    .Include(item => item.PlaylistItems)
                    .ThenInclude(item => item.MediaEntry)
                    .ThenInclude(item => item.LinkedTo)
                    .Where(item => item.ArtworkUpdateNeeded)
                    .FirstOrDefaultAsync(cancellationToken);

                if (playlist == null)
                    return;

                playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

                var art = new Dictionary<int, string>();

                for (int i = 0; i < playlist.PlaylistItems.Count; i++)
                {
                    if (playlist.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Movie)
                    {
                        if (!art.ContainsKey(playlist.PlaylistItems[i].MediaEntryId))
                        {
                            art.Add(playlist.PlaylistItems[i].MediaEntryId, playlist.PlaylistItems[i].MediaEntry.ArtworkUrl);
                            if (art.Count > 3)
                                break;
                        }
                    }
                    else if (playlist.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Episode)
                    {
                        if (playlist.PlaylistItems[i].MediaEntry.LinkedToId.HasValue)
                            if (!art.ContainsKey(playlist.PlaylistItems[i].MediaEntry.LinkedToId.Value))
                            {
                                art.Add(playlist.PlaylistItems[i].MediaEntry.LinkedToId.Value, playlist.PlaylistItems[i].MediaEntry.LinkedTo.ArtworkUrl);
                                if (art.Count > 3)
                                    break;
                            }
                    }
                }

                if (art.Count > 0)
                {

                    if (art.Count == 1)
                    {
                        playlist.ArtworkUrl = art[art.Keys.First()];
                    }
                    else
                    {
                        string artId = $"{playlist.Id}." + string.Join('.', art.Keys);
                        string calcArt = Constants.DEFAULT_PLAYLIST_URL_ROOT + artId + ".jpg";
                        if (playlist.ArtworkUrl != calcArt)
                        {
                            using var images = new MagickImageCollection();

                            var dataLst = new List<byte[]>();
                            foreach (var key in art.Keys)
                                dataLst.Add(await SimpleDownloader.DownloadDataAsync(art[key], cancellationToken));

                            if (art.Count == 2)
                            {
                                images.Add(new MagickImage(dataLst[0]));
                                images.Add(new MagickImage(dataLst[1]));
                                images.Add(new MagickImage(dataLst[1]));
                                images.Add(new MagickImage(dataLst[0]));
                            }

                            if (art.Count == 3)
                            {
                                images.Add(new MagickImage(dataLst[0]));
                                images.Add(new MagickImage(dataLst[1]));
                                images.Add(new MagickImage(dataLst[2]));
                                images.Add(new MagickImage(dataLst[0]));
                            }

                            if (art.Count == 4)
                            {
                                images.Add(new MagickImage(dataLst[0]));
                                images.Add(new MagickImage(dataLst[1]));
                                images.Add(new MagickImage(dataLst[2]));
                                images.Add(new MagickImage(dataLst[3]));
                            }

                            var montageSettings = new MontageSettings()
                            {
                                BackgroundColor = MagickColors.None,
                                Geometry = new MagickGeometry(0, 0, POSTER_WIDTH, POSTER_HEIGHT),
                            };
                            var montage = images.Montage(montageSettings);
                            
                            using var ms = new MemoryStream();
                            await montage.WriteAsync(ms, ImageMagick.MagickFormat.Jpg, cancellationToken);
                            await S3.UploadPlaylistArtAsync(ms, artId, cancellationToken);

                            if (!string.IsNullOrWhiteSpace(playlist.ArtworkUrl))
                                if (playlist.ArtworkUrl.ICStartsWith(Constants.DEFAULT_PLAYLIST_URL_ROOT))
                                    if (!playlist.ArtworkUrl.ICEquals(Constants.DEFAULT_PLAYLIST_IMAGE))
                                    {
                                        string oldKey = new Uri(playlist.ArtworkUrl).LocalPath.Trim('/');
                                        _toDelete.Enqueue(oldKey);
                                    }

                            //Set AFTER deleting old one above
                            playlist.ArtworkUrl = calcArt;
                        }
                    }
                }

                playlist.ArtworkUpdateNeeded = false;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
   
        private async Task DeleteNext(CancellationToken cancellationToken)
        {
            try
            {
                if (_toDelete.TryDequeue(out string key))
                {
                    if (key.ICStartsWith("playlist/"))
                        if (!key.ICEndsWith("default.jpg"))
                            await S3.DeletePlaylistArtAsync(key, cancellationToken);
                }
            }
            catch { }
        }
    }
}