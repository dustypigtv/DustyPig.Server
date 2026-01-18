using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Services;
using DustyPig.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DustyPig.Server.Services.TMDBService;

namespace DustyPig.Server.HostedServices;

public class TMDB_Updater : IHostedService, IDisposable
{
    private const int ONE_MINUTE = 1000 * 60;
    private const int CHUNK_SIZE = 1000;

    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _timer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private readonly ILogger<TMDB_Updater> _logger;


    public TMDB_Updater(IServiceProvider serviceProvider, ILogger<TMDB_Updater> logger)
    {
        _serviceProvider = serviceProvider;
        _cancellationToken = _cancellationTokenSource.Token;
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
            try
            {
                await DoUpdateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }
        }

        if (!_cancellationToken.IsCancellationRequested)
            try { _timer.Change(ONE_MINUTE, Timeout.Infinite); }
            catch { }
    }



    private async Task DoUpdateAsync()
    {
        using var db = new AppDbContext();

        //Get tmdb_ids from MediaEntries that have not yet been linked to a TMDB_Entry.
        //These are newly added and should update first
        int start = 0;
        while (true)
        {
            var newItemsLst = await db.MediaEntries
                .AsNoTracking()
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
                .Where(m => m.TMDB_EntryId == null)
                .Where(m => m.TMDB_Id.HasValue)
                .Where(m => m.TMDB_Id > 0)
                .OrderBy(m => m.Added)
                .Skip(start)
                .Take(CHUNK_SIZE)
                .Select(m => new
                {
                    m.TMDB_Id,
                    m.EntryType
                })
                .Distinct()
                .ToListAsync(_cancellationToken);

            if (newItemsLst.Count == 0)
                break;

            foreach (var item in newItemsLst)
            {
                var tmdbType = item.EntryType == MediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series;
                await DoUpdateAsync(item.TMDB_Id.Value, tmdbType, true);
            }

            start += CHUNK_SIZE;
        }


        start = 0;
        while (true)
        {
            //Now get any existing tmdb entries that are due to update
            await Task.Delay(100, _cancellationToken);
            var updateItemsLst = await db.TMDB_Entries
                .AsNoTracking()
                .Where(m => m.LastUpdated < DateTime.UtcNow.AddDays(-1))
                .OrderBy(m => m.LastUpdated)
                .Skip(start)
                .Take(CHUNK_SIZE)
                .Select(m => new
                {
                    m.TMDB_Id,
                    m.MediaType
                })
                .Distinct()
                .ToListAsync(_cancellationToken);

            if (updateItemsLst.Count == 0)
                break;

            foreach (var item in updateItemsLst)
                await DoUpdateAsync(item.TMDB_Id, item.MediaType, false);

            start += CHUNK_SIZE;
        }
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
        var tmdbService = _serviceProvider.GetRequiredService<TMDBService>();

        var response = await tmdbService.GetMovieAsync(tmdbId, _cancellationToken);
        if (!response.Success)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                await DeleteTmdbMediaEntryAsync(tmdbId, TMDB_MediaTypes.Movie);
            else
                _logger.LogError(response.Error, "TMDB.Movie.{0}", tmdbId);
            return null;
        }

        var movie = response.Data;
        return await AddOrUpdateTMDBEntryAsync(tmdbId, TMDB_MediaTypes.Movie, TMDBService.GetCommonCredits(movie), movie.BackdropPath, TMDBService.TryGetMovieDate(movie), movie.Overview, movie.Popularity, TMDBService.TryMapMovieRatings(movie));
    }



    private async Task<TMDBInfo> AddOrUpdateTMDBSeriesAsync(int tmdbId)
    {
        var tmdbService = _serviceProvider.GetRequiredService<TMDBService>();
        var response = await tmdbService.GetSeriesAsync(tmdbId, _cancellationToken);
        if (!response.Success)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                await DeleteTmdbMediaEntryAsync(tmdbId, TMDB_MediaTypes.Series);
            else
                _logger.LogError(response.Error, "TMDB.Series.{0}", tmdbId);
            return null;
        }

        var series = response.Data;
        return await AddOrUpdateTMDBEntryAsync(tmdbId, TMDB_MediaTypes.Series, TMDBService.GetCommonCredits(series), series.BackdropPath, series.FirstAirDate, series.Overview, series.Popularity, TMDBService.TryMapTVRatings(series));
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


        var backdropUrl = TMDBService.GetPosterPath(backdropPath);

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
                var size = await Program.SharedHttpClient.GetDownloadSizeAsync(backdropUrl, cancellationToken: _cancellationToken);
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


        foreach (var entry in existing)
        {
            if (entry.Role == CreditRoles.Cast)
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
                    .Where(item => TMDBService.GetCreditRole(item.Job) == entry.Role)
                    .FirstOrDefault();
                if (crewMember == null)
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


            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_DIRECTOR]);
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_PRODUCER]);
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_EXECUTIVE_PRODUCER]);
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_WRITER, TMDBService.JOB_SCREENPLAY]);
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

                var creditRole = TMDBService.GetCreditRole(roleName) ?? throw new Exception($"Unknown credit role for: {roleName}");
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
