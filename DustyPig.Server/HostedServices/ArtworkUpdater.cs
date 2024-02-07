using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data;
using DustyPig.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
#if !DEBUG
            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_DELAY, Timeout.Infinite);
#endif
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
            using var db = new AppDbContext();

            var playlist = await db.Playlists
                .Include(item => item.Profile)
                .Include(item => item.PlaylistItems)
                .Where(item => item.ArtworkUpdateNeeded)
                .FirstOrDefaultAsync(_cancellationToken);

            if (playlist == null)
                return;

            try
            {

                var q =
                    from me in db.MediaEntries
                    join lib in db.Libraries on me.LibraryId equals lib.Id
                    join pli in db.PlaylistItems on me.Id equals pli.MediaEntryId

                    join series in db.MediaEntries on me.LinkedToId equals series.Id into series_lj
                    from series in series_lj.DefaultIfEmpty()

                    join fls in db.FriendLibraryShares
                        .Where(t => t.Friendship.Account1Id == playlist.Profile.AccountId || t.Friendship.Account2Id == playlist.Profile.AccountId)
                        .Select(t => (int?)t.LibraryId)
                        on lib.Id equals fls into fls_lj
                    from fls in fls_lj.DefaultIfEmpty()

                    join pls in db.ProfileLibraryShares
                        on new { LibraryId = lib.Id, ProfileId = playlist.Profile.Id }
                        equals new { pls.LibraryId, pls.ProfileId }
                        into pls_lj
                    from pls in pls_lj.DefaultIfEmpty()

                    join ovrride in db.TitleOverrides
                        on new { MediaEntryId = me.EntryType == MediaTypes.Episode ? series.Id : me.Id, ProfileId = playlist.Profile.Id, Valid = true }
                        equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                        into ovrride_lj
                    from ovrride in ovrride_lj.DefaultIfEmpty()

                    where

                        //Allow to play filters
                        Constants.PLAYABLE_MEDIA_TYPES.Contains(me.EntryType)
                        && pli.PlaylistId == playlist.Id
                        &&
                        (
                            ovrride.State == OverrideState.Allow
                            ||
                            (
                                playlist.Profile.IsMain
                                &&
                                (
                                    lib.AccountId == playlist.Profile.AccountId
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
                                        && playlist.Profile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.Unrated)
                                    )
                                    ||
                                    (
                                        me.EntryType == MediaTypes.Episode
                                        && playlist.Profile.MaxTVRating >= (series.TVRating ?? TVRatings.NotRated)
                                    )
                                )
                            )
                        )

                    select new
                    {
                        me.Id,
                        ArtworkId = me.EntryType == MediaTypes.Episode ? series.Id : me.Id,
                        ArtworkUrl = me.EntryType == MediaTypes.Episode ? series.ArtworkUrl : me.ArtworkUrl
                    };

                var playable = await q
                    .AsNoTracking()
                    .Distinct()
                    .ToListAsync(_cancellationToken);



                playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));

                var art = new Dictionary<int, string>();
                foreach (var playlistItem in playlist.PlaylistItems)
                {
                    var playableME = playable.FirstOrDefault(item => item.Id == playlistItem.MediaEntryId);
                    if (playableME != null)
                        if (!art.ContainsKey(playableME.ArtworkId))
                        {
                            art.Add(playableME.ArtworkId, playableME.ArtworkUrl);
                            if (art.Count > 3)
                                break;
                        }
                }

                if (art.Count == 0)
                {
                    playlist.ArtworkUrl = Constants.DEFAULT_PLAYLIST_IMAGE;
                    playlist.ArtworkSize = Constants.DEFAULT_PLAYLIST_IMAGE_SIZE;
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
                        playlist.ArtworkSize = (ulong)ms.Length;
                    }

                }

                playlist.ArtworkUpdateNeeded = false;
                await db.SaveChangesAsync(_cancellationToken);
            }
            catch (Exception ex)
            {
                //Otherwise, the next playlist in the database will never get updated
                playlist.ArtworkUpdateNeeded = false;
                await db.SaveChangesAsync();

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
