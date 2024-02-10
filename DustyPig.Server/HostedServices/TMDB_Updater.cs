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
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.HostedServices
{
    public class TMDB_Updater : IHostedService, IDisposable
    {
        private const int MILLISECONDS_PER_HOUR = 1000 * 60 * 60;

        private readonly Timer _timer;
        private CancellationToken _cancellationToken = default;
        private readonly ILogger<TMDB_Updater> _logger;
        private static readonly TMDBClient _client = new();

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

                await DoUpdate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork");
            }

            if (!_cancellationToken.IsCancellationRequested)
                _timer.Change(MILLISECONDS_PER_HOUR, Timeout.Infinite);
        }


        private async Task DoUpdate()
        {
            using var db = new AppDbContext();
            
            var entryDatQuery =
                from me in db.MediaEntries
                    .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
                    .Where(item => item.PopularityUpdated == null || item.PopularityUpdated <= DateTime.UtcNow.AddDays(-1))
                    .Where(item => item.TMDB_Id.HasValue)
                    .Where(item => item.TMDB_Id > 0)
                group me by new { me.TMDB_Id, me.EntryType } into g                
                select new
                {
                    TMDB_Id = g.Key.TMDB_Id.Value,
                    EntryType = g.Key.EntryType,
                    MinDate = g.Min(item => item.PopularityUpdated),
                    MissingDescriptionCount = g.Sum(item => item.Description == null || item.Description == "" ? 1 : 0),
                    MissingBackdropCount = g.Sum(item => item.BackdropUrl == null || item.BackdropUrl == "" ? 1 : 0),
                    MissingLinkCount = g.Sum(item => item.TMDB_EntryId == null ? 1 : 0),
                    MissingMovieRatingCount = g.Sum(item => g.Key.EntryType == MediaTypes.Movie ? item.MovieRating == null || item.MovieRating == MovieRatings.None ? 1 : 0 : 0),
                    MissingTVRatingCount = g.Sum(item => g.Key.EntryType == MediaTypes.Series ? item.TVRating == null || item.TVRating == TVRatings.None ? 1 : 0 : 0)
                };

            await Task.Delay(100, _cancellationToken);
            var entryDataList = await entryDatQuery
                .AsNoTracking()
                .OrderBy(item => item.MinDate)
                .ToListAsync(_cancellationToken);

            foreach (var entryData in entryDataList)
            {
                try
                {
                    var info = entryData.EntryType == MediaTypes.Movie ?
                        await AddOrUpdateTMDBMovieAsync(entryData.TMDB_Id, _cancellationToken) :
                        await AddOrUpdateTMDBSeriesAsync(entryData.TMDB_Id, _cancellationToken);


                    if (info != null)
                    {
                        var conn = db.Database.GetDbConnection();
                        if (conn.State != System.Data.ConnectionState.Open)
                            await conn.OpenAsync(_cancellationToken);

                        //Popularity
                        await Task.Delay(100, _cancellationToken);
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Popularity)}=@p1, {nameof(MediaEntry.PopularityUpdated)}=@p2 WHERE {nameof(MediaEntry.TMDB_Id)}=@p3 AND {nameof(MediaEntry.EntryType)}=@p4";
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Popularity));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", DateTime.UtcNow));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryData.TMDB_Id));
                        cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", (int)entryData.EntryType));
                        await cmd.ExecuteNonQueryAsync(_cancellationToken);



                        //Movie Rating
                        if (entryData.EntryType == MediaTypes.Movie && entryData.MissingMovieRatingCount > 0)
                        {
                            var infoRating = (int?)info.MovieRating;
                            if (infoRating > 0)
                            {
                                await Task.Delay(100, _cancellationToken);
                                cmd = conn.CreateCommand();
                                cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.MovieRating)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.MovieRating)} IS NULL";
                                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", infoRating.Value));
                                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", entryData.TMDB_Id));
                                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", (int)entryData.EntryType));
                                await cmd.ExecuteNonQueryAsync(_cancellationToken);
                            }
                        }

                        //TV Rating
                        if (entryData.EntryType == MediaTypes.Series && entryData.MissingTVRatingCount > 0)
                        {
                            var infoRating = (int?)info.TVRating;
                            if (infoRating > 0)
                            {
                                await Task.Delay(100, _cancellationToken);
                                cmd = conn.CreateCommand();
                                cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TVRating)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.TVRating)} IS NULL";
                                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", infoRating.Value));
                                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", entryData.TMDB_Id));
                                cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", (int)entryData.EntryType));
                                await cmd.ExecuteNonQueryAsync(_cancellationToken);
                            }
                        }

                        //Description
                        if (entryData.MissingDescriptionCount > 0 && !string.IsNullOrWhiteSpace(info.Overview))
                        {
                            await Task.Delay(100, _cancellationToken);
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.Description)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND ({nameof(MediaEntry.Description)}=@p4 OR {nameof(MediaEntry.Description)} IS NULL)";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Overview));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", entryData.TMDB_Id));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", (int)entryData.EntryType));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", ""));
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }

                        //Backdrop Url
                        if (entryData.MissingBackdropCount > 0 && !string.IsNullOrWhiteSpace(info.BackdropUrl))
                        {
                            await Task.Delay(100, _cancellationToken);
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.BackdropUrl)}=@p1, {nameof(MediaEntry.BackdropSize)}=@p2 WHERE {nameof(MediaEntry.TMDB_Id)}=@p3 AND {nameof(MediaEntry.EntryType)}=@p4 AND ({nameof(MediaEntry.BackdropUrl)}=@p5 OR {nameof(MediaEntry.BackdropUrl)} IS NULL)";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.BackdropUrl));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", (ulong)info.BackdropSize));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", entryData.TMDB_Id));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p4", (int)entryData.EntryType));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p5", ""));
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }

                        //Link
                        if (entryData.MissingLinkCount > 0)
                        {
                            await Task.Delay(100, _cancellationToken);
                            cmd = conn.CreateCommand();
                            cmd.CommandText = $"UPDATE {nameof(db.MediaEntries)} SET {nameof(MediaEntry.TMDB_EntryId)}=@p1 WHERE {nameof(MediaEntry.TMDB_Id)}=@p2 AND {nameof(MediaEntry.EntryType)}=@p3 AND {nameof(MediaEntry.TMDB_EntryId)} IS NULL";
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p1", info.Id));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p2", entryData.TMDB_Id));
                            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("p3", (int)entryData.EntryType));
                            await cmd.ExecuteNonQueryAsync(_cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }



        public static async Task<TMDBInfo> AddOrUpdateTMDBMovieAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1000, cancellationToken);
            var response = await _client.GetMovieAsync(tmdbId, cancellationToken);
            if (!response.Success)
                return null;
            var movie = response.Data;
            if (movie == null)
                return null;

            using var db = new AppDbContext();

            await Task.Delay(100, cancellationToken);
            var entry = await db.TMDB_Entries
                .AsNoTracking()
                .Where(item => item.TMDB_Id == movie.Id)
                .Where(item => item.MediaType == TMDB_MediaTypes.Movie)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry != null)
                if (entry.LastUpdated > DateTime.UtcNow.AddDays(-1))
                    return TMDBInfo.FromEntry(entry); ;


            //Make sure all the people exist
            movie.Credits ??= new();
            movie.Credits.Cast ??= [];
            movie.Credits.Crew ??= [];
            await EnsurePeopleExistAsync(movie.Credits, cancellationToken);

            //Make sure movie exists
            var backdropUrl = TMDB.Utils.GetFullBackdropPath(movie.BackdropPath, false);


            bool changed = false;
            bool newEntry = false;
            if (entry == null)
            {
                entry = new TMDB_Entry
                {
                    TMDB_Id = movie.Id,
                    MediaType = TMDB_MediaTypes.Movie
                };
                changed = true;
                newEntry = true;
            }

            if (entry.BackdropUrl != backdropUrl)
            {
                try
                {
                    var size = await SimpleDownloader.GetDownloadSizeAsync(entry.BackdropUrl, cancellationToken);
                    entry.BackdropUrl = backdropUrl;
                    entry.BackdropSize = (ulong)size;
                    changed = true;
                }
                catch { }
            }
            if (entry.Date != movie.ReleaseDate)
            {
                entry.Date = movie.ReleaseDate;
                changed = true;
            }
            if (entry.Description != movie.Overview)
            {
                entry.Description = movie.Overview;
                changed = true;
            }
            if (entry.Popularity != movie.Popularity)
            {
                entry.Popularity = movie.Popularity;
                changed = true;
            }
           
            //Ratings
            var ratingStr = TryMapMovieRatings(movie.Releases);
            MovieRatings? movieRating = string.IsNullOrWhiteSpace(ratingStr) ? null : ratingStr.ToMovieRatings();
            if (movieRating == MovieRatings.None)
                movieRating = null;

            if (entry.MovieRating != movieRating && movieRating != null)
            {
                entry.MovieRating = movieRating;
                changed = true;
            }


            if (changed)
            {
                entry.LastUpdated = DateTime.UtcNow;
                if (newEntry)
                    db.TMDB_Entries.Add(entry);
                else
                    db.TMDB_Entries.Update(entry);
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            await BridgeEntryAndPeopleAsync(entry.TMDB_Id, TMDB_MediaTypes.Movie, movie.Credits, cancellationToken);

            return TMDBInfo.FromEntry(entry);
        }

        public static async Task<TMDBInfo> AddOrUpdateTMDBSeriesAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1000, cancellationToken);
            var response = await _client.GetSeriesAsync(tmdbId, cancellationToken);
            if (!response.Success)
                return null;
            var series = response.Data;
            if (series == null)
                return null;

            using var db = new AppDbContext();

            await Task.Delay(100, cancellationToken);
            var entry = await db.TMDB_Entries
                .AsNoTracking()
                .Where(item => item.TMDB_Id == series.Id)
                .Where(item => item.MediaType == TMDB_MediaTypes.Series)
                .FirstOrDefaultAsync(cancellationToken);

            if (entry != null)
                if (entry.LastUpdated > DateTime.UtcNow.AddDays(-1))
                    return TMDBInfo.FromEntry(entry);


            //Make sure all the people exist
            series.Credits ??= new();
            series.Credits.Cast ??= [];
            series.Credits.Crew ??= [];
            await EnsurePeopleExistAsync(series.Credits, cancellationToken);

            //Make sure entity exists
            var backdropUrl = TMDB.Utils.GetFullBackdropPath(series.BackdropPath, false);


            bool changed = false;
            bool newEntry = false;
            if (entry == null)
            {
                entry = new TMDB_Entry
                {
                    TMDB_Id = series.Id,
                    MediaType = TMDB_MediaTypes.Series
                };
                changed = true;
                newEntry = true;
            }

            if (entry.BackdropUrl != backdropUrl)
            {
                try
                {
                    var size = await SimpleDownloader.GetDownloadSizeAsync(entry.BackdropUrl, cancellationToken);
                    entry.BackdropUrl = backdropUrl;
                    entry.BackdropSize = (ulong)size;
                    changed = true;
                }
                catch { }
            }
            if (entry.Date != series.FirstAirDate)
            {
                entry.Date = series.FirstAirDate;
                changed = true;
            }
            if (entry.Description != series.Overview)
            {
                entry.Description = series.Overview;
                changed = true;
            }
            if (entry.Popularity != series.Popularity)
            {
                entry.Popularity = series.Popularity;
                changed = true;
            }
            
            //Ratings
            var ratingStr = TryMapTVRatings(series.ContentRatings);
            TVRatings? tvRating = string.IsNullOrWhiteSpace(ratingStr) ? null : ratingStr.ToTVRatings();
            if (tvRating == TVRatings.None)
                tvRating = null;

            if (entry.TVRating != tvRating && tvRating != null)
            {
                entry.TVRating = tvRating;
                changed = true;
            }

            if (changed)
            {
                entry.LastUpdated = DateTime.UtcNow;
                if (newEntry)
                    db.TMDB_Entries.Add(entry);
                else
                    db.TMDB_Entries.Update(entry);
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync();
            }

            await BridgeEntryAndPeopleAsync(entry.TMDB_Id, TMDB_MediaTypes.Series, series.Credits, cancellationToken);

            return TMDBInfo.FromEntry(entry);
        }




        private static async Task EnsurePeopleExistAsync(Credits credits, CancellationToken cancellationToken)
        {
            var alreadyProcessed = new List<int>();

            credits.Cast = credits.Cast
                .OrderBy(item => item.Order)
                .Take(25)
                .ToList();

            foreach (var apiPerson in credits.Cast.Where(item => !alreadyProcessed.Contains(item.Id)))
            {
                int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                alreadyProcessed.Add(tmdbId);
            }

            int directorCount = 0;
            int producerCount = 0;
            int writerCount = 0;
            foreach (var apiPerson in credits.Crew.Where(item => !alreadyProcessed.Contains(item.Id)))
            {
                if (apiPerson.Job.ICEquals("Director") && directorCount < 25)
                {
                    int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                    alreadyProcessed.Add(tmdbId);
                    directorCount++;
                }

                if (apiPerson.Job.ICEquals("Producer") && producerCount < 25)
                {
                    int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                    alreadyProcessed.Add(tmdbId);
                    producerCount++;
                }

                if (apiPerson.Job.ICEquals("Writer") && writerCount < 25)
                {
                    int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                    alreadyProcessed.Add(tmdbId);
                    writerCount++;
                }
            }

            if (producerCount == 0)
                foreach (var apiPerson in credits.Crew.Where(item => !alreadyProcessed.Contains(item.Id)))
                {
                    if (apiPerson.Job.ICEquals("Executive Producer") && producerCount < 25)
                    {
                        int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                        alreadyProcessed.Add(tmdbId);
                        producerCount++;
                    }
                }

            if (writerCount == 0)
                foreach (var apiPerson in credits.Crew.Where(item => !alreadyProcessed.Contains(item.Id)))
                {
                    if (apiPerson.Job.ICEquals("Screenplay") && writerCount < 25)
                    {
                        int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                        alreadyProcessed.Add(tmdbId);
                        writerCount++;
                    }
                }

            if (writerCount == 0)
                foreach (var apiPerson in credits.Crew.Where(item => !alreadyProcessed.Contains(item.Id)))
                {
                    if (apiPerson.Job.ICEquals("Story") && writerCount < 25)
                    {
                        int tmdbId = await AddOrUpdatePersonAsync(apiPerson, cancellationToken);
                        alreadyProcessed.Add(tmdbId);
                        writerCount++;
                    }
                }
        }

        private static Task<int> AddOrUpdatePersonAsync(Cast cast, CancellationToken cancellationToken) => AddOrUpdatePersonAsync(cast.Id, cast.Name, cast.ProfilePath, cancellationToken);

        private static Task<int> AddOrUpdatePersonAsync(Crew crew, CancellationToken cancellationToken) => AddOrUpdatePersonAsync(crew.Id, crew.Name, crew.ProfilePath, cancellationToken);

        private static async Task<int> AddOrUpdatePersonAsync(int tmdbId, string name, string profilePath, CancellationToken cancellationToken)
        {
            using var db = new AppDbContext();

            await Task.Delay(100, cancellationToken);
            var dbPerson = await db.TMDB_People
                .AsNoTracking()
                .Where(item => item.TMDB_Id == tmdbId)
                .FirstOrDefaultAsync(cancellationToken);

            if (dbPerson == null)
            {
                dbPerson = db.TMDB_People.Add(new TMDB_Person
                {
                    TMDB_Id = tmdbId,
                    Name = name,
                    AvatarUrl = TMDB.Utils.GetFullPosterPath(profilePath, false)
                }).Entity;
            }
            else
            {
                string avatarUrl = TMDB.Utils.GetFullPosterPath(profilePath, false);

                if (dbPerson.Name != name || dbPerson.AvatarUrl != avatarUrl)
                {
                    dbPerson.Name = name;
                    dbPerson.AvatarUrl = avatarUrl;
                    db.TMDB_People.Update(dbPerson);
                }
            }

            await Task.Delay(100, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return dbPerson.TMDB_Id;
        }





        private static async Task BridgeEntryAndPeopleAsync(int tmdbId, TMDB_MediaTypes mediaType, Credits credits, CancellationToken cancellationToken)
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
                .Where(item => item.Role == Roles.Cast)
                .OrderBy(item => item.SortOrder)
                .ToList();

            var bridgeIds = bridges
                .Select(item => item.TMDB_PersonId)
                .ToList();

            var apiCast = credits.Cast
                .OrderBy(item => item.Order)
                .ToList();

            var apiCastIds = apiCast.Select(item => item.Id).ToList();

            if (!bridgeIds.SequenceEqual(apiCastIds))
            {
                bridges.ForEach(item => db.TMDB_EntryPeopleBridges.Remove(item));
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);

                foreach (var item in credits.Cast)
                    db.TMDB_EntryPeopleBridges.Add(new TMDB_EntryPersonBridge
                    {
                        TMDB_EntryId = entry.Id,
                        TMDB_PersonId = item.Id,
                        Role = Roles.Cast,
                        SortOrder = item.Order
                    });

                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }



            //Directors
            await SetCrew(entry, Roles.Director, credits, "Director", null, null, cancellationToken);
            await SetCrew(entry, Roles.Producer, credits, "Producer", "Executive Producer", null, cancellationToken);
            await SetCrew(entry, Roles.Writer, credits, "Writer", "Screenplay", "Story", cancellationToken);

        }

        private static async Task SetCrew(TMDB_Entry entry, Roles role, Credits credits, string job1, string job2, string job3, CancellationToken cancellationToken)
        {
            using var db = new AppDbContext();

            var bridges = entry.People
                .Where(item => item.Role == role)
                .ToList();

            var bridgeIds = bridges
                .Select(item => item.TMDB_PersonId)
                .Distinct()
                .OrderBy(item => item)
                .ToList();

            var crew = credits.Crew
                .Where(item => item.Job.ICEquals(job1))
                .ToList();

            if (crew.Count == 0 && !string.IsNullOrWhiteSpace(job2))
                crew = credits.Crew
                    .Where(item => item.Job.ICEquals(job2))
                    .ToList();

            if (crew.Count == 0 && !string.IsNullOrWhiteSpace(job3))
                crew = credits.Crew
                    .Where(item => item.Job.ICEquals(job3))
                    .ToList();

            var apiCastIds = crew
                .Select(item => item.Id)
                .Distinct()
                .OrderBy(item => item)
                .ToList();

            if (!bridgeIds.SequenceEqual(apiCastIds))
            {
                bridges.ForEach(item => db.TMDB_EntryPeopleBridges.Remove(item));
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                foreach (var item in crew)
                    db.TMDB_EntryPeopleBridges.Add(new TMDB_EntryPersonBridge
                    {
                        TMDB_EntryId = entry.Id,
                        TMDB_PersonId = item.Id,
                        Role = role
                    });
                await Task.Delay(100, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            return;

        }




        public static string TryMapMovieRatings(Releases releases)
        {
            if (releases == null)
                return null;

            if (releases.Countries == null)
                return null;

            foreach (var country in releases.Countries.Where(item => item.Name.ICEquals("US")))
                if (TryMapMovieRatings(country.Name, country.Certification, out string ret))
                    return ret;

            foreach (var country in releases.Countries)
                if (TryMapTVRatings(country.Name, country.Certification, out string ret))
                    return ret;

            return null;
        }

        public static string TryMapTVRatings(ContentRatings contentRatings)
        {
            if (contentRatings == null)
                return null;

            if (contentRatings.Results == null)
                return null;

            foreach (var contentRating in contentRatings.Results.Where(item => item.Country.ICEquals("US")))
                if (TryMapTVRatings(contentRating.Country, contentRating.Rating, out string ret))
                    return ret;

            foreach (var contentRating in contentRatings.Results)
                if (TryMapMovieRatings(contentRating.Country, contentRating.Rating, out string ret))
                    return ret;

            return null;
        }

        private static bool TryMapMovieRatings(string country, string rating, out string rated)
        {
            rated = null;

            if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(rating))
                return false;

            rated = RatingsUtils.MapMovieRatings(country, rating);
            if (!string.IsNullOrWhiteSpace(rated))
                return true;

            rated = RatingsUtils.MapTVRatings(country, rating);
            if (!string.IsNullOrWhiteSpace(rated))
                return true;

            return false;
        }

        private static bool TryMapTVRatings(string country, string rating, out string rated)
        {
            rated = null;

            if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(rating))
                return false;

            rated = RatingsUtils.MapTVRatings(country, rating);
            if (!string.IsNullOrWhiteSpace(rated))
                return true;

            rated = RatingsUtils.MapMovieRatings(country, rating);
            if (!string.IsNullOrWhiteSpace(rated))
                return true;

            return false;
        }


        public class TMDBInfo
        {
            /// <summary>
            /// DB Id, not TMDB_ID
            /// </summary>
            public int Id { get; set; }
            public double Popularity { get; set; }
            public string BackdropUrl { get; set; }
            public ulong BackdropSize { get; set; }
            public MovieRatings? MovieRating { get; set; }
            public TVRatings? TVRating { get; set; }
            public string Overview { get; set; }

            public static TMDBInfo FromEntry(TMDB_Entry entry)
            {
                return new TMDBInfo
                {
                    Id = entry.Id,
                    BackdropUrl = entry.BackdropUrl,
                    BackdropSize = entry.BackdropSize,
                    MovieRating = entry.MovieRating,
                    Overview = entry.Description,
                    Popularity = entry.Popularity,
                    TVRating = entry.TVRating
                };
            }
        }

    }
}
