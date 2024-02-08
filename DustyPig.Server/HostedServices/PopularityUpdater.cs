using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class PopularityUpdater : IHostedService, IDisposable
    {
        private const int MILLISECONDS_PER_HOUR = 1000 * 60 * 60;
        private const int SUCCESS_RUN_AGAIN = MILLISECONDS_PER_HOUR * 24; //Run again in a day
        private const int FAILURE_RUN_AGAIN = MILLISECONDS_PER_HOUR * 4;  //Run again in 4 hours

        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;
        private readonly ILogger<PopularityUpdater> _logger;

        public PopularityUpdater(ILogger<PopularityUpdater> logger)
        {
            _logger = logger;
            _timer = new Timer(new TimerCallback(DoWork), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
            GC.SuppressFinalize(this);
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _timer.Change(10_000, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            bool success = false;
            try
            {
#if !DEBUG
                await DoUpdate();
#endif
                success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }

            if (!_cancellationToken.IsCancellationRequested)
            {
                var nextRun = success ? SUCCESS_RUN_AGAIN : FAILURE_RUN_AGAIN;
                _timer.Change(nextRun, Timeout.Infinite);
            }
        }


        private async Task DoUpdate()
        {
            await UpdateForEntityAsync(MediaTypes.Movie);
            await UpdateForEntityAsync(MediaTypes.Series);
        }



        async Task UpdateForEntityAsync(MediaTypes mediaType)
        {
            using var db = new AppDbContext();

            var popInfoList = await LoadFromServerAsync(mediaType == MediaTypes.Movie);

            var ids = await db.MediaEntries
                .AsNoTracking()
                .Where(item => item.EntryType == mediaType)
                .Where(item => item.TMDB_Id.HasValue)
                .Where(item => item.TMDB_Id > 0)
                .Where(item => item.PopularityUpdated <= DateTime.UtcNow.AddDays(-1))
                .Select(item => item.TMDB_Id.Value)
                .Distinct()
                .ToListAsync(_cancellationToken);

            foreach (var id in ids)
            {
                string ts = DateTime.UtcNow.ToString("yyyy-MM-dd H:mm:ss");
                var popInfo = popInfoList.FirstOrDefault(item => item.Id == id);
                string query = popInfo == null ?
                    $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}=NULL, {nameof(MediaEntry.PopularityUpdated)}='{ts}' WHERE {nameof(MediaEntry.TMDB_Id)}={id} AND {nameof(MediaEntry.EntryType)}={(int)mediaType}" :
                    $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}={popInfo.Popularity}, {nameof(MediaEntry.PopularityUpdated)}='{ts}' WHERE {nameof(MediaEntry.TMDB_Id)}={id} AND {nameof(MediaEntry.EntryType)}={(int)mediaType}";

                await db.Database.ExecuteSqlRawAsync(query, _cancellationToken);
            }
        }




        class PopularityInfo
        {
            public int Id { get; set; }
            public double Popularity { get; set; }
        }

        async Task<List<PopularityInfo>> LoadFromServerAsync(bool movies)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            Exception err = new();

            //Start with the current date and try up to 2 previous days (3 total)
            var startDate = DateTime.Today;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var date = startDate.AddDays(-1 * i);
                    var url = movies ? TMDB.Utils.GetExportedMoviesUrl(date) : TMDB.Utils.GetExportedSeriesUrl(date);
                    var ret = new List<PopularityInfo>();

                    using var client = new HttpClient();
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellationToken);
                    response.EnsureSuccessStatusCode();
                    using var responseStream = await response.Content.ReadAsStreamAsync(_cancellationToken);
                    using var decompressor = new GZipStream(responseStream, CompressionMode.Decompress);
                    using var textReader = new StreamReader(decompressor);

                    //The file isn't valid json but each line is
                    string jsonLine;
                    while ((jsonLine = await textReader.ReadLineAsync(_cancellationToken)) != null)
                    {
                        try { ret.Add(JsonSerializer.Deserialize<PopularityInfo>(jsonLine, options)); }
                        catch { }
                    }

                    if (ret.Count == 0)
                        throw new Exception("No entries in the file: " + (movies ? "movies" : "series"));

                    return ret;
                }
                catch (OperationCanceledException ex)
                {
                    err = ex;
                    break;
                }
                catch (Exception ex)
                {
                    err = ex;
                    await Task.Delay(10_000, _cancellationToken);
                }
            }

            throw err;
        }
    }
}
