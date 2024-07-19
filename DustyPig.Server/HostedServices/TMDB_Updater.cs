using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using DustyPig.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static DustyPig.Server.Services.TMDBClient;

namespace DustyPig.Server.HostedServices
{
    public class TMDB_Updater : IHostedService, IDisposable
    {
        private const int MILLISECONDS_PER_MINUTE = 1000 * 60;

        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;
        private readonly ILogger<TMDB_Updater> _logger;
        private static readonly TMDBClient _client = new()
        {
            RetryCount = 1,
            RetryDelay = 250,
            Throttle = 250
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
                await DoUpdateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_PER_MINUTE, Timeout.Infinite);
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
                await DoUpdateAsync(item.TMDB_Id.Value, tmdbType, true);
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
                await DoUpdateAsync(item.TMDB_Id, item.MediaType, false);
        }



        /// <param name="forceUpdate">For when the tmdbId came from an unlinked MediaEntry</param>
        private async Task DoUpdateAsync(int tmdbId, TMDB_MediaTypes mediaType, bool forceUpdate)
        {
            using var db = new AppDbContext();

            try
            {
                var info = mediaType == TMDB_MediaTypes.Movie ?
                    await AddOrUpdateTMDBMovieAsync(tmdbId) :
                    await AddOrUpdateTMDBSeriesAsync(tmdbId);

                if (info != null && (forceUpdate || info.Changed))
                {
                    var conn = db.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync(_cancellationToken);

                    var entryType = (int)(mediaType == TMDB_MediaTypes.Movie ? MediaTypes.Movie : MediaTypes.Series);

                    //Popularity
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3";
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Popularity));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                    await Task.Delay(100, _cancellationToken);
                    await cmd.ExecuteNonQueryAsync(_cancellationToken);



                    //Movie Rating
                    if (entryType == (int)MediaTypes.Movie)
                    {
                        var infoRating = (int?)info.MovieRating;
                        if (infoRating > 0)
                        {
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.MovieRating)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.MovieRating)} IS NULL";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", infoRating.Value));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                            await Task.Delay(100, _cancellationToken);
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }
                    }

                    //TV Rating
                    if (entryType == (int)MediaTypes.Series)
                    {
                        var infoRating = (int?)info.TVRating;
                        if (infoRating > 0)
                        {
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TVRating)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.TVRating)} IS NULL";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", infoRating.Value));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                            await Task.Delay(100, _cancellationToken);
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }
                    }

                    //Description
                    if (!string.IsNullOrWhiteSpace(info.Overview))
                    {
                        cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Description)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND ({nameof(MediaEntry.Description)}=@p4 OR {nameof(MediaEntry.Description)} IS NULL)";
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Overview));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", ""));
                        await Task.Delay(100, _cancellationToken);
                        await cmd.ExecuteNonQueryAsync(_cancellationToken);
                    }

                    //Backdrop Url
                    if (!string.IsNullOrWhiteSpace(info.BackdropUrl))
                    {
                        cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.BackdropUrl)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p3 AND {nameof(MediaEntry.EntryType)}=@p4 AND ({nameof(MediaEntry.BackdropUrl)}=@p5 OR {nameof(MediaEntry.BackdropUrl)} IS NULL)";
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.BackdropUrl));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", tmdbId));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", entryType));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p5", ""));
                        await Task.Delay(100, _cancellationToken);
                        await cmd.ExecuteNonQueryAsync(_cancellationToken);
                    }

                    //Link
                    cmd = conn.CreateCommand();
                    cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TMDB_EntryId)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.TMDB_EntryId)} IS NULL";
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Id));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                    cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryType));
                    await Task.Delay(100, _cancellationToken);
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

                var cmd = conn.CreateCommand();
                cmd.CommandText = $"UPDATE {nameof(db.TMDB_Entries)} SET {nameof(TMDB_Entry.LastUpdated)}=@p1 WHERE {nameof(TMDB_Entry.TMDB_Id)}=@p2 AND {nameof(TMDB_Entry.MediaType)}=@p3";
                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", DateTime.UtcNow));
                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", tmdbId));
                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", (int)mediaType));
                await Task.Delay(100, _cancellationToken);
                await cmd.ExecuteNonQueryAsync(_cancellationToken);
            }
        }



        private async Task<TMDBInfo> AddOrUpdateTMDBMovieAsync(int tmdbId)
        {
            var response = await _client.GetMovieAsync(tmdbId, _cancellationToken);
            if (!response.Success)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                   await DeleteTmdbMediaEntryAsync(tmdbId, TMDB_MediaTypes.Movie);
                else
                    _logger.LogError(response.Error, "TMDB.Movie.{0}", tmdbId);
                return null;
            }
            
            var movie = response.Data;
            return await AddOrUpdateTMDBEntryAsync(tmdbId, TMDB_MediaTypes.Movie, TMDBClient.GetCommonCredits(movie), movie.BackdropPath, TMDBClient.TryGetMovieDate(movie), movie.Overview, movie.Popularity, TMDBClient.TryMapMovieRatings(movie));
        }



        private async Task<TMDBInfo> AddOrUpdateTMDBSeriesAsync(int tmdbId)
        {
            var response = await _client.GetSeriesAsync(tmdbId, _cancellationToken);
            if (!response.Success)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    await DeleteTmdbMediaEntryAsync(tmdbId, TMDB_MediaTypes.Series);
                else
                    _logger.LogError(response.Error, "TMDB.Series.{0}", tmdbId);
                return null;
            }

            var series = response.Data;
            return await AddOrUpdateTMDBEntryAsync(tmdbId, TMDB_MediaTypes.Series, TMDBClient.GetCommonCredits(series), series.BackdropPath, series.FirstAirDate, series.Overview, series.Popularity, TMDBClient.TryMapTVRatings(series));
        }



        private async Task<TMDBInfo> AddOrUpdateTMDBEntryAsync(int tmdbId, TMDB_MediaTypes mediaType, CreditsDTO credits, string backdropPath, DateOnly? date, string overview, double popularity, string rated)
        {
            using var db = new AppDbContext();

            await Task.Delay(100, _cancellationToken);
            var entry = await db.TMDB_Entries
                .Where(item => item.TMDB_Id == tmdbId)
                .Where(item => item.MediaType == mediaType)
                .FirstOrDefaultAsync(_cancellationToken);

            if (entry != null)
                if (entry.LastUpdated > DateTime.UtcNow.AddDays(-1))
                    return TMDBInfo.FromEntry(entry, false);

            
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
                    var size = await SimpleDownloader.GetDownloadSizeAsync(backdropUrl, cancellationToken: _cancellationToken);
                    entry.BackdropUrl = backdropUrl;
                    entry.BackdropSize = (ulong)size;
                    changed = true;
                }
                catch { }
            }

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
            await Task.Delay(100, _cancellationToken);
            await db.SaveChangesAsync(_cancellationToken);

            await EnsurePeopleExistAsync(credits);
            await BridgeEntryAndPeopleAsync(entry.Id, mediaType, credits);

            return TMDBInfo.FromEntry(entry, changed);
        }



        private async Task EnsurePeopleExistAsync(CreditsDTO credits)
        {
            var needed = credits.CastMembers.Select(item => item.Id).ToList();
            needed.AddRange(credits.CrewMembers.Select(item => item.Id));
            needed = needed.Distinct().ToList();
            needed.Sort();

            //Process 100 at a time to keep memory usage tiny
            int startIdx = 0;
            while (true)
            {
                var subNeeded = needed.Skip(startIdx).Take(100).ToList();
                if (subNeeded.Count == 0)
                    break;
                startIdx += 100;

                await Task.Delay(100, _cancellationToken);
                using var db = new AppDbContext();
                var existing = await db.TMDB_People
                    .AsNoTracking()
                    .Where(item => subNeeded.Contains(item.TMDB_Id))
                    .ToListAsync(_cancellationToken);

                foreach (var id in subNeeded)
                {
                    string avatarUrl;
                    string personName;

                    var neededCast = credits.CastMembers.FirstOrDefault(item => item.Id == id);
                    if (neededCast != null)
                    {
                        avatarUrl = neededCast.FullImagePath;
                        personName = neededCast.Name;
                    }
                    else
                    {
                        var neededCrew = credits.CrewMembers.FirstOrDefault(item => item.Id == id);
                        avatarUrl = neededCrew.FullImagePath;
                        personName = neededCrew.Name;
                    }


                    var entry = existing.FirstOrDefault(item => item.TMDB_Id == id);
                    if (entry == null)
                    {
                        entry = db.TMDB_People.Add(new Data.Models.TMDB_Person
                        {
                            TMDB_Id = id,
                            AvatarUrl = avatarUrl,
                            Name = personName
                        }).Entity;
                    }
                    else if (entry.Name != personName || entry.AvatarUrl != avatarUrl)
                    {
                        entry.Name = personName;
                        entry.AvatarUrl = avatarUrl;
                        db.TMDB_People.Update(entry);
                    }
                }

                await Task.Delay(100, _cancellationToken);
                await db.SaveChangesAsync(_cancellationToken);
            }
        }



        private async Task BridgeEntryAndPeopleAsync(int entryId, TMDB_MediaTypes mediaType, CreditsDTO credits)
        {
            //This is nothing but small lists of ints, no need to worry about memory
            
            using var db = new AppDbContext();

            //Remove any that are no longer valid
            await Task.Delay(100, _cancellationToken);
            var existing = await db.TMDB_EntryPeopleBridges
                .AsNoTracking()
                .Where(item => item.TMDB_EntryId == entryId)
                .ToListAsync(_cancellationToken);


            foreach(var entry in existing)
            {
                if(entry.Role == CreditRoles.Cast)
                {
                    var castMember = credits.CastMembers
                        .Where(item => item.Id == entry.TMDB_PersonId)
                        .FirstOrDefault();
                    if (castMember == null)
                        db.TMDB_EntryPeopleBridges.Remove(entry);
                }
                else
                {
                    var crewMember = credits.CrewMembers
                        .Where(item => item.Id == entry.TMDB_PersonId)
                        .Where(item => TMDBClient.GetCreditRole(item.Job) == entry.Role)
                        .FirstOrDefault();
                    if(crewMember == null)
                        db.TMDB_EntryPeopleBridges.Remove(entry);
                }
            }

            await db.SaveChangesAsync(_cancellationToken);




            //Add new or update
            var needed = credits.CastMembers.Select(item => item.Id).ToList();
            needed.AddRange(credits.CrewMembers.Select(item => item.Id));
            needed = needed.Distinct().ToList();

            await Task.Delay(100, _cancellationToken);
            existing = await db.TMDB_EntryPeopleBridges
                .AsNoTracking()
                .Where(item => item.TMDB_EntryId == entryId)
                .ToListAsync(_cancellationToken);

            foreach (var personId in needed)
            {
                if (credits.CastMembers.Any(item => item.Id == personId))
                {
                    var castMember = credits.CastMembers.First(item => item.Id == personId);
                    //castMember.Order = credits.CastMembers.Min(item => item.Order);

                    var entry = existing
                        .Where(item => item.TMDB_PersonId == personId)
                        .Where(item => item.Role == CreditRoles.Cast)
                        .FirstOrDefault();

                    if (entry == null)
                    {
                        db.TMDB_EntryPeopleBridges.Add(new TMDB_EntryPersonBridge
                        {
                            Role = CreditRoles.Cast,
                            SortOrder = castMember.Order,
                            TMDB_EntryId = entryId,
                            TMDB_PersonId = personId
                        });
                    }
                    else if (entry.SortOrder != castMember.Order)
                    {
                        entry.SortOrder = castMember.Order;
                        db.TMDB_EntryPeopleBridges.Update(entry);
                    }
                }


                SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBClient.JOB_DIRECTOR]);
                SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBClient.JOB_PRODUCER]);
                SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBClient.JOB_EXECUTIVE_PRODUCER]);
                SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBClient.JOB_WRITER, TMDBClient.JOB_SCREENPLAY]);
            }

            await db.SaveChangesAsync(_cancellationToken);
        }
        
        

        private static void SetCrew(AppDbContext db, List<CrewDTO> crew, List<TMDB_EntryPersonBridge> bridges, int entryId, int personId, string[] roleNames)
        {
            foreach (string roleName in roleNames)
            {
                bool found = false;

                foreach (var crewMember in crew.Where(item => item.Id == personId).Where(item => item.Job.ICEquals(roleName)))
                {
                    found = true;

                    var creditRole = TMDBClient.GetCreditRole(roleName) ?? throw new Exception($"Unknown credit role for: {roleName}");
                    var entry = bridges
                        .Where(item => item.TMDB_PersonId == personId)
                        .Where(item => item.Role == creditRole)
                        .FirstOrDefault();

                    if (entry == null)
                        db.TMDB_EntryPeopleBridges.Add(new TMDB_EntryPersonBridge
                        {
                            TMDB_EntryId = entryId,
                            TMDB_PersonId = personId,
                            Role = creditRole,
                        });
                }

                if (found)
                    return;
            }
        }



        private async Task DeleteTmdbMediaEntryAsync(int tmdbId, TMDB_MediaTypes mediaType)
        {
            using var db = new AppDbContext();
            using var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(_cancellationToken);


            //TMDB_Entries
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {nameof(db.TMDB_Entries)} WHERE {nameof(TMDB_Entry.TMDB_Id)}=@p1 AND {nameof(TMDB_Entry.MediaType)}=@p2";
            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", tmdbId));
            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", (int)mediaType));
            await Task.Delay(100, _cancellationToken);
            await cmd.ExecuteNonQueryAsync(_cancellationToken);


            //MediaEntries
            cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TMDB_Id)}=NULL, {nameof(MediaEntry.Popularity)}=NULL WHERE {nameof(MediaEntry.TMDB_Id)}=@p1 AND {nameof(MediaEntry.EntryType)}=@p2";
            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", tmdbId));
            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", (int)(mediaType == TMDB_MediaTypes.Movie ? MediaTypes.Movie : MediaTypes.Series)));
            await Task.Delay(100, _cancellationToken);
            await cmd.ExecuteNonQueryAsync(_cancellationToken);
        }
    }
}
