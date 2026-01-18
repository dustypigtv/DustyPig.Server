using DustyPig.API.v3.Models;
using DustyPig.Server.Services;
using Google.Cloud.Firestore;
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
public class FirestoreMediaChangedTriggerManager : IHostedService, IDisposable
{
    private const int TICK_INTERVAL = 100;

    private static readonly ConcurrentQueue<int> _homescreen = new();
    private static readonly ConcurrentQueue<int> _continueWatching = new();
    private static readonly ConcurrentQueue<int> _watchlist = new();
    private static readonly ConcurrentQueue<int> _playlist = new();

    private readonly FirestoreDb _firestoreDb;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private readonly Timer _timer;
    private readonly ILogger<FirestoreMediaChangedTriggerManager> _logger;

    public FirestoreMediaChangedTriggerManager(FirestoreDb firestoreDb, ILogger<FirestoreMediaChangedTriggerManager> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
        _cancellationToken = _cancellationTokenSource.Token;
        _timer = new Timer(new TimerCallback(TimerTickedAsync), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }


    public static void QueueHomeScreen(int profileId)
    {
        if (!_homescreen.Contains(profileId))
            _homescreen.Enqueue(profileId);
    }

    public static void QueueHomeScreen(IEnumerable<int> profileIds)
    {
        foreach(int profileId in profileIds.Distinct())
            QueueHomeScreen(profileId);
    }


    public static void QueueContinueWatching(int profileId)
    {
        if(!_continueWatching.Contains(profileId))
            _continueWatching.Enqueue(profileId);
    }

    public static void QueueContinueWatching(IEnumerable<int> profileIds)
    {
        foreach(int profileId in profileIds.Distinct())
            QueueContinueWatching(profileId);
    }

    public static void QueueWatchlist(int profileId)
    {
        if(!_watchlist.Contains(profileId))
            _watchlist.Enqueue(profileId);
    }

    public static void QueuePlaylist(int profileId)
    {
        if(!_playlist.Contains(profileId))
            _playlist.Enqueue(profileId);
    }



    public Task StartAsync(CancellationToken cancellationToken)
    {
        //Use a timer to start it
        _timer.Change(TICK_INTERVAL, Timeout.Infinite);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        _timer.Dispose();
        return Task.CompletedTask;
    }

    private async void TimerTickedAsync(object state)
    {
        try
        {
            await ProcessQueue(_homescreen, Constants.FDB_KEY_HOMESCREEN_COLLECTION);
            await ProcessQueue(_watchlist, Constants.FDB_KEY_WATCHLIST);
            await ProcessQueue(_playlist, Constants.FDB_KEY_PLAYLIST);
            await ProcessQueue(_continueWatching, Constants.FDB_KEY_CONTINUE_WATCHING);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TimerTicked");
        }

        try { _timer.Change(TICK_INTERVAL, Timeout.Infinite); }
        catch { }
    }

    private async Task ProcessQueue(ConcurrentQueue<int> queue, string key)
    {
        while (queue.TryDequeue(out int profileId))
        {
            await Task.Delay(TICK_INTERVAL, _cancellationToken);
            await WriteDoc(key, profileId);
        }

        await Task.Delay(TICK_INTERVAL, _cancellationToken);
    }

    private Task WriteDoc(string key, int profileId)
    {
        //The clients only wait for a change to the document, and don't care what the changes are
        //A 1-char key and 8 byte timestamp is small, fast and always updates
        DocumentReference docRef = _firestoreDb.Collection(key).Document(profileId.ToString());
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "t", DateTime.UtcNow.Ticks }
        };
        return docRef.SetAsync(data, cancellationToken: _cancellationToken);
    }
}
