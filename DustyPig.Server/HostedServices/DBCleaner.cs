using DustyPig.Server.Data;
using DustyPig.Timers;
using Microsoft.EntityFrameworkCore;
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
                    .OrderBy(_ => _.Created)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                if (codes.Count == 0)
                    break;

                db.ActivationCodes.RemoveRange(codes);
                await db.SaveChangesAsync(cancellationToken);
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
                    .OrderBy(_ => _.LastSeen)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                if (tokens.Count == 0)
                    break;

                db.FCMTokens.RemoveRange(tokens);
                await db.SaveChangesAsync(cancellationToken);
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
