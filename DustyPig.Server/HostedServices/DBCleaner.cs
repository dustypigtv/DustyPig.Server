using DustyPig.Server.Data;
using DustyPig.Server.Utilities;
using DustyPig.Timers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices;

public class DBCleaner : IHostedService, IDisposable
{
    private readonly SafeTimer _timer;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<DBCleaner> _logger;

    private bool _disposed;

    public DBCleaner(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<DBCleaner> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _timer = new SafeTimer(TimerTick, TimeSpan.FromDays(1));
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
        using var db = _dbContextFactory.CreateDbContext();

        var dt = DateTime.UtcNow.AddDays(-1);

        //Clean activation codes more than 1 day old
        try
        {
            while (true)
            {
                var codes = await db.ActivationCodes
                    .Where(_ => _.Created < dt)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                if (codes.Count == 0)
                    break;

                db.ActivationCodes.RemoveRange(codes);
                await db.SaveChangesAsync(cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clean activation codes");
        }




        //Clean FCM tokens over 3 months old
        try
        {
            while (true)
            {
                var tokens = await db.FCMTokens
                    .Where(_ => _.LastSeen < dt)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                if (tokens.Count == 0)
                    break;

                db.FCMTokens.RemoveRange(tokens);
                await db.SaveChangesAsync(cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clean FCM tokens");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _timer.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~DBCleaner()
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
