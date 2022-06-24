using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Services;
using DustyPig.TMDB;
using DustyPig.TMDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ProhibitTestUser]
    [ExceptionLogger(typeof(TMDBController))]
    [SwaggerResponse((int)HttpStatusCode.OK)]
    [SwaggerResponse((int)HttpStatusCode.BadRequest)]
    [SwaggerResponse((int)HttpStatusCode.Unauthorized)]
    [SwaggerResponse((int)HttpStatusCode.Forbidden)]
    [SwaggerResponse((int)HttpStatusCode.NotFound)]
    public class TMDBController : _BaseProfileController
    {
        private readonly TMDBClient _client;

        public TMDBController(AppDbContext db, TMDBClient client) : base(db)
        {
            _client = client;
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DetailedTMDB>> GetMovie(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid;

            var movie = await _client.GetMovieAsync(id);
            if (!movie.Success)
            {
                if (movie.Error.InnerException is System.Net.Http.HttpRequestException httpEx)
                    if (httpEx.StatusCode == HttpStatusCode.NotFound)
                        return NotFound(movie.Error.GetErrorResponse().StatusMessage);
                return BadRequest(movie.Error.GetErrorResponse().StatusMessage);
            }

            var ret = new DetailedTMDB
            {
                ArtworkUrl = TMDB.Utils.GetFullImagePath(movie.Data.PosterPath, true),
                BackdropUrl = TMDB.Utils.GetFullImagePath(movie.Data.BackdropPath, true),
                Description = movie.Data.Overview,
                MediaType = TMDB_MediaTypes.Movie,
                Rated = MapRatings(movie.Data.Releases),
                Title = movie.Data.Title,
                TMDB_ID = movie.Data.Id
            };

            if (movie.Data.ReleaseDate.HasValue)
            {
                ret.Year = movie.Data.ReleaseDate.Value.Year;
            }
            else
            {
                movie.Data.Releases.Countries.Sort((x, y) => -x.ReleaseDate.CompareTo(y.ReleaseDate));
                foreach (var release in movie.Data.Releases.Countries.Where(item => item.Name == "US"))
                {
                    ret.Year = release.ReleaseDate.Year;
                    break;
                }
            }

            if (movie.Data.Genres != null)
                ret.Genres = string.Join(",", movie.Data.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(movie.Data.Credits, ret);

            ret.Available = (await DB.MoviesSearchableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Where(item => item.TMDB_Id == id)
                .OrderBy(item => item.SortTitle)
                .ToListAsync())
                .Select(item => item.ToBasicMedia()).ToList();


            ret.RequestPermission = await CalculateTitleRequestPermissions();


            return ret;
        }



        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DetailedTMDB>> GetSeries(int id)
        {
            if (!(UserProfile.IsMain || UserProfile.TitleRequestPermission != TitleRequestPermissions.Disabled))
                return CommonResponses.Forbid;

            var series = await _client.GetSeriesAsync(id);
            if (!series.Success)
            {
                if (series.Error.InnerException is System.Net.Http.HttpRequestException httpEx)
                    if (httpEx.StatusCode == HttpStatusCode.NotFound)
                        return NotFound(series.Error.GetErrorResponse().StatusMessage);
                return BadRequest(series.Error.GetErrorResponse().StatusMessage);
            }

            // Response
            var ret = new DetailedTMDB
            {
                ArtworkUrl = TMDB.Utils.GetFullImagePath(series.Data.PosterPath, true),
                BackdropUrl = TMDB.Utils.GetFullImagePath(series.Data.BackdropPath, true),
                Description = series.Data.Overview,
                MediaType = TMDB_MediaTypes.Movie,
                Rated = MapRatings(series.Data.ContentRatings),
                Title = series.Data.Title,
                TMDB_ID = series.Data.Id
            };


            if (series.Data.FirstAirDate.HasValue)
                ret.Year = series.Data.FirstAirDate.Value.Year;

            if (series.Data.Genres != null)
                ret.Genres = string.Join(",", series.Data.Genres.Select(item => item.Name)).ToGenres();

            FillCredits(series.Data.Credits, ret);

            ret.Available = (await DB.SeriesSearchableByProfile(UserAccount, UserProfile)
                .AsNoTracking()
                .Where(item => item.TMDB_Id == id)
                .OrderBy(item => item.SortTitle)
                .ToListAsync())
                .Select(item => item.ToBasicMedia()).ToList();

            ret.RequestPermission = await CalculateTitleRequestPermissions();

            return ret;
        }


        /// <summary>
        /// Level 2
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<SimpleValue<TitleRequestPermissions>>> GetRequestTitlePermission()
        {
            var ret = await CalculateTitleRequestPermissions();
            return new SimpleValue<TitleRequestPermissions>(ret);
        }

        /// <summary>
        /// Level 2
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> RequestTitle(TitleRequest data)
        {
            //Validate
            try { data.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }

            if (data.AccountId == UserAccount.Id)
                return BadRequest("You cannot request a title from yourself");

            if (UserProfile.IsMain)
            {
                if (data.AccountId == null)
                    return BadRequest("You cannot request a title from yourself");

                if (data.AccountId.Value <= 0)
                    return BadRequest($"Invalid {nameof(data.AccountId)}");

                var friends = await DB.Friendships
                    .AsNoTracking()
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .ToListAsync();

                if (friends.Count == 0)
                    return BadRequest("You cannot request a title from yourself");

                bool validFriend = friends.Any(item => item.Account1Id == UserAccount.Id && item.Account2Id == data.AccountId);
                if (!validFriend)
                    validFriend = friends.Any(item => item.Account2Id == UserAccount.Id && item.Account1Id == data.AccountId);

                if (!validFriend)
                    return BadRequest("Friend not found");
            }
            else
            {
                if (UserProfile.TitleRequestPermission == TitleRequestPermissions.Disabled)
                    return BadRequest("You are not authorized to request titles");


                if (data.AccountId == null)
                    data.AccountId = UserAccount.Id;


                if (UserProfile.TitleRequestPermission != TitleRequestPermissions.RequiresAuthorization)
                {
                    if (data.AccountId != UserAccount.Id)
                    {
                        if (data.AccountId.Value <= 0)
                            return BadRequest($"Invalid {nameof(data.AccountId)}");

                        var friends = await DB.Friendships
                            .AsNoTracking()
                            .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                            .ToListAsync();

                        bool validFriend = friends.Any(item => item.Account1Id == UserAccount.Id && item.Account2Id == data.AccountId);
                        if (!validFriend)
                            validFriend = friends.Any(item => item.Account2Id == UserAccount.Id && item.Account1Id == data.AccountId);

                        if (!validFriend)
                            return BadRequest("Friend not found");
                    }
                }
            }



            //Check for existing request
            var existingRequest = await DB.GetRequests
                .AsNoTracking()
                .Include(item => item.Profile)
                .Where(item => item.Profile.AccountId == UserAccount.Id)
                .Where(item => item.TMDB_Id == data.TMDB_Id)
                .Where(item => item.EntryType == data.MediaType)
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                if (existingRequest.ProfileId == UserProfile.Id)
                {
                    return BadRequest("You have already requested this title");
                }
                else
                {
                    //Add get request for this profile, so they are notified when available
                    DB.GetRequests.Add(new Data.Models.GetRequest
                    {
                        AccountId = existingRequest.AccountId,
                        EntryType = existingRequest.EntryType,
                        ParentalStatus = existingRequest.ParentalStatus,
                        ProfileId = UserProfile.Id,
                        Status = existingRequest.Status,
                        Timestamp = DateTime.UtcNow,
                        TMDB_Id = data.TMDB_Id
                    });

                    await DB.SaveChangesAsync();

                    return Ok();
                }
            }

            //Validate TMDB Id
            if (data.MediaType == TMDB_MediaTypes.Movie)
            {
                var response = await _client.GetMovieAsync(data.TMDB_Id);
                if (!response.Success)
                    return BadRequest("Movie not found");
            }
            else
            {
                var response = await _client.GetSeriesAsync(data.TMDB_Id);
                if (!response.Success)
                    return BadRequest("Series not found");
            }


            //Create the request
            DB.GetRequests.Add(new Data.Models.GetRequest
            {
                AccountId = data.AccountId.Value,
                EntryType = data.MediaType,
                ParentalStatus = RequestStatus.Requested,
                ProfileId = UserProfile.Id,
                Status = RequestStatus.Requested,
                Timestamp = DateTime.UtcNow,
                TMDB_Id = data.TMDB_Id
            });

            await DB.SaveChangesAsync();

            return Ok();
        }


        private async Task<TitleRequestPermissions> CalculateTitleRequestPermissions()
        {
            
            if (UserProfile.IsMain)
            {
                var hasFriends = await DB.Friendships
                    .AsNoTracking()
                    .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                    .AnyAsync();

                return hasFriends ? TitleRequestPermissions.Enabled : TitleRequestPermissions.Disabled;
            }
            else
            {
                if(UserProfile.TitleRequestPermission == TitleRequestPermissions.Enabled)
                {
                    var hasFriends = await DB.Friendships
                        .AsNoTracking()
                        .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                        .AnyAsync();

                    if (hasFriends)
                        return TitleRequestPermissions.Enabled;
                    else
                        return TitleRequestPermissions.RequiresAuthorization;
                }

                return UserProfile.TitleRequestPermission;
            }
        }




        private static void FillCredits(Credits credits, DetailedTMDB ret)
        {
            if (credits != null)
            {
                if (credits.Cast != null)
                {
                    foreach (var cast in credits.Cast.OrderBy(item => item.Order).Take(10))
                    {
                        if (ret.Cast == null)
                            ret.Cast = new List<string>();

                        if (!ret.Cast.Contains(cast.Name))
                            ret.Cast.Add(cast.Name);
                    }
                }

                if (credits.Crew != null)
                {
                    foreach (var director in credits.Crew.Where(item => item.Job.ICEquals("Director")).Take(2))
                    {
                        if (ret.Directors == null)
                            ret.Directors = new List<string>();

                        if (!ret.Directors.Contains(director.Name))
                            ret.Directors.Add(director.Name);
                    }

                    foreach (var producer in credits.Crew.Where(item => item.Job.ICEquals("Producer")).Take(2))
                    {
                        if (ret.Producers == null)
                            ret.Producers = new List<string>();

                        if (!ret.Producers.Contains(producer.Name))
                            ret.Producers.Add(producer.Name);
                    }

                    foreach (var writer in credits.Crew.Where(item => item.Job.ICEquals("Screenplay")).Take(2))
                    {
                        if (ret.Writers == null)
                            ret.Writers = new List<string>();
                        if (!ret.Writers.Contains(writer.Name))
                            ret.Writers.Add(writer.Name);
                    }
                }
            }
        }




        private static string MapRatings(Releases releases)
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

        private static string MapRatings(ContentRatings contentRatings)
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

        public static bool TryMapMovieRatings(string country, string rating, out string rated)
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

        public static bool TryMapTVRatings(string country, string rating, out string rated)
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

    }
}
