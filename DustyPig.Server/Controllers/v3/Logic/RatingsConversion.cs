//using DustyPig.API.v3.MPAA;
//using DustyPig.Server.Data.Models;

//namespace DustyPig.Server.Controllers.v3.Logic
//{
//    static class RatingsConversion
//    {
//       public static MovieRatings ToMovieRatings(this Ratings self)
//        {
//            if(self.HasFlag(Ratings.G)) return MovieRatings.G;
//            if(self.HasFlag(Ratings.PG)) return MovieRatings.PG;
//            if(self.HasFlag(Ratings.PG_13)) return MovieRatings.PG_13;
//            if(self.HasFlag(Ratings.R)) return MovieRatings.R;
//            if(self.HasFlag(Ratings.NC_17)) return MovieRatings.NC_17;
//            if(self.HasFlag(Ratings.Unrated)) return MovieRatings.Unrated;
//            return MovieRatings.Unrated;
//        }

//        public static MovieRatings ToMovieRatings(this Ratings? self) => (self ?? Ratings.NR).ToMovieRatings();


//        public static TVRatings ToTVRatings(this Ratings self)
//        {
//            if (self.HasFlag(Ratings.TV_Y)) return TVRatings.Y;
//            if (self.HasFlag(Ratings.TV_Y7)) return TVRatings.Y7;
//            if (self.HasFlag(Ratings.TV_G)) return TVRatings.G;
//            if (self.HasFlag(Ratings.TV_PG)) return TVRatings.PG;
//            if (self.HasFlag(Ratings.TV_14)) return TVRatings.TV_14;
//            if (self.HasFlag(Ratings.TV_MA)) return TVRatings.MA;
//            return TVRatings.NotRated;
//        }

//        public static TVRatings ToTVRatings(this Ratings? self) => (self ?? Ratings.NR).ToTVRatings();


//        public static Ratings ToRatings(this MovieRatings self) =>
//            self switch
//            {
//                MovieRatings.G => Ratings.G,
//                MovieRatings.PG => Ratings.PG,
//                MovieRatings.PG_13 => Ratings.PG_13,
//                MovieRatings.R => Ratings.R,
//                MovieRatings.NC_17 => Ratings.NC_17,
//                MovieRatings.Unrated => Ratings.Unrated,
//                _ => Ratings.NR
//            };

//        public static Ratings ToRatings(this MovieRatings? self) => (self ?? MovieRatings.Unrated).ToRatings();

//        public static Ratings ToRatings(this TVRatings self) =>
//            self switch
//            {
//                TVRatings.Y => Ratings.TV_Y,
//                TVRatings.Y7 => Ratings.TV_Y7,
//                TVRatings.G => Ratings.TV_G,
//                TVRatings.PG => Ratings.TV_PG,
//                TVRatings.TV_14 => Ratings.TV_14,
//                TVRatings.MA => Ratings.TV_MA,
//                _ => Ratings.NR
//            };

//        public static Ratings ToRatings(this TVRatings? self) => (self ?? TVRatings.NotRated).ToRatings();
//    }
//}
