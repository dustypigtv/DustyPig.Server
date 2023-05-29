using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class LogCleaner : IHostedService, IDisposable
    {
        //No reason to run this more than once/day
        private const int MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;

        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;

        public LogCleaner()
        {
            _timer = new Timer(new TimerCallback(DoWork), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _timer.Change(0, Timeout.Infinite);
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
                await CleanLogsAsync();
            }
            catch { }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_PER_DAY, Timeout.Infinite);
        }

        private async Task CleanLogsAsync()
        {
            using var db = new AppDbContext();
            string query = $"DELETE FROM {nameof(db.Logs)} WHERE {nameof(LogEntry.Timestamp)} < '{DateTime.UtcNow.AddMonths(-3):yyyy-MM-dd}'";
            await db.Database.ExecuteSqlRawAsync(query);
        }

    }
}
