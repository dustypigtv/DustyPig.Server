using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.REST;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Extensions;
using DustyPig.Server.Services.TMDB_Service;
using DustyPig.Timers;
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


namespace DustyPig.Server.HostedServices;

public class TMDB_Updater : IHostedService, IDisposable
{
    private const int CHUNK_SIZE = 1000;

    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly SafeTimer _timer;
    private readonly ILogger<TMDB_Updater> _logger;


    public TMDB_Updater(IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> dbContextFactory, ILogger<TMDB_Updater> logger)
    {
        _serviceProvider = serviceProvider;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _timer = new(TimerTick);
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
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
        try
        {
            using var db = _dbContextFactory.CreateDbContext();
            await DoUpdateAsync(db, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DoWork");
        }
    }



    private async Task DoUpdateAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        //Get tmdb_ids from MediaEntries that have not yet been linked to a TMDB_Entry.
        //These are newly added and should update first
        int start = 0;
        while (true)
        {
            var newItemsLst = await db.MediaEntries
                .AsNoTracking()
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
                .Where(m => m.TMDB_Id.HasValue)
                .Where(m => m.TMDB_Id > 0)
                .Where(m => m.TMDB_Updated < DateTime.UtcNow.AddDays(-1))
                .Select(m => new
                {
                    m.TMDB_Id,
                    m.EntryType,
                    m.TMDB_Updated
                })
                .Distinct()
                .OrderBy(m => m.TMDB_Updated)
                .Skip(start)
                .Take(CHUNK_SIZE)
                .ToListAsync(cancellationToken);

            if (newItemsLst.Count == 0)
                break;


            foreach (var item in newItemsLst)
            {
                var tmdbType = item.EntryType == MediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series;
                await DoUpdateAsync(db, item.TMDB_Id.Value, tmdbType, true, cancellationToken);
            }

            start += CHUNK_SIZE;
        }


        start = 0;
        while (true)
        {
            //Now get any existing tmdb entries that are due to update
            var updateItemsLst = await db.TMDB_Entries
                .AsNoTracking()
                .Where(m => m.LastUpdated < DateTime.UtcNow.AddDays(-1))
                .Select(m => new
                {
                    m.TMDB_Id,
                    m.MediaType,
                    m.LastUpdated
                })
                .Distinct()
                .OrderBy(m => m.LastUpdated)
                .Skip(start)
                .Take(CHUNK_SIZE)
                .ToListAsync(cancellationToken);

            if (updateItemsLst.Count == 0)
                break;

            foreach (var item in updateItemsLst)
                await DoUpdateAsync(db, item.TMDB_Id, item.MediaType, false, cancellationToken);

            start += CHUNK_SIZE;
        }
    }


