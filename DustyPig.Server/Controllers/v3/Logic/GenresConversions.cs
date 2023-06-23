using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    static class GenresConversions
    {
        public static void SetBits(this MediaEntry self, Genres genres)
        {
            long lg = (long)genres;
            self.Genre_Action = (lg & (long)Genres.Action) != 0;
            self.Genre_Adventure = (lg & (long)Genres.Adventure) != 0;
            self.Genre_Animation = (lg & (long)Genres.Animation) != 0;
            self.Genre_Anime = (lg & (long)Genres.Anime) != 0;
            self.Genre_Awards_Show = (lg & (long)Genres.Awards_Show) != 0;
            self.Genre_Children = (lg & (long)Genres.Children) != 0;
            self.Genre_Comedy = (lg & (long)Genres.Comedy) != 0;
            self.Genre_Crime = (lg & (long)Genres.Crime) != 0;
            self.Genre_Documentary = (lg & (long)Genres.Documentary) != 0;
            self.Genre_Drama = (lg & (long)Genres.Drama) != 0;
            self.Genre_Family = (lg & (long)Genres.Family) != 0;
            self.Genre_Fantasy = (lg & (long)Genres.Fantasy) != 0;
            self.Genre_Food = (lg & (long)Genres.Food) != 0;
            self.Genre_Game_Show = (lg & (long)Genres.Game_Show) != 0;
            self.Genre_History = (lg & (long)Genres.History) != 0;
            self.Genre_Home_and_Garden = (lg & (long)Genres.Home_and_Garden) != 0;
            self.Genre_Horror = (lg & (long)Genres.Horror) != 0;
            self.Genre_Indie = (lg & (long)Genres.Indie) != 0;
            self.Genre_Martial_Arts = (lg & (long)Genres.Martial_Arts) != 0;
            self.Genre_Mini_Series = (lg & (long)Genres.Mini_Series) != 0;
            self.Genre_Music = (lg & (long)Genres.Music) != 0;
            self.Genre_Musical = (lg & (long)Genres.Musical) != 0;
            self.Genre_Mystery = (lg & (long)Genres.Mystery) != 0;
            self.Genre_News = (lg & (long)Genres.News) != 0;
            self.Genre_Podcast = (lg & (long)Genres.Podcast) != 0;
            self.Genre_Political = (lg & (long)Genres.Political) != 0;
            self.Genre_Reality = (lg & (long)Genres.Reality) != 0;
            self.Genre_Romance = (lg & (long)Genres.Romance) != 0;
            self.Genre_Science_Fiction = (lg & (long)Genres.Science_Fiction) != 0;
            self.Genre_Soap = (lg & (long)Genres.Soap) != 0;
            self.Genre_Sports = (lg & (long)Genres.Sports) != 0;
            self.Genre_Suspense = (lg & (long)Genres.Suspense) != 0;
            self.Genre_Talk_Show = (lg & (long)Genres.Talk_Show) != 0;
            self.Genre_Thriller = (lg & (long)Genres.Thriller) != 0;
            self.Genre_Travel = (lg & (long)Genres.Travel) != 0;
            self.Genre_TV_Movie = (lg & (long)Genres.TV_Movie) != 0;
            self.Genre_War = (lg & (long)Genres.War) != 0;
            self.Genre_Western = (lg & (long)Genres.Western) != 0;
        }

        public static Genres GetBits(this MediaEntry self)
        {
            var ret = Genres.Unknown;
            if (self.Genre_Action) ret |= Genres.Action;
            if (self.Genre_Adventure) ret |= Genres.Adventure;
            if (self.Genre_Animation) ret |= Genres.Animation;
            if (self.Genre_Anime) ret |= Genres.Anime;
            if (self.Genre_Awards_Show) ret |= Genres.Awards_Show;
            if (self.Genre_Children) ret |= Genres.Children;
            if (self.Genre_Comedy) ret |= Genres.Comedy;
            if (self.Genre_Crime) ret |= Genres.Crime;
            if (self.Genre_Documentary) ret |= Genres.Documentary;
            if (self.Genre_Drama) ret |= Genres.Drama;
            if (self.Genre_Family) ret |= Genres.Family;
            if (self.Genre_Fantasy) ret |= Genres.Fantasy;
            if (self.Genre_Food) ret |= Genres.Food;
            if (self.Genre_Game_Show) ret |= Genres.Game_Show;
            if (self.Genre_History) ret |= Genres.History;
            if (self.Genre_Home_and_Garden) ret |= Genres.Home_and_Garden;
            if (self.Genre_Horror) ret |= Genres.Horror;
            if (self.Genre_Indie) ret |= Genres.Indie;
            if (self.Genre_Martial_Arts) ret |= Genres.Martial_Arts;
            if (self.Genre_Mini_Series) ret |= Genres.Mini_Series;
            if (self.Genre_Music) ret |= Genres.Music;
            if (self.Genre_Musical) ret |= Genres.Musical;
            if (self.Genre_Mystery) ret |= Genres.Mystery;
            if (self.Genre_News) ret |= Genres.News;
            if (self.Genre_Podcast) ret |= Genres.Podcast;
            if (self.Genre_Political) ret |= Genres.Political;
            if (self.Genre_Reality) ret |= Genres.Reality;
            if (self.Genre_Romance) ret |= Genres.Romance;
            if (self.Genre_Science_Fiction) ret |= Genres.Science_Fiction;
            if (self.Genre_Soap) ret |= Genres.Soap;
            if (self.Genre_Sports) ret |= Genres.Sports;
            if (self.Genre_Suspense) ret |= Genres.Suspense;
            if (self.Genre_Talk_Show) ret |= Genres.Talk_Show;
            if (self.Genre_Thriller) ret |= Genres.Thriller;
            if (self.Genre_Travel) ret |= Genres.Travel;
            if (self.Genre_TV_Movie) ret |= Genres.TV_Movie;
            if (self.Genre_War) ret |= Genres.War;
            if (self.Genre_Western) ret |= Genres.Western;

            return ret;
        }
    }
}

