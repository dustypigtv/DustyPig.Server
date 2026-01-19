using Amazon.S3.Model;
using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Extensions;
using DustyPig.Server.Services;
using DustyPig.Server.Utilities;
using DustyPig.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices;

internal class ArtworkUpdater : IHostedService, IDisposable
{
    //ImageMagick makes each quadrant the specified size, so set to 1/2
    const int POSTER_WIDTH = Constants.GUIDELINE_MAX_JPG_POSTER_WIDTH / 2;
    const int POSTER_HEIGHT = Constants.GUIDELINE_MAX_JPG_POSTER_HEIGHT / 2;
    const int BACKDROP_WIDTH = Constants.GUIDELINE_MAX_JPG_BACKDROP_WIDTH / 2;
    const int BACKDROP_HEIGHT = Constants.GUIDELINE_MAX_JPG_BACKDROP_HEIGHT / 2;


    readonly ILogger<ArtworkUpdater> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SafeTimer _updateTimer;
    private readonly SafeTimer _cleanupTimer;
    private bool disposedValue;

    public ArtworkUpdater(IServiceProvider serviceProvider, ILogger<ArtworkUpdater> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _updateTimer = new(UpdateTimerTick);
        _cleanupTimer = new(CleanupTimerTick, TimeSpan.FromDays(1));
    }




    public Task StartAsync(CancellationToken cancellationToken)
    {
        _updateTimer.Enabled = true;
        _cleanupTimer.Enabled = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _updateTimer.TryForceStop();
        _cleanupTimer.TryForceStop();
        return Task.CompletedTask;
    }




    private async Task UpdateTimerTick(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            //Get playlists marked for an update
            var playlist = await db.Playlists
                .Include(item => item.Profile)
                .Include(item => item.PlaylistItems)
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo)
                .Where(item => item.ArtworkUpdateNeeded)
                .FirstOrDefaultAsync(cancellationToken);

            if (playlist == null)
                return;


            var topLevelIds = playlist.PlaylistItems
                .OrderBy(_ => _.Index)
                .Select(_ =>
                {
                    if (_.MediaEntry.EntryType == MediaTypes.Movie)
                        return _.MediaEntry.Id;

                    if (_.MediaEntry.EntryType == MediaTypes.Episode)
                        if (_.MediaEntry.LinkedToId.HasValue)
                            return _.MediaEntry.LinkedToId.Value;

                    return -1;
                })
                .Distinct()
                .ToList();


            List<MediaEntry> topLevelMediaEntries = [];
            if (topLevelIds.Count > 0)
            {
                topLevelMediaEntries = await db
                    .TopLevelWatchableMediaByProfileQuery(playlist.Profile)
                    .AsNoTracking()
                    .Where(_ => topLevelIds.Contains(_.Id))
                    .Take(4)
                    .ToListAsync(cancellationToken);

                //Sort the same order as playlist items
                var tmp = topLevelMediaEntries.ToList();
                topLevelMediaEntries.Clear();
                for (int i = 0; i < Math.Min(4, topLevelIds.Count); i++)
                {
                    var mediaEntry = tmp.FirstOrDefault(_ => _.Id == topLevelIds[i]);
                    if (mediaEntry != null)
                        topLevelMediaEntries.Add(mediaEntry);
                }
            }

            await UpdatePlaylistArtAsync(playlist, topLevelMediaEntries, false, cancellationToken);
            await UpdatePlaylistArtAsync(playlist, topLevelMediaEntries, true, cancellationToken);

