using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using DustyPig.TMDB.Models;
using DustyPig.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static DustyPig.Server.Services.TMDBClient;

namespace DustyPig.Server.HostedServices
{
    public class TMDB_Updater : IHostedService, IDisposable
    {
        private const int MILLISECONDS_PER_HOUR = 1000 * 60 * 60;

        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;
        private readonly ILogger<TMDB_Updater> _logger;
        private static readonly TMDBClient _client = new()
        {
            RetryCount = 100,
            RetryDelay = 250
        };

        public TMDB_Updater(ILogger<TMDB_Updater> logger)
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
#if !DEBUG
                await DoUpdateAsync();
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_PER_HOUR, Timeout.Infinite);
        }

        private async Task DoUpdateAsync()
        {
            using var db = new AppDbContext();

            //Get tmdb_ids from MediaEntries that have not yet been linked to a TMDB_Entry.
            //These are newly added and should update first
            await Task.Delay(100, _cancellationToken);
            var toDoList1 = await db.MediaEntries
                .AsNoTracking()
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
                .Where(m => m.TMDB_EntryId == null)
                .Where(m => m.TMDB_Id.HasValue)
                .Where(m => m.TMDB_Id > 0)
                .OrderBy(m => m.Added)
                .Select(m => new
                {
                    m.TMDB_Id,
                    m.EntryType
                })
                .Distinct()
                .ToListAsync(_cancellationToken);

            foreach (var item in toDoList1)
            {
                var tmdbType = item.EntryType == MediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series;
                await DoUpdateAsync(item.TMDB_Id.Value, tmdbType);
            }



            //Now get any existing tmdb entries that are due to update
            await Task.Delay(100, _cancellationToken);
            var toDoList2 = await db.TMDB_Entries
                .AsNoTracking()
                .Where(m => m.LastUpdated < DateTime.UtcNow.AddDays(-1))
                .OrderBy(m => m.LastUpdated)
                .Select(m => new
                {
                    m.TMDB_Id,
                    m.MediaType
                })
                .Distinct()
                .ToListAsync(_cancellationToken);

            foreach (var item in toDoList2)
                await DoUpdateAsync(item.TMDB_Id, item.MediaType);
        }


        private async Task DoUpdateAsync(int tmdbId, TMDB_MediaTypes mediaType)
        {
            using var db = new AppDbContext();

            try
            {
                var info = mediaType == TMDB_MediaTypes.Movie ?
                    await AddOrUpdateTMDBMovieAsync(tmdbId, _cancellationToken) :
                    await AddOrUpdateTMDBSeriesAsync(tmdbId, _cancellationToken);


                if (info != null && info.Changed)
                {
                    var conn = db.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync(_cancellationToken);

                    var entryType = (int)(mediaType == TMDB_MediaTypes.Movie ? MediaTypes.Movie : MediaTypes.Series);

                    //Popularity
                    await Task.Delay(100, _cancellationToken);
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3";
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Popularity));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                    await cmd.ExecuteNonQueryAsync(_cancellationToken);



                    //Movie Rating
                    if (entryType == (int)MediaTypes.Movie)
                    {
                        var infoRating = (int?)info.MovieRating;
                        if (infoRating > 0)
                        {
                            await Task.Delay(100, _cancellationToken);
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.MovieRating)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.MovieRating)} IS NULL";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", infoRating.Value));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }
                    }

                    //TV Rating
                    if (entryType == (int)MediaTypes.Series)
                    {
                        var infoRating = (int?)info.TVRating;
                        if (infoRating > 0)
                        {
                            await Task.Delay(100, _cancellationToken);
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TVRating)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.TVRating)} IS NULL";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", infoRating.Value));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }
                    }

                    //Description
                    if (!string.IsNullOrWhiteSpace(info.Overview))
                    {
                        await Task.Delay(100, _cancellationToken);
                        cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Description)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND ({nameof(MediaEntry.Description)}=@p4 OR {nameof(MediaEntry.Description)} IS NULL)";
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Overview));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", ""));
                        await cmd.ExecuteNonQueryAsync(_cancellationToken);
                    }

                    //Backdrop Url
                    if (!string.IsNullOrWhiteSpace(info.BackdropUrl))
                    {
                        await Task.Delay(100, _cancellationToken);
                        cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.BackdropUrl)}=@p1, {nameof(MediaEntry.BackdropSize)}=@p2 WHERE {nameof(MediaEntry.TMDB_Id)}=@p3 AND {nameof(MediaEntry.EntryType)}=@p4 AND ({nameof(MediaEntry.BackdropUrl)}=@p5 OR {nameof(MediaEntry.BackdropUrl)} IS NULL)";
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.BackdropUrl));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", (ulong)info.BackdropSize));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", tmdbId));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", entryType));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p5", ""));
                        await cmd.ExecuteNonQueryAsync(_cancellationToken);
                    }

                    //Link
                    await Task.Delay(100, _cancellationToken);
                    cmd = conn.CreateCommand();
                    cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TMDB_EntryId)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.TMDB_EntryId)} IS NULL";
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Id));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                    await cmd.ExecuteNonQueryAsync(_cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                //This is so failures don't repeatedly try - wait a day!
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync(_cancellationToken);

                await Task.Delay(100, _cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"UPDATE {nameof(db.TMDB_Entries)} SET {nameof(TMDB_Entry.LastUpdated)}=@p1 WHERE {nameof(TMDB_Entry.TMDB_Id)}=@p2 AND {nameof(TMDB_Entry.MediaType)}=@p3";
                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", DateTime.UtcNow));
                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", (int)mediaType));
                await cmd.ExecuteNonQueryAsync(_cancellationToken);
            }
        }


        private static async Task<TMDBInfo> AddOrUpdateTMDBMovieAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(250, cancellationToken);
            var response = await _client.GetMovieAsync(tmdbId, cancellationToken);
            if (!response.Success)
                return null;
            var movie = response.Data;
            if (movie == null)
                return null;

            return await AddOrUpdateTMDBEntryAsync(tmdbId, TMDB_MediaTypes.Movie, TMDBClient.GetCommonCredits(movie), movie.BackdropPath, TMDBClient.TryGetMovieDate(movie), movie.Overview, movie.Popularity, TMDBClient.TryMapMovieRatings(movie), cancellationToken);
        }

        private static async Task<TMDBInfo> AddOrUpdateTMDBSeriesAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(250, cancellationToken);
            var response = await _client.GetSeriesAsync(tmdbId, cancellationToken);
            if (!response.Success)
                return null;
            var series = response.Data;
            if (series == null)
                return null;

            return await AddOrUpdateTMDBEntryAsync(tmdbId, TMDB_MediaTypes.Series, TMDBClient.GetCommonCredits(series), series.BackdropPath, series.FirstAirDate, series.Overview, series.Popularity, TMDBClient.TryMapTVRatings(series), cancellationToken);
        }





        private static async Task<TMDBInfo> AddOrUpdateTMDBEntryAsync(int tmdbId, TMDB_MediaTypes mediaType, CreditsDTO credits, string backdropPath, DateOnly? date, string overview, double popularity, string rated, CancellationToken cancellationToken)
        {
            using var db = new AppDbContext();

            await Task.Delay(100, cancellationToken);
            var entry = await db.TMDB_Entries
                .Where(item => item.TMDB_Id == tmdbId)
                .Where(item => item.MediaType == mediaType)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry != null)
                if (entry.LastUpdated > DateTime.UtcNow.AddDays(-1))
                    return TMDBInfo.FromEntry(entry, false);

            await EnsurePeopleExistAsync(credits, cancellationToken);

            var backdropUrl = TMDBClient.GetPosterPath(backdropPath);

            bool changed = false;
            bool newEntry = false;
            if (entry == null)
            {
                entry = new TMDB_Entry
                {
                    TMDB_Id = tmdbId,
                    MediaType = mediaType
                };
                changed = true;
                newEntry = true;
            }

            if (entry.BackdropUrl != backdropUrl && !string.IsNullOrWhiteSpace(backdropUrl))
            {
                try
                {
                    var size = await SimpleDownloader.GetDownloadSizeAsync(backdropUrl, cancellationToken);
                    entry.BackdropUrl = backdropUrl;
                    entry.BackdropSize = (ulong)size;
                    changed = true;
                }
                catch { }
            }

            //DateTime? movieDate = TMDBClient.ConvertToDateTime(date);
            if (entry.Date != date)
            {
                entry.Date = date;
                changed = true;
            }
            if (entry.Description != overview)
            {
                entry.Description = overview;
                changed = true;
            }
            if (entry.Popularity != popularity)
            {
                entry.Popularity = popularity;
                changed = true;
            }


            //Rated
            if (mediaType == TMDB_MediaTypes.Movie)
            {
                MovieRatings? movieRating = string.IsNullOrWhiteSpace(rated) ? null : rated.ToMovieRatings();
                if (movieRating == MovieRatings.None)
                    movieRating = null;

                if (entry.MovieRating != movieRating && movieRating != null)
                {
                    entry.MovieRating = movieRating;
                    changed = true;
                }
            }
            else
            {
                TVRatings? tvRating = string.IsNullOrWhiteSpace(rated) ? null : rated.ToTVRatings();
                if (tvRating == TVRatings.None)
                    tvRating = null;

                if (entry.TVRating != tvRating && tvRating != null)
                {
                    entry.TVRating = tvRating;
                    changed = true;
                }
            }

            
            //Save changes
            entry.LastUpdated = DateTime.UtcNow;
            if (newEntry)
                db.TMDB_Entries.Add(entry);
            else
                db.TMDB_Entries.Update(entry);
            await Task.Delay(100, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            await BridgeEntryAndPeopleAsync(tmdbId, mediaType, credits, cancellationToken);

            return TMDBInfo.FromEntry(entry, changed);
        }

        private static async Task EnsurePeopleExistAsync(CreditsDTO credits, CancellationToken cancellationToken)
        {
            var alreadyProcessed = new List<int>();

            foreach (var apiPerson in credits.CastMembers.Where(item => !alreadyProcessed.Contains(item.Id)))
            {
                int tmdbId = await AddOrUpdatePersonAsync(apiPerson.Id, apiPerson.Name, apiPerson.FullImagePath, cancellationToken);
                alreadyProcessed.Add(tmdbId);
            }

            foreach (var apiPerson in credits.CrewMembers.Where(item => !alreadyProcessed.Contains(item.Id)))
            {
                if (TMDBClient.CrewJobs.ICContains(apiPerson.Job))
                {
                    int tmdbId = await AddOrUpdatePersonAsync(apiPerson.Id, apiPerson.Name, apiPerson.FullImagePath, cancellationToken);
                    alreadyProcessed.Add(tmdbId);
                }
            }
        }

        private static async Task<int> AddOrUpdatePersonAsync(int tmdbId, string name, string fullImageUrl, CancellationToken cancellationToken)
        {
            using var db = new AppDbContext();

            await Task.Delay(100, cancellationToken);
            var dbPerson = await db.TMDB_People
                .AsNoTracking()
                .Where(item => item.TMDB_Id == tmdbId)
                .FirstOrDefaultAsync(cancellationToken);

            if (dbPerson == null)
            {
                dbPerson = db.TMDB_People.Add(new Data.Models.TMDB_Person
                {
                    TMDB_Id = tmdbId,
                    Name = name,
                    AvatarUrl = fullImageUrl
                }).Entity;
            }
            else
            {
                if (dbPerson.Name != name || dbPerson.AvatarUrl != fullImageUrl)
                {
                    dbPerson.Name = name;
                    dbPerson.AvatarUrl = fullImageUrl;
                    db.TMDB_People.Update(dbPerson);
                }
            }

            await Task.Delay(100, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return dbPerson.TMDB_Id;
        }

        private static async Task BridgeEntryAndPeopleAsync(int tmdbId, TMDB_MediaTypes mediaType, CreditsDTO credits, CancellationToken cancellationToken)
        {
            using var db = new AppDbContext();

            await Task.Delay(100, cancellationToken);
            var entry = await db.TMDB_Entries
                .AsNoTracking()
                .Include(item => item.People)
                .Where(item => item.TMDB_Id == tmdbId)
                .Where(item => item.MediaType == mediaType)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry == null)
                return;

            //Cast
            var bridges = entry.People
                .Where(item => item.Role == CreditRoles.Cast)
                .OrderBy(item => item.SortOrder)
                .ToList();

            var bridgeIds = bridges
                .Select(item => item.TMDB_PersonId)
                .ToList();

            var apiCast = credits.CastMembers
                .OrderBy(item => item.Order)
                .ToList();

            var apiCastIds = apiCast.Select(item => item.Id).ToList();

            if (!bridgeIds.SequenceEqual(apiCastIds))
            {
                bridges.ForEach(item => db.TMDB_EntryPeopleBridges.Remove(item));
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                var alreadySet = new List<int>();
                foreach (var item in credits.CastMembers)
                    if (!alreadySet.Contains(item.Id))
                    {
                        db.TMDB_EntryPeopleBridges.Add(new TMDB_EntryPersonBridge
                        {
                            TMDB_EntryId = entry.Id,
                            TMDB_PersonId = item.Id,
                            Role = CreditRoles.Cast,
                            SortOrder = item.Order
                        });
                        alreadySet.Add(item.Id);
                    }

                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }



            await SetCrew(entry, CreditRoles.Director, credits, "Director", null, cancellationToken);
            await SetCrew(entry, CreditRoles.Producer, credits, "Producer", null, cancellationToken);
            await SetCrew(entry, CreditRoles.ExecutiveProducer, credits, "Executive Producer", null, cancellationToken);
            await SetCrew(entry, CreditRoles.Writer, credits, "Writer", "Screenplay", cancellationToken);

        }

        private static async Task SetCrew(TMDB_Entry entry, CreditRoles role, CreditsDTO credits, string job1, string job2, CancellationToken cancellationToken)
        {
            using var db = new AppDbContext();

            var bridges = entry.People
                .Where(item => item.Role == role)
                .OrderBy(item => item.SortOrder)
                .ToList();

            var bridgeIds = bridges
                .Select(item => item.TMDB_PersonId)
                .ToList();

            var crew = credits.CrewMembers
                .Where(item => item.Job.ICEquals(job1))
                .ToList();

            if (crew.Count == 0 && !string.IsNullOrWhiteSpace(job2))
                crew = credits.CrewMembers
                    .Where(item => item.Job.ICEquals(job2))
                    .ToList();

            //Preserve sort order from tmdb api
            var apiCastIds = new List<int>();
            foreach (var crewMember in crew)
                if (!apiCastIds.Contains(crewMember.Id))
                    apiCastIds.Add(crewMember.Id);


            if (!bridgeIds.SequenceEqual(apiCastIds))
            {
                bridges.ForEach(item => db.TMDB_EntryPeopleBridges.Remove(item));
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                var alreadySet = new List<int>();
                foreach (var crewMember in crew)
                    if (!alreadySet.Contains(crewMember.Id))
                    {
                        db.TMDB_EntryPeopleBridges.Add(new TMDB_EntryPersonBridge
                        {
                            TMDB_EntryId = entry.Id,
                            TMDB_PersonId = crewMember.Id,
                            Role = role,
                            SortOrder = alreadySet.Count
                        });
                        alreadySet.Add(crewMember.Id);
                    }
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            return;

        }
    }
}
