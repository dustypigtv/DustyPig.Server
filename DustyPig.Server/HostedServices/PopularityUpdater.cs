using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class PopularityUpdater : IHostedService, IDisposable
    {
        //Movies and Series have popularity set when added, so this only needs
        //to run once/day maximum
        private const int MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;

        private readonly Timer _timer;
        private readonly TMDBClient _client;
        private CancellationToken _cancellationToken = default;
        private readonly ILogger<PopularityUpdater> _logger;

        public PopularityUpdater(ILogger<PopularityUpdater> logger)
        {
            _client = new TMDBClient();
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
            _timer.Change(0, Timeout.Infinite);
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
                await DoUpdate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_PER_DAY, Timeout.Infinite);
        }


        private async Task DoUpdate()
        {
            using var db = new AppDbContext();

            // To keep it lean, limit to 1k at once
            const int CHUNK_SIZE = 1000;

            int start = 0;
            while (true)
            {
                var ids = await db.MediaEntries
                    .AsNoTracking()
                    .Where(item => item.EntryType == MediaTypes.Movie)
                    .Where(item => item.TMDB_Id.HasValue)
                    .Where(item => item.TMDB_Id > 0)
                    .Where(item => item.PopularityUpdated == null || item.PopularityUpdated < DateTime.UtcNow.AddDays(-1))
                    .Select(item => item.TMDB_Id)
                    .Distinct()
                    .OrderBy(item => item)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync(_cancellationToken);

                foreach (var id in ids)
                {
                    var movie = await _client.GetMovieAsync(id.Value, _cancellationToken);
                    double popularity = movie.Success ? movie.Data.Popularity : 0;
                    string ts = DateTime.UtcNow.ToString("yyyy-MM-dd H:mm:ss");
                    string query = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}={popularity}, {nameof(MediaEntry.PopularityUpdated)}='{ts}' WHERE {nameof(MediaEntry.TMDB_Id)}={id} AND {nameof(MediaEntry.EntryType)}={(int)MediaTypes.Movie}";
                    await db.Database.ExecuteSqlRawAsync(query, _cancellationToken);
                    await Task.Delay(1000, _cancellationToken);
                }

                if (ids.Count < CHUNK_SIZE)
                    break;

                start += CHUNK_SIZE;
            }


            start = 0;
            while (true)
            {
                var ids = await db.MediaEntries
                    .AsNoTracking()
                    .Where(item => item.EntryType == MediaTypes.Series)
                    .Where(item => item.TMDB_Id.HasValue)
                    .Where(item => item.TMDB_Id > 0)
                    .Where(item => item.PopularityUpdated == null || item.PopularityUpdated < DateTime.UtcNow.AddDays(-1))
                    .Select(item => item.TMDB_Id)
                    .Distinct()
                    .OrderBy(item => item)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync();

                foreach (var id in ids)
                {
                    var series = await _client.GetSeriesAsync(id.Value, _cancellationToken);
                    double popularity = series.Success ? series.Data.Popularity : 0;
                    string ts = DateTime.UtcNow.ToString("yyyy-MM-dd H:mm:ss");
                    string query = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}={popularity}, {nameof(MediaEntry.PopularityUpdated)}='{ts}' WHERE {nameof(MediaEntry.TMDB_Id)}={id} AND {nameof(MediaEntry.EntryType)}={(int)MediaTypes.Series}";
                    await db.Database.ExecuteSqlRawAsync(query, _cancellationToken);
                    await Task.Delay(1000, _cancellationToken);
                }

                if (ids.Count < CHUNK_SIZE)
                    break;

                start += CHUNK_SIZE;
            }
        }


    }
}
