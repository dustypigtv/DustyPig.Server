using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using DustyPig.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class ArtworkUpdater : IHostedService, IDisposable
    {
        //ImageMagick makes each quadrant the specified size, so set to 1/2
        const int POSTER_WIDTH = Constants.GUIDELINE_MAX_JPG_POSTER_WIDTH / 2;
        const int POSTER_HEIGHT = Constants.GUIDELINE_MAX_JPG_POSTER_HEIGHT / 2;
        const int BACKDROP_WIDTH = Constants.GUIDELINE_MAX_JPG_BACKDROP_WIDTH / 2;
        const int BACKDROP_HEIGHT = Constants.GUIDELINE_MAX_JPG_BACKDROP_HEIGHT / 2;



        //15 Seconds
        const int ONE_SECOND = 1000;


        class ArtDTO
        {
            public int Id { get; set; }
            public int ArtworkId { get; set; }
            public string ArtworkUrl { get; set; }
            public string BackdropUrl { get; set; }
        }


        readonly ILogger<ArtworkUpdater> _logger;
        private readonly Timer _timer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CancellationToken _cancellationToken;


        //Reduce polling the database by only polling on startup, then using a queue
        bool _processFirstRun = true;
        static readonly ConcurrentQueue<int> _processQueue = new();
        
        bool _deleteFirstRun = true;
        static readonly ConcurrentQueue<int> _deleteQueue = new();


        public ArtworkUpdater(ILogger<ArtworkUpdater> logger)
        {
            _logger = logger;
            _cancellationToken = _cancellationTokenSource.Token;
            _timer = new Timer(new TimerCallback(DoWork), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(ONE_SECOND, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }




        private async void DoWork(object state)
        {
            try
            {
                if(_processFirstRun || !_processQueue.IsEmpty)
                    await ProcessNextAsync();

                if(_deleteFirstRun || !_deleteQueue.IsEmpty)
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
                try { _timer.Change(ONE_SECOND, Timeout.Infinite); }
                catch { }
        }


        private async Task ProcessNextAsync()
        {
            int pid = -1;
            if (!_processFirstRun)
                if (!_processQueue.TryDequeue(out pid))
                    return;

            using var db = new AppDbContext();

            var query = db.Playlists
                .Include(item => item.Profile)
                .Include(item => item.PlaylistItems)
                .Where(item => item.ArtworkUpdateNeeded);

            if (!_processFirstRun)
                query = query.Where(p => p.Id == pid);

            var playlist = await query
                .FirstOrDefaultAsync(_cancellationToken);

            if (playlist == null)
            {
                _processFirstRun = false;
                return;
            }



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

                    select new ArtDTO
                    {
                        Id = me.Id,
                        ArtworkId = me.EntryType == MediaTypes.Episode ? series.Id : me.Id,
                        ArtworkUrl = me.EntryType == MediaTypes.Episode ? series.ArtworkUrl : me.ArtworkUrl,
                        BackdropUrl = me.EntryType == MediaTypes.Episode ? series.BackdropUrl : me.BackdropUrl
                    };

                var playable = await q
                    .AsNoTracking()
                    .Distinct()
                    .ToListAsync(_cancellationToken);



                playlist.PlaylistItems.Sort((x, y) => x.Index.CompareTo(y.Index));


                await UpdatePlaylistArtAsync(db, playlist, playable, false);
                await UpdatePlaylistArtAsync(db, playlist, playable, true);



                playlist.ArtworkUpdateNeeded = false;
                await db.SaveChangesAsync(_cancellationToken);

                FirestoreMediaChangedTriggerManager.QueuePlaylist(playlist.ProfileId);
            }
            catch (Exception ex)
            {
                //Otherwise, the next playlist in the database will never get updated
                playlist.ArtworkUpdateNeeded = false;
                await db.SaveChangesAsync(_cancellationToken);

                _logger.LogError(ex, ex.Message);
            }
        }

        private async Task DeleteNextAsync()
        {
            try
            {
                int did = -1;
                if (!_deleteFirstRun)
                    if (!_deleteQueue.TryDequeue(out did))
                        return;

                using var db = new AppDbContext();

                var query = db.S3ArtFilesToDelete.AsQueryable();
                if (!_deleteFirstRun)
                    query = query.Where(d => d.Id == did);

                var entry = await query.FirstOrDefaultAsync(_cancellationToken);
                if (entry == null)
                {
                    _deleteFirstRun = false;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(entry.Url))
                {
                    bool delete = false;
                    if (entry.Url.ICStartsWith(Constants.DEFAULT_PLAYLIST_URL_ROOT))
                    {
                        string[] defaultPlaylistImages =
                        [
                            Constants.DEFAULT_PLAYLIST_BACKDROP,
                            Constants.DEFAULT_PLAYLIST_IMAGE
                        ];
                        delete = true;
                        foreach (string defaultImg in defaultPlaylistImages)
                            if (entry.Url.ICEquals(defaultImg))
                            {
                                delete = false;
                                break;
                            }
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
            _processQueue.Enqueue(id);
        }


        /// <summary>
        /// To avoid timing bugs, call this AFTER other changes to the database
        /// </summary>
        public static async Task SetNeedsUpdateAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            var copy = ids.ToList();

            using var db = new AppDbContext();

            while (copy.Count > 0)
            {
                string idStr = string.Join(',', copy.Take(100));
                string query = $"UPDATE {nameof(db.Playlists)} SET {nameof(Data.Models.Playlist.ArtworkUpdateNeeded)} = 1 WHERE {nameof(Data.Models.Playlist.Id)} IN ({idStr})";
                await db.Database.ExecuteSqlRawAsync(query);

                copy = copy.Skip(100).ToList();
                if (copy.Count > 0)
                    await Task.Delay(ONE_SECOND);
            }

            foreach (var id in ids)
                _processQueue.Enqueue(id);
        }



        public static async Task SetNeedsDeletionAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return;

                using var db = new AppDbContext();
                var entity = db.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = url }).Entity;
                await db.SaveChangesAsync();
                _deleteQueue.Enqueue(entity.Id);
            }
            catch { }
        }

        public static async Task SetNeedsDeletionAsync(IEnumerable<string> urls)
        {
            try
            {
                if (urls == null || !urls.Any(u => !string.IsNullOrWhiteSpace(u)))
                    return;

                using var db = new AppDbContext();
                var entities = new List<S3ArtFileToDelete>();
                foreach (var url in urls.Where(u => !string.IsNullOrWhiteSpace(u)))
                    entities.Add(db.S3ArtFilesToDelete.Add(new S3ArtFileToDelete { Url = url }).Entity);
                await db.SaveChangesAsync();
                entities.ForEach(e => _deleteQueue.Enqueue(e.Id));
            }
            catch { }
        }



        async Task UpdatePlaylistArtAsync(AppDbContext db, Playlist playlist, List<ArtDTO> playable, bool backdrop)
        {
            Dictionary<int, string> art = [];
            foreach (var playlistItem in playlist.PlaylistItems)
            {
                var playableME = playable.FirstOrDefault(item => item.Id == playlistItem.MediaEntryId);
                if (playableME != null)
                    if (!art.ContainsKey(playableME.ArtworkId))
                    {
                        if (backdrop)
                        {
                            if (!string.IsNullOrWhiteSpace(playableME.BackdropUrl))
                                art.Add(playableME.ArtworkId, playableME.BackdropUrl);
                        }
                        else
                        {
                            art.Add(playableME.ArtworkId, playableME.ArtworkUrl);
                        }
                        if (art.Count > 3)
                            break;
                    }
            }

            if (art.Count == 0)
            {
                if (backdrop)
                    playlist.BackdropUrl = Constants.DEFAULT_PLAYLIST_BACKDROP;
                else
                    playlist.ArtworkUrl = Constants.DEFAULT_PLAYLIST_IMAGE;
            }
            else
            {
                string artId = $"{playlist.Id}." + string.Join('.', art.Keys);
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
                    var dataLst = new List<byte[]>();
                    foreach (var key in art.Keys)
                        dataLst.Add(await Program.SharedHttpClient.DownloadDataAsync(art[key], null, _cancellationToken));

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


                    int outputWidth = (backdrop ? BACKDROP_WIDTH : POSTER_WIDTH) * 2;
                    int outputHeight = (backdrop ? BACKDROP_HEIGHT : POSTER_HEIGHT) * 2;

                    using (Image<Rgba32> img1 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_tl])))
                    using (Image<Rgba32> img2 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_tr])))
                    using (Image<Rgba32> img3 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_bl])))
                    using (Image<Rgba32> img4 = Image.Load<Rgba32>(new ReadOnlySpan<byte>(dataLst[idx_br])))
                    using (Image<Rgba32> outputImage = new Image<Rgba32>(outputWidth, outputHeight))
                    {
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

                    string uploadKey = $"{Constants.DEFAULT_PLAYLIST_PATH}/{artId}";
                    if (backdrop)
                        uploadKey += ".backdrop";
                    uploadKey += ".jpg";
                    await S3.UploadFileAsync(ms, uploadKey, _cancellationToken);



                    //Do this first
                    if (backdrop)
                    {
                        if (calcArt != playlist.BackdropUrl)
                            await SetNeedsDeletionAsync(playlist.BackdropUrl);
                    }
                    else
                    {
                        if (calcArt != playlist.ArtworkUrl)
                            await SetNeedsDeletionAsync(playlist.ArtworkUrl);
                    }


                    //Then this
                    if (backdrop)
                        playlist.BackdropUrl = calcArt;
                    else
                        playlist.ArtworkUrl = calcArt;
                }

            }
        }
    }
}