            playlist.ArtworkUpdateNeeded = false;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTimerTick));
        }
    }

    private async Task UpdatePlaylistArtAsync(Playlist playlist, List<MediaEntry> mediaEntries, bool backdrop, CancellationToken cancellationToken)
    {
        if (mediaEntries.Count == 0)
        {
            //Defaults
            if (backdrop)
                playlist.BackdropUrl = Constants.DEFAULT_PLAYLIST_BACKDROP;
            else
                playlist.ArtworkUrl = Constants.DEFAULT_PLAYLIST_IMAGE;
            return;
        }


        string artId = $"{playlist.Id}." + string.Join('.', mediaEntries.Select(_ => _.Id.ToString()));
        string calcArt = Constants.DEFAULT_PLAYLIST_URL_ROOT + artId;
        if (backdrop)
            calcArt += ".backdrop";
        calcArt += ".jpg";

        bool shouldBuild =
            backdrop ?
            playlist.BackdropUrl != calcArt :
            playlist.ArtworkUrl != calcArt;

        if (shouldBuild)
        {
            using var scope = _serviceProvider.CreateScope();
            using var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();


            var dataLst = new List<byte[]>();
            foreach (var mediaEntry in mediaEntries)
            {
                string url = backdrop ? mediaEntry.BackdropUrl : mediaEntry.ArtworkUrl;
                if (url.HasValue())
                {
                    try
                    {
                        dataLst.Add(await httpClient.DownloadDataAsync(url, null, cancellationToken));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Download artwork: {url}", url);
                    }
                }
            }




            using MemoryStream ms = new();
            try
            {
                /*
                    tl  tr
                    bl  br
                */
                int idx_tl = 0;
                int idx_tr = 0;
                int idx_bl = 0;
                int idx_br = 0;

                if (dataLst.Count > 1)
                {
                    idx_tr = 1;
                    idx_bl = 1;
                }

                if (dataLst.Count > 2)
                    idx_bl = 2;

                if (dataLst.Count > 3)
                    idx_br = 3;

                int outputWidth = (backdrop ? BACKDROP_WIDTH : POSTER_WIDTH) * 2;
                int outputHeight = (backdrop ? BACKDROP_HEIGHT : POSTER_HEIGHT) * 2;

                using Image<Rgba32> img1 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_tl]));
                using Image<Rgba32> img2 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_tr]));
                using Image<Rgba32> img3 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_bl]));
                using Image<Rgba32> img4 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_br]));
                using Image<Rgba32> outputImage = new(outputWidth, outputHeight);

                int w = backdrop ? BACKDROP_WIDTH : POSTER_WIDTH;
                int h = backdrop ? BACKDROP_HEIGHT : POSTER_HEIGHT;

                img1.Mutate(o => o.Resize(w, h));
                img2.Mutate(o => o.Resize(w, h));
                img3.Mutate(o => o.Resize(w, h));
                img4.Mutate(o => o.Resize(w, h));

                outputImage.Mutate(o => o
                    .DrawImage(img1, new Point(0, 0), 1f)
                    .DrawImage(img2, new Point(w, 0), 1f)
                    .DrawImage(img3, new Point(0, h), 1f)
                    .DrawImage(img4, new Point(w, h), 1f)
                );

                outputImage.SaveAsJpeg(ms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Build tiled image");
                throw;
            }



            try
            {
                string uploadKey = $"{Constants.DEFAULT_PLAYLIST_PATH}/{artId}";
                if (backdrop)
                    uploadKey += ".backdrop";
                uploadKey += ".jpg";

                var s3Service = _serviceProvider.GetRequiredService<S3Service>();
                await s3Service.UploadImageAsync(ms, uploadKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload image");
                throw;
            }


            if (backdrop)
                playlist.BackdropUrl = calcArt;
            else
                playlist.ArtworkUrl = calcArt;
        }

    }




    private async Task CleanupTimerTick(CancellationToken cancellationToken)
    {
        var s3Service = _serviceProvider.GetRequiredService<S3Service>();

        foreach (string key in new string[] { Constants.USER_PLAYLIST_PATH, Constants.USER_PROFILE_PATH })
        {
            List<S3Object> s3Objs;
            try
            {
                s3Objs = await s3Service.ListImagesAsync(key, [".jpg", ".png"], cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(s3Service.ListImagesAsync));
                return;
            }


            //Skip newer images
            s3Objs.RemoveAll(_ => _.LastModified > DateTime.UtcNow.AddDays(-3));
            if (s3Objs.Count == 0)
                return;

            foreach (var s3Obj in s3Objs)
            {
                //Don't crash before scanning all files
                try
                {
                    //Not critical, throttle it down
                    await Task.Delay(1000, cancellationToken);

                    //Convert the key to url
                    string filename = s3Obj.Key.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                    string url = key == Constants.DEFAULT_PLAYLIST_PATH ?
                        Constants.USER_PLAYLIST_URL_ROOT + filename :
                        Constants.USER_PROFILE_URL_ROOT + filename;


                    //Check if the url is in a playlist/PROFILE
                    using var scope = _serviceProvider.CreateScope();
                    using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    bool keep;
                    if (key == Constants.USER_PLAYLIST_PATH)
                    {
                        keep = await db.Playlists
                            .AsNoTracking()
                            .Where(_ => _.ArtworkUrl == url || _.BackdropUrl == url)
                            .AnyAsync(cancellationToken);
                    }
                    else
                    {
                        keep = await db.Profiles
                            .AsNoTracking()
                            .Where(_ => _.AvatarUrl == url)
                            .AnyAsync(cancellationToken);
                    }


                    if (!keep)
                    {
                        //The file is at least a few days old, and id not used by a playlist or profile. 
                        //Delete it
                        await s3Service.DeleteFileAsync(s3Obj.Key, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Check and delete art: {key}", s3Obj.Key);
                }
            }
        }
    }




    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _cleanupTimer.Dispose();
                _updateTimer.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ArtworkUpdater()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}