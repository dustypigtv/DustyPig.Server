using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.REST;
using DustyPig.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Services.TMDB_Service;

/// <summary>
/// Provides a wrapper around Common TMDB api endpoints and objects
/// </summary>
public class TMDBService : TMDB.Client
{
    private const string CONFIG_KEY = "TMDB-API-KEY";

    public const string JOB_DIRECTOR = "Director";
    public const string JOB_PRODUCER = "Producer";
    public const string JOB_EXECUTIVE_PRODUCER = "Executive Producer";
    public const string JOB_WRITER = "Writer";
    public const string JOB_SCREENPLAY = "Screenplay";
    public const string COUNTRY_US = "US";
    public const string LANGUAGE_ENGLISH = "en";

    public static readonly string[] CrewJobs = [JOB_DIRECTOR, JOB_PRODUCER, JOB_EXECUTIVE_PRODUCER, JOB_WRITER, JOB_SCREENPLAY];


    private const string POSTER_WIDTH = "w342";
    private const string BACKDROP_WIDTH = "w780";

    private readonly ILogger<TMDBService> _logger;

    public TMDBService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TMDBService> logger, ILogger<TMDB.Client> clientLogger) : base(httpClientFactory.CreateClient(), clientLogger)
    {
        _logger = logger;
        SetAuth(AuthTypes.APIKey, configuration.GetRequiredValue(CONFIG_KEY));

        AutoThrowIfError = true;

#if DEBUG
        IncludeRawContentInResponse = true;
#endif
    }



    public Task<Response<TMDB.Models.Movies.Details>> GetMovieAsync(int id, CancellationToken cancellationToken = default) =>
        Endpoints.Movies.GetDetailsAsync(id, TMDB.Models.Movies.AppendToResponse.Credits | TMDB.Models.Movies.AppendToResponse.ReleaseDates, cancellationToken: cancellationToken);

    public Task<Response<TMDB.Models.TvSeries.Details>> GetSeriesAsync(int id, CancellationToken cancellationToken = default) =>
        Endpoints.TvSeries.GetDetailsAsync(id, TMDB.Models.TvSeries.AppendToResponse.Credits | TMDB.Models.TvSeries.AppendToResponse.ContentRatings, cancellationToken: cancellationToken);


    public static CreditRoles? GetCreditRole(string job)
    {
        if (string.IsNullOrWhiteSpace(job))
            return null;

        if (job.ICEquals(JOB_DIRECTOR))
            return CreditRoles.Director;

        if (job.ICEquals(JOB_PRODUCER))
            return CreditRoles.Producer;

        if (job.ICEquals(JOB_EXECUTIVE_PRODUCER))
            return CreditRoles.ExecutiveProducer;

        if (job.ICEquals(JOB_WRITER))
            return CreditRoles.Writer;

        if (job.ICEquals(JOB_SCREENPLAY))
            return CreditRoles.Writer;

        return null;
    }


    public static DateOnly? TryGetMovieDate(TMDB.Models.Movies.Details movieDetails)
    {
        if (movieDetails.ReleaseDate != null)
            return movieDetails.ReleaseDate;

        if (movieDetails.ReleaseDates?.Results != null)
        {
            foreach (var resultObject in movieDetails.ReleaseDates.Results.OrderBy(item => !item.CountryCode.ICEquals(COUNTRY_US)))
                if (resultObject.ReleaseDates != null)
                {
                    foreach (var releaseDate in resultObject.ReleaseDates.OrderBy(item => !item.LanguageCode.ICEquals(LANGUAGE_ENGLISH)))
                        if (releaseDate.ReleaseDate != null)
                            return releaseDate.ReleaseDate;
                }
        }

        return null;
    }


    public static string TryMapMovieRatings(TMDB.Models.Movies.Details movieDetails)
    {
        if (movieDetails == null || movieDetails.ReleaseDates == null)
            return null;

        foreach (var resultsObject in movieDetails.ReleaseDates.Results.OrderBy(item => !item.CountryCode.ICEquals(COUNTRY_US)))
        {
            if (resultsObject.ReleaseDates != null)
            {
                var releaseDatesObjectsWithCertification = resultsObject.ReleaseDates.Where(item => !string.IsNullOrWhiteSpace(item.Certification));
                foreach (var releaseDateObject in releaseDatesObjectsWithCertification.Where(item => item.LanguageCode.ICEquals(LANGUAGE_ENGLISH)))
                    if (TryMapMovieRatings(resultsObject.CountryCode, releaseDateObject.Certification, out string rated))
                        return rated;

                foreach (var releaseDateObject in releaseDatesObjectsWithCertification.Where(item => !item.LanguageCode.ICEquals(LANGUAGE_ENGLISH)))
                    if (TryMapTVRatings(resultsObject.CountryCode, releaseDateObject.Certification, out string rated))
                        return rated;
            }
        }

        return null;
    }

    private static bool TryMapMovieRatings(string country, string rating, out string rated)
    {
        rated = null;

        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(rating))
            return false;

        rated = RatingsUtils.MapMovieRatings(country, rating);
        if (!string.IsNullOrWhiteSpace(rated))
        {
            if (rated.EndsWith(" *"))
                rated = rated[..^2];
            return true;
        }

        return false;
    }





    public static string TryMapTVRatings(TMDB.Models.TvSeries.Details details)
    {
        if (details == null || details.ContentRatings == null || details.ContentRatings.Results == null)
            return null;

        foreach (var contentRating in details.ContentRatings.Results.OrderBy(item => !item.CountryCode.ICEquals(COUNTRY_US)))
        {
            if (TryMapTVRatings(contentRating.CountryCode, contentRating.Rating, out string ret))
                return ret;

            if (TryMapMovieRatings(contentRating.CountryCode, contentRating.Rating, out ret))
                return ret;
        }

        return null;
    }

    private static bool TryMapTVRatings(string country, string rating, out string rated)
    {
        rated = null;

        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(rating))
            return false;

        rated = RatingsUtils.MapTVRatings(country, rating);
        if (!string.IsNullOrWhiteSpace(rated))
        {
            if (rated.EndsWith(" *"))
                rated = rated[..^2];
            return true;
        }

        return false;
    }



    public static CreditsDTO GetCommonCredits(TMDB.Models.Movies.Details details) => CreditsDTO.FromAPI(details.Credits);

    public static CreditsDTO GetCommonCredits(TMDB.Models.TvSeries.Details details) => CreditsDTO.FromAPI(details.Credits);


    public static string GetPosterPath(string path) => TMDB.Utils.GetFullImageUrl(path, POSTER_WIDTH);

    public static string GetBackdropPath(string path) => TMDB.Utils.GetFullImageUrl(path, BACKDROP_WIDTH);


    public static string GetAvatarPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Constants.DEFAULT_PROFILE_IMAGE_GREY;
        return GetPosterPath(path);
    }


    public static DateTime? ConvertToDateTime(DateOnly? dt)
    {
        if (dt.HasValue)
            return DateTime.Parse(dt.Value.ToString("yyyy-MM-dd"));
        return null;
    }
}