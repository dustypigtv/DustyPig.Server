using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    static class RatingsConversion
    {
        public static MovieRatings ToMovieRatings(this Ratings self) =>
            self switch
            {
                Ratings.G => MovieRatings.G,
                Ratings.PG => MovieRatings.PG,
                Ratings.PG_13 => MovieRatings.PG_13,
                Ratings.R => MovieRatings.R,
                Ratings.NC_17 => MovieRatings.NC_17,
                Ratings.Unrated => MovieRatings.Unrated,
                _ => MovieRatings.NotRated
            };

        public static MovieRatings ToMovieRatings(this Ratings? self) => (self ?? Ratings.NR).ToMovieRatings();


        public static TVRatings ToTVRatings(this Ratings self) =>
            self switch
            {
                Ratings.TV_Y => TVRatings.Y,
                Ratings.TV_Y7 => TVRatings.Y,
                Ratings.TV_G => TVRatings.G,
                Ratings.TV_PG => TVRatings.PG,
                Ratings.TV_14 => TVRatings.TV_14,
                Ratings.TV_MA => TVRatings.MA,
                Ratings.Unrated => TVRatings.Unrated,
                _ => TVRatings.NotRated
            };

        public static TVRatings ToTVRatings(this Ratings? self) => (self ?? Ratings.NR).ToTVRatings();


        public static Ratings ToRatings(this MovieRatings self) =>
            self switch
            {
                MovieRatings.G => Ratings.G,
                MovieRatings.PG => Ratings.PG,
                MovieRatings.PG_13 => Ratings.PG_13,
                MovieRatings.R => Ratings.R,
                MovieRatings.NC_17 => Ratings.NC_17,
                MovieRatings.Unrated => Ratings.Unrated,
                _ => Ratings.NR
            };

        public static Ratings ToRatings(this MovieRatings? self) => (self ?? MovieRatings.NotRated).ToRatings();

        public static Ratings ToRatings(this TVRatings self) =>
            self switch
            {
                TVRatings.Y => Ratings.TV_Y,
                TVRatings.Y7 => Ratings.TV_Y7,
                TVRatings.G => Ratings.TV_G,
                TVRatings.PG => Ratings.TV_PG,
                TVRatings.TV_14 => Ratings.TV_14,
                TVRatings.MA => Ratings.TV_MA,
                TVRatings.Unrated => Ratings.Unrated,
                _ => Ratings.NR
            };

        public static Ratings ToRatings(this TVRatings? self) => (self ?? TVRatings.NotRated).ToRatings();
    }
}
