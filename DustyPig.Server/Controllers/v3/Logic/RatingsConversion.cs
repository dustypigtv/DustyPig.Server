using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using MySqlX.XDevAPI.Relational;

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
                Ratings _ => MovieRatings.NotRated
            };

        public static TVRatings ToTVRatings(this Ratings self) =>
            self switch
            {
                Ratings.TV_Y => TVRatings.TV_Y,
                Ratings.TV_Y7 => TVRatings.TV_Y,
                Ratings.TV_G => TVRatings.TV_G,
                Ratings.TV_PG => TVRatings.TV_PG,
                Ratings.TV_14 => TVRatings.TV_14,
                Ratings.TV_MA => TVRatings.TV_MA,
                Ratings.Unrated => TVRatings.Unrated,
                Ratings _ => TVRatings.NotRated
            };

        public static Ratings ToRatings(this MovieRatings self) =>
            self switch
            {
                MovieRatings.G => Ratings.G,
                MovieRatings.PG => Ratings.PG,
                MovieRatings.PG_13 => Ratings.PG_13,
                MovieRatings.R => Ratings.R,
                MovieRatings.NC_17 => Ratings.NC_17,
                MovieRatings.Unrated => Ratings.Unrated,
                MovieRatings _ => Ratings.NR
            };

        public static Ratings ToRatings(this TVRatings self) =>
            self switch
            {
                TVRatings.TV_Y => Ratings.TV_Y,
                TVRatings.TV_Y7 => Ratings.TV_Y7,
                TVRatings.TV_G => Ratings.TV_G,
                TVRatings.TV_PG => Ratings.TV_PG,
                TVRatings.TV_14 => Ratings.TV_14,
                TVRatings.TV_MA => Ratings.TV_MA,
                TVRatings.Unrated => Ratings.Unrated,
                TVRatings _ => Ratings.NR
            };


    }
}