    /// <param name="forceUpdate">For when the tmdbId came from an unlinked MediaEntry</param>
    private async Task DoUpdateAsync(AppDbContext db, int tmdbId, TMDB_MediaTypes mediaType, bool forceUpdate, CancellationToken cancellationToken)
    {
        var entryType = mediaType == TMDB_MediaTypes.Movie ? MediaTypes.Movie : MediaTypes.Series;

        bool success = true;
        int start = 0;
        try
        {
            var info = mediaType == TMDB_MediaTypes.Movie ?
                await UpdateTMDBMovie(db, tmdbId, cancellationToken) :
                await UpdateTMDBSeries(db, tmdbId, cancellationToken);

            if (info != null && (forceUpdate || info.Changed))
            {
                var infoRating = mediaType == TMDB_MediaTypes.Movie ? (int?)info.MovieRating : (int?)info.TVRating;

                while (true)
                {
                    var mediaEntries = await db.MediaEntries
                        .Where(_ => _.TMDB_Id == tmdbId)
                        .Where(_ => _.EntryType == entryType)
                        .OrderBy(_ => _.Id)
                        .Skip(start)
                        .Take(CHUNK_SIZE)
                        .ToListAsync(cancellationToken);

                    if (mediaEntries.Count == 0)
                        break;

                    foreach (var mediaEntry in mediaEntries)
                    {
                        if (!mediaEntry.Popularity.HasValue || mediaEntry.Popularity.Value != info.Popularity)
                            mediaEntry.Popularity = info.Popularity;

                        if (infoRating > 0)
                        {
                            if (mediaEntry.EntryType == MediaTypes.Movie && mediaEntry.MovieRating == null)
                                mediaEntry.MovieRating = info.MovieRating;
                            else if (mediaEntry.EntryType == MediaTypes.Series && mediaEntry.TVRating == null)
                                mediaEntry.TVRating = info.TVRating;
                        }
                        if (mediaEntry.Description.IsNullOrWhiteSpace() && info.Overview.HasValue())
                            mediaEntry.Description = info.Overview;

                        if (mediaEntry.BackdropUrl.IsNullOrWhiteSpace() && info.BackdropUrl.HasValue())
                            mediaEntry.BackdropUrl = info.BackdropUrl;

                        if (mediaEntry.TMDB_EntryId == null)
                            mediaEntry.TMDB_EntryId = info.Id;

                        mediaEntry.TMDB_Updated = DateTime.UtcNow;
                    }

                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Update media entry");
                        success = false;

                        //Stop if cancelled, otherwise continue with next batch
                        if (ex is OperationCanceledException)
                            throw;
                    }

                    start += CHUNK_SIZE;
                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update entry");
            success = false;
        }


        if (success)
            return;

        //Make sure failures don't repeatedly try - wait a day!
        start = 0;
        while (true)
        {
            try
            {
                var mediaEntries = await db.MediaEntries
                    .Where(_ => _.TMDB_Id == tmdbId)
                    .Where(_ => _.EntryType == entryType)
                    .Skip(start)
                    .Take(CHUNK_SIZE)
                    .ToListAsync(cancellationToken);

                start += CHUNK_SIZE;
                if (mediaEntries.Count == 0)
                    break;

                mediaEntries.ForEach(_ => _.TMDB_Updated = DateTime.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Set MediaEntry.TMDB_Updated");
            }
        }


        try
        {
            var tmdbEntry = await db.TMDB_Entries
                .Where(_ => _.TMDB_Id == tmdbId)
                .Where(_ => _.MediaType == mediaType)
                .FirstOrDefaultAsync(cancellationToken);

            if (tmdbEntry == null)
                return;

            tmdbEntry.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set TMDB_Entry.LastUpdated");
        }

    }



    private async Task<TMDBInfo> UpdateTMDBMovie(AppDbContext db, int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await db.TMDB_Entries
                .Where(item => item.TMDB_Id == tmdbId)
                .Where(item => item.MediaType == TMDB_MediaTypes.Movie)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry != null)
                if (entry.LastUpdated > DateTime.UtcNow.AddDays(-1))
                    return TMDBInfo.FromEntry(entry, false);


            using var scope = _serviceProvider.CreateScope();
            var tmdbService = scope.ServiceProvider.GetRequiredService<TMDBService>();
            var response = await tmdbService.GetMovieAsync(tmdbId, cancellationToken);

            var movie = response.Data;
            return await AddOrUpdateTMDBEntry(db, entry, tmdbId, TMDB_MediaTypes.Movie, TMDBService.GetCommonCredits(movie), movie.BackdropPath, TMDBService.TryGetMovieDate(movie), movie.Overview, movie.Popularity, TMDBService.TryMapMovieRatings(movie), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTMDBMovie) + "({tmdbid})", tmdbId);
            if (ex is RestException rex && rex.StatusCode == System.Net.HttpStatusCode.NotFound)
                await DeleteTmdbEntry(db, tmdbId, TMDB_MediaTypes.Movie, cancellationToken);
            throw;
        }
    }



    private async Task<TMDBInfo> UpdateTMDBSeries(AppDbContext db, int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await db.TMDB_Entries
               .Where(item => item.TMDB_Id == tmdbId)
               .Where(item => item.MediaType == TMDB_MediaTypes.Series)
               .FirstOrDefaultAsync(cancellationToken);

            if (entry != null)
                if (entry.LastUpdated > DateTime.UtcNow.AddDays(-1))
                    return TMDBInfo.FromEntry(entry, false);

            using var scope = _serviceProvider.CreateScope();
            var tmdbService = scope.ServiceProvider.GetRequiredService<TMDBService>();
            var response = await tmdbService.GetSeriesAsync(tmdbId, cancellationToken);

            var series = response.Data;
            return await AddOrUpdateTMDBEntry(db, entry, tmdbId, TMDB_MediaTypes.Series, TMDBService.GetCommonCredits(series), series.BackdropPath, null, series.Overview, series.Popularity, TMDBService.TryMapTVRatings(series), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTMDBSeries) + "({tmdbid})", tmdbId);
            if (ex is RestException rex && rex.StatusCode == System.Net.HttpStatusCode.NotFound)
                await DeleteTmdbEntry(db, tmdbId, TMDB_MediaTypes.Series, cancellationToken);
            throw;
        }
    }



    private async Task<TMDBInfo> AddOrUpdateTMDBEntry(AppDbContext db, TMDB_Entry entry, int tmdbId, TMDB_MediaTypes mediaType, CreditsDTO credits, string backdropPath, DateOnly? date, string overview, double popularity, string rated, CancellationToken cancellationToken)
    {
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

        if (entry.BackdropUrl != backdropUrl && backdropUrl.HasValue())
        {
            try
            {
                entry.BackdropUrl = backdropUrl;
                changed = true;
            }
            catch { }
        }

        if (entry.Date != date && date.HasValue)
        {
            entry.Date = date;
            changed = true;
        }
        if (entry.Description != overview && overview.HasValue())
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
        if (rated.HasValue())
        {
            if (mediaType == TMDB_MediaTypes.Movie)
            {
                MovieRatings? movieRating = rated.ToMovieRatings();
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
                TVRatings? tvRating = rated.ToTVRatings();
                if (tvRating == TVRatings.None)
                    tvRating = null;

                if (entry.TVRating != tvRating && tvRating != null)
                {
                    entry.TVRating = tvRating;
                    changed = true;
                }
            }
        }


        //Save changes
        entry.LastUpdated = DateTime.UtcNow;
        if (newEntry)
            db.TMDB_Entries.Add(entry);
        else
            db.TMDB_Entries.Update(entry);
        await db.SaveChangesAsync(cancellationToken);

        await EnsurePeopleExistAsync(db, credits, cancellationToken);
        await BridgeEntryAndPeopleAsync(db, entry.Id, mediaType, credits, cancellationToken);

        return TMDBInfo.FromEntry(entry, changed);
    }



    private async Task EnsurePeopleExistAsync(AppDbContext db, CreditsDTO credits, CancellationToken cancellationToken)
    {
        var needed = credits.CastMembers.Select(item => item.Id).ToList();
        needed.AddRange(credits.CrewMembers.Select(item => item.Id));
        needed = needed.Distinct().ToList();
        needed.Sort();


        int startIdx = 0;
        while (true)
        {
            var subNeeded = needed.Skip(startIdx).Take(CHUNK_SIZE).ToList();
            if (subNeeded.Count == 0)
                break;
            startIdx += CHUNK_SIZE;

            var existing = await db.TMDB_People
                .AsNoTracking()
                .Where(item => subNeeded.Contains(item.TMDB_Id))
                .ToListAsync(cancellationToken);

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
                    //Add new
                    entry = db.TMDB_People.Add(new Data.Models.TMDB_Person
                    {
                        TMDB_Id = id,
                        AvatarUrl = avatarUrl,
                        Name = personName
                    }).Entity;
                }
                else
                {
                    //Update existing if different
                    if (entry.Name != personName && personName.HasValue())
                    {
                        entry.Name = personName;
                        db.TMDB_People.Update(entry);
                    }

                    if (entry.AvatarUrl != avatarUrl && avatarUrl.HasValue())
                    {
                        entry.AvatarUrl = avatarUrl;
                        db.TMDB_People.Update(entry);
                    }
                }
            }


            await db.SaveChangesAsync(cancellationToken);
        }
    }


    /// <summary>
    /// This WILL call SaveChangesAsync
    /// </summary>
    private async Task BridgeEntryAndPeopleAsync(AppDbContext db, int entryId, TMDB_MediaTypes mediaType, CreditsDTO credits, CancellationToken cancellationToken)
    {
        //Remove any that are no longer valid
        var existing = await db.TMDB_EntryPeopleBridges
            .AsNoTracking()
            .Where(item => item.TMDB_EntryId == entryId)
            .ToListAsync(cancellationToken);


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

        await db.SaveChangesAsync(cancellationToken);




        //Add new or update
        var neededIds = credits.CastMembers.Select(item => item.Id).ToList();
        neededIds.AddRange(credits.CrewMembers.Select(item => item.Id));
        neededIds = neededIds.Distinct().ToList();

        existing = await db.TMDB_EntryPeopleBridges
            .AsNoTracking()
            .Where(item => item.TMDB_EntryId == entryId)
            .ToListAsync(cancellationToken);

        foreach (var personId in neededIds)
        {
            //Set cast
            if (credits.CastMembers.Any(item => item.Id == personId))
            {
                var castMember = credits.CastMembers.First(item => item.Id == personId);

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

            //Crew
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_DIRECTOR]);
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_PRODUCER]);
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_EXECUTIVE_PRODUCER]);
            SetCrew(db, credits.CrewMembers, existing, entryId, personId, [TMDBService.JOB_WRITER, TMDBService.JOB_SCREENPLAY]);
        }

        await db.SaveChangesAsync(cancellationToken);
    }


    /// <summary>
    /// This adds entitie, but does not call SaveChangesAsync
    /// </summary>
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


    private async Task DeleteTmdbEntry(AppDbContext db, int tmdbId, TMDB_MediaTypes mediaType, CancellationToken cancellationToken)
    {
        try
        {
            var tmdbEntry = await db.TMDB_Entries
                .Where(_ => _.Id == tmdbId)
                .Where(_ => _.MediaType == mediaType)
                .FirstOrDefaultAsync(cancellationToken);

            if (tmdbEntry == null)
                return;

            db.TMDB_Entries.Remove(tmdbEntry);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(DeleteTmdbEntry));
        }
    }
}
