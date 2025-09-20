using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class DBCleaner : IHostedService, IDisposable
    {
        //No reason to run this more than once/day
        private const int MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;

        private readonly Timer _timer;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CancellationToken _cancellationToken;

        public DBCleaner()
        {
            _cancellationToken = _cancellationTokenSource.Token;
            _timer = new Timer(new TimerCallback(DoWork), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(0, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            if (AppDbContext.Ready)
            {
                try { await CleanupAsync(); }
                catch { }
            }

            if (!_cancellationToken.IsCancellationRequested)
                try { _timer.Change(MILLISECONDS_PER_DAY, Timeout.Infinite); }
                catch { }
        }

        private async Task CleanupAsync()
        {
            using var db = new AppDbContext();

            //Clean logs more than 14 days old
            string query = $"DELETE FROM {nameof(db.Logs)} WHERE {nameof(LogEntry.Timestamp)} < '{DateTime.UtcNow.AddDays(-14):yyyy-MM-dd}'";
            await db.Database.ExecuteSqlRawAsync(query);

            //Clean activation codes more than 1 day old
            query = $"DELETE FROM {nameof(db.ActivationCodes)} WHERE {nameof(ActivationCode.Created)} < '{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}'";
            await db.Database.ExecuteSqlRawAsync(query);
        }

    }
}
