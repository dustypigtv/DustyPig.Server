using DustyPig.Server.Data;
using DustyPig.Server.Utilities;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DBCleaner> _logger;

    public DBCleaner(IServiceProvider serviceProvider, ILogger<DBCleaner> logger)
    {
        _serviceProvider = serviceProvider;
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
        using var scope = _serviceProvider.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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



    public void Dispose()
    {
        _timer.Dispose();
    }
}
