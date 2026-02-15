using DustyPig.API.v3.Models;
using DustyPig.Server.Services;
using DustyPig.Timers;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices;

/// <summary>
/// This is used to notify profiles via firestore that media they have access to has changed, and they should refresh things like the home screen
/// </summary>
public class MediaChangedTriggerManager : IHostedService, IDisposable
{
    private const int TICK_INTERVAL_MS = 100;

    private static readonly ConcurrentQueue<int> _homescreen = new();
    private static readonly ConcurrentQueue<int> _continueWatching = new();
    private static readonly ConcurrentQueue<int> _watchlist = new();
    private static readonly ConcurrentQueue<int> _playlist = new();

    private readonly FirestoreDb _firestoreDb;
    private readonly IServiceProvider _serviceProvider;
    private readonly SafeTimer _timer;
    private readonly ILogger<MediaChangedTriggerManager> _logger;

    private bool _disposed;

    public MediaChangedTriggerManager(FirestoreDb firestoreDb, IServiceProvider serviceProvider, ILogger<MediaChangedTriggerManager> logger)
    {
        _firestoreDb = firestoreDb;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timer = new(TimerTick, TimeSpan.FromMilliseconds(TICK_INTERVAL_MS));
    }



    public static void QueueHomeScreen(int profileId)
    {
        if (!_homescreen.Contains(profileId))
            _homescreen.Enqueue(profileId);
    }

    public static void QueueHomeScreen(IEnumerable<int> profileIds)
    {
        foreach (int profileId in profileIds.Distinct())
            QueueHomeScreen(profileId);
    }


    public static void QueueContinueWatching(int profileId)
    {
        if (!_continueWatching.Contains(profileId))
            _continueWatching.Enqueue(profileId);
    }

    public static void QueueContinueWatching(IEnumerable<int> profileIds)
    {
        foreach (int profileId in profileIds.Distinct())
            QueueContinueWatching(profileId);
    }

    public static void QueueWatchlist(int profileId)
    {
        if (!_watchlist.Contains(profileId))
            _watchlist.Enqueue(profileId);
    }

    public static void QueuePlaylist(int profileId)
    {
        if (!_playlist.Contains(profileId))
            _playlist.Enqueue(profileId);
    }



    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Enabled = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryForceStop();
        return Task.CompletedTask;
    }

    private async Task TimerTick(CancellationToken cancellationToken)
    {
        await ProcessQueue(_homescreen, Constants.FDB_KEY_HOMESCREEN_COLLECTION, Constants.UPDATE_POLLING_HOMESCREEN_PATH, cancellationToken);
        await ProcessQueue(_watchlist, Constants.FDB_KEY_WATCHLIST, Constants.UPDATE_POLLING_WATCHLIST_PATH, cancellationToken);
        await ProcessQueue(_playlist, Constants.FDB_KEY_PLAYLIST, Constants.UPDATE_POLLING_PLAYLISTS_PATH, cancellationToken);
        await ProcessQueue(_continueWatching, Constants.FDB_KEY_CONTINUE_WATCHING, Constants.UPDATE_POLLING_CONTINUE_WATCHING_PATH, cancellationToken);
    }

    private async Task ProcessQueue(ConcurrentQueue<int> queue, string firestoreKey, string pollingKey, CancellationToken cancellationToken)
    {
        while (queue.TryDequeue(out int profileId))
        {
            await WriteFirestoreDoc(firestoreKey, profileId, cancellationToken);
            await WriteS3Doc(pollingKey, profileId, cancellationToken);
        }
    }

    private async Task WriteFirestoreDoc(string firestoreKey, int profileId, CancellationToken cancellationToken)
    {
        try
        {
            //The clients only wait for a change to the document, and don't care what the changes are
            //A 1-char key and 8 byte timestamp is small, fast and always updates
            DocumentReference docRef = _firestoreDb.Collection(firestoreKey).Document(profileId.ToString());
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "t", DateTime.UtcNow.Ticks }
            };
            await docRef.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(WriteFirestoreDoc));
        }
    }

    private async Task WriteS3Doc(string pollingKey, int profileId, CancellationToken cancellationToken)
    {
        try
        {
            using var s3 = _serviceProvider.GetRequiredService<S3Service>();
            await s3.WritePollingFileAsync(pollingKey + profileId.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(WriteS3Doc));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
