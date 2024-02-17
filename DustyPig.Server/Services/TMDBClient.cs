using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DustyPig.Server.Services
{
    /// <summary>
    /// Provides a wrapper around Common TMDB api endpoints and objects
    /// </summary>
    public class TMDBClient : TMDB.Client
    {
        private static string _apiKey;

        public static void Configure(string apiKey) => _apiKey = apiKey;

        public TMDBClient() : base(AuthTypes.APIKey, _apiKey) { }


        public Task<REST.Response<TMDB.Models.Movie.DetailsResponse>> GetMovieAsync(int id, CancellationToken cancellationToken = default) =>
            Endpoints.Movie.GetDetailsAsync(id, TMDB.Models.Movie.AppendToResponse.Credits | TMDB.Models.Movie.AppendToResponse.ReleaseDates, cancellationToken:  cancellationToken);

        public Task<REST.Response<TMDB.Models.TvSeries.DetailsResponse>> GetSeriesAsync(int id, CancellationToken cancellationToken = default) =>
            Endpoints.TvSeries.GetDetailsAsync(id, TMDB.Models.TvSeries.AppendToResponse.Credits | TMDB.Models.TvSeries.AppendToResponse.ContentRatings, cancellationToken: cancellationToken);

        


        public static DateOnly? TryGetMovieDate(TMDB.Models.Movie.DetailsResponse response)
        {
            if (response.ReleaseDate != null)
                return response.ReleaseDate;

            if (response.ReleaseDates?.Results != null)
            {
                foreach (var resultObject in response.ReleaseDates.Results.Where(item => item.CountryCode.ICEquals("US")))
                    if (resultObject.ReleaseDates != null)
                    {
                        foreach (var releaseDate in resultObject.ReleaseDates.Where(item => item.LanguageCode.ICEquals("en")))
                            if (releaseDate.ReleaseDate != null)
                                return releaseDate.ReleaseDate;

                        foreach (var releaseDate in resultObject.ReleaseDates.Where(item => !item.LanguageCode.ICEquals("en")))
                            if (releaseDate.ReleaseDate != null)
                                return releaseDate.ReleaseDate;
                    }

                foreach (var resultObject in response.ReleaseDates.Results.Where(item => !item.CountryCode.ICEquals("US")))
                    if (resultObject.ReleaseDates != null)
                    {
                        foreach (var releaseDate in resultObject.ReleaseDates.Where(item => item.LanguageCode.ICEquals("en")))
                            if (releaseDate.ReleaseDate != null)
                                return releaseDate.ReleaseDate;

                        foreach (var releaseDate in resultObject.ReleaseDates.Where(item => !item.LanguageCode.ICEquals("en")))
                            if (releaseDate.ReleaseDate != null)
                                return releaseDate.ReleaseDate;
                    }
            }

            return null;
        }


        public static string TryMapMovieRatings(TMDB.Models.Movie.DetailsResponse.AppendReleaseDatesObject appendReleaseDatesObject)
        {
            if (appendReleaseDatesObject == null || appendReleaseDatesObject.Results == null)
                return null;

            foreach (var resultsObject in appendReleaseDatesObject.Results.Where(item => item.CountryCode.ICEquals("US")))
            {
                if (resultsObject.ReleaseDates != null)
                {
                    var releaseDatesObjectsWithCertification = resultsObject.ReleaseDates.Where(item => !string.IsNullOrWhiteSpace(item.Certification));
                    foreach (var releaseDateObject in releaseDatesObjectsWithCertification.Where(item => item.LanguageCode.ICEquals("en")))
                        if (TryMapMovieRatings(resultsObject.CountryCode, releaseDateObject.Certification, out string rated))
                            return rated;

                    foreach (var releaseDateObject in releaseDatesObjectsWithCertification.Where(item => !item.LanguageCode.ICEquals("en")))
                        if (TryMapMovieRatings(resultsObject.CountryCode, releaseDateObject.Certification, out string rated))
                            return rated;
                }
            }


            foreach (var resultsObject in appendReleaseDatesObject.Results.Where(item => !item.CountryCode.ICEquals("US")))
            {
                if (resultsObject.ReleaseDates != null)
                {
                    var releaseDatesObjectsWithCertification = resultsObject.ReleaseDates.Where(item => !string.IsNullOrWhiteSpace(item.Certification));
                    foreach (var releaseDateObject in releaseDatesObjectsWithCertification.Where(item => item.LanguageCode.ICEquals("en")))
                        if (TryMapMovieRatings(resultsObject.CountryCode, releaseDateObject.Certification, out string rated))
                            return rated;

                    foreach (var releaseDateObject in releaseDatesObjectsWithCertification.Where(item => !item.LanguageCode.ICEquals("en")))
                        if (TryMapMovieRatings(resultsObject.CountryCode, releaseDateObject.Certification, out string rated))
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





        public static string TryMapTVRatings(TMDB.Models.TvSeries.DetailsResponse.AppendContentRatingsObject contentRatings)
        {
            if (contentRatings == null)
                return null;

            if (contentRatings.Results == null)
                return null;

            foreach (var contentRating in contentRatings.Results.Where(item => item.CountryCode.ICEquals("US")))
                if (TryMapTVRatings(contentRating.CountryCode, contentRating.Rating, out string ret))
                    return ret;

            foreach (var contentRating in contentRatings.Results.Where(item => !item.CountryCode.ICEquals("US")))
                if (TryMapMovieRatings(contentRating.CountryCode, contentRating.Rating, out string ret))
                    return ret;

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



        public static CreditsDTO GetCommonCredits(TMDB.Models.Movie.DetailsResponse.AppendCreditsObject credits) => CreditsDTO.FromAPI(credits);

        public static CreditsDTO GetCommonCredits(TMDB.Models.TvSeries.DetailsResponse.AppendCreditsObject credits) => CreditsDTO.FromAPI(credits);


        public static string GetPosterPath(string path) => TMDB.Utils.GetFullImageUrl(path, "w185");

        public static string GetBackdropPath(string path) => TMDB.Utils.GetFullImageUrl(path, "w300");



        public static DateTime? ConvertToDateTime(DateOnly? dt)
        {
            if (dt.HasValue)
                return DateTime.Parse(dt.Value.ToString("yyyy-MM-dd"));
            return null;
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


        public class CrewDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string FullImagePath { get; set; }
            public string Job { get; set; }

            public static CrewDTO FromAPI(TMDB.Models.Movie.DetailsResponse.AppendCreditsObject.CrewObject crewObject)
            {
                return new CrewDTO
                {
                    Id = crewObject.Id,
                    Job = crewObject.Job,
                    Name = crewObject.Name,
                    FullImagePath = TMDB.Utils.GetFullImageUrl(crewObject.ProfilePath, "w185")
                };
            }

            public static CrewDTO FromAPI(TMDB.Models.TvSeries.DetailsResponse.AppendCreditsObject.CrewObject crewObject)
            {
                return new CrewDTO
                {
                    Id = crewObject.Id,
                    Job = crewObject.Job,
                    Name = crewObject.Name,
                    FullImagePath = TMDB.Utils.GetFullImageUrl(crewObject.ProfilePath, "w185")
                };
            }
        }

        public class CastDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string FullImagePath { get; set; }
            public int Order { get; set; }

            public static CastDTO FromAPI(TMDB.Models.Movie.DetailsResponse.AppendCreditsObject.CastObject castObject)
            {
                return new CastDTO
                {
                    Id = castObject.Id,
                    Name = castObject.Name,
                    Order = castObject.Order,
                    FullImagePath = TMDB.Utils.GetFullImageUrl(castObject.ProfilePath, "w185")
                };
            }

            public static CastDTO FromAPI(TMDB.Models.TvSeries.DetailsResponse.AppendCreditsObject.CastObject castObject)
            {
                return new CastDTO
                {
                    Id = castObject.Id,
                    Name = castObject.Name,
                    Order = castObject.Order,
                    FullImagePath = TMDB.Utils.GetFullImageUrl(castObject.ProfilePath, "w185")
                };
            }
        }

        public class CreditsDTO
        {
            public List<CrewDTO> CrewMembers { get; } = [];
            public List<CastDTO> CastMembers { get; } = [];

            public static CreditsDTO FromAPI(TMDB.Models.Movie.DetailsResponse.AppendCreditsObject credits)
            {
                var ret = new CreditsDTO();
                if (credits != null)
                {
                    if (credits.Crew != null)
                        ret.CrewMembers.AddRange(credits.Crew.Select(item => CrewDTO.FromAPI(item)));

                    if (credits.Cast != null)
                        ret.CastMembers.AddRange(credits.Cast.Select(item => CastDTO.FromAPI(item)));
                }
                return ret;
            }

            public static CreditsDTO FromAPI(TMDB.Models.TvSeries.DetailsResponse.AppendCreditsObject credits)
            {
                var ret = new CreditsDTO();
                if (credits != null)
                {
                    if (credits.Crew != null)
                        ret.CrewMembers.AddRange(credits.Crew.Select(item => CrewDTO.FromAPI(item)));

                    if (credits.Cast != null)
                        ret.CastMembers.AddRange(credits.Cast.Select(item => CastDTO.FromAPI(item)));
                }
                return ret;
            }
        }


    }
}
