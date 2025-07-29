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
    private static readonly BlockingCollection<int> _profileIds = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private readonly Timer _timer;
    private readonly ILogger<FirestoreMediaChangedTriggerManager> _logger;

    public FirestoreMediaChangedTriggerManager(ILogger<FirestoreMediaChangedTriggerManager> logger)
    {
        _logger = logger;
        _cancellationToken = _cancellationTokenSource.Token;
        _timer = new Timer(new TimerCallback(TimerTickedAsync), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }


    public static void QueueProfileId(int profileId)
    {
        if (!_profileIds.Contains(profileId))
            _profileIds.Add(profileId);
    }

    public static void QueueProfileIds(IEnumerable<int> profileIds)
    {
        foreach(int profileId in profileIds.Distinct())
            QueueProfileId(profileId);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //Use a timer to start it
        _timer.Change(0, Timeout.Infinite);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _profileIds.CompleteAdding();
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    private async void TimerTickedAsync(object state)
    {
        try
        {
            foreach (int profileId in _profileIds.GetConsumingEnumerable(_cancellationToken))
            {
                // Update Firestore to let mobile listeners know to update their home screen / media views
                try
                {
                    DocumentReference docRef = FDB.Service.Collection(Constants.FDB_KEY_HOMESCREEN_COLLECTION).Document(profileId.ToString());

                    //The clients only wait for a change to the document, and don't care what the changes are
                    //A 1-char key and 8 byte timestamp is small, fast and always updates
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "t", DateTime.UtcNow.Ticks }
                    };
                    await docRef.SetAsync(data, cancellationToken: _cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating alerts in Firestore");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoWork");
        }
    }
}
