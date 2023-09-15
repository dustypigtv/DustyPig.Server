using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static IOrderedQueryable<MediaEntry> ApplySortOrder(this IQueryable<MediaEntry> self, SortOrder sortOrder)
        {
            //Popularity DESC is the default

            return sortOrder switch
            {
                SortOrder.Added => self.OrderBy(item => item.Added),
                SortOrder.Added_Descending => self.OrderByDescending(item => item.Added),
                SortOrder.Alphabetical => self.OrderBy(item => item.SortTitle),
                SortOrder.Alphabetical_Descending => self.OrderByDescending(item => item.SortTitle),
                SortOrder.Popularity => self.OrderBy(item => item.Popularity),
                //SortOrder.Popularity_Descending => q.OrderByDescending(item => item.Popularity),
                SortOrder.Released => self.OrderBy(item => item.Date),
                SortOrder.Released_Descending => self.OrderByDescending(item => item.Date),
                _ => self.OrderByDescending(item => item.Popularity)
            };
        }


        public static BasicMedia ToBasicMedia(this MediaEntry self)
        {
            var ret = new BasicMedia
            {
                Id = self.Id,
                ArtworkUrl = self.ArtworkUrl,
                BackdropUrl = self.BackdropUrl,
                MediaType = self.EntryType,
                Title = self.Title
            };

            if (ret.MediaType == MediaTypes.Movie && self.Date.HasValue)
                ret.Title += $" ({self.Date.Value.Year})";

            return ret;
        }

        public static ExternalSubtitle ToExternalSubtitle(this Subtitle self) =>
            new ExternalSubtitle
            {
                Name = self.Name,
                Url = self.Url
            };


        public static List<ExternalSubtitle> ToExternalSubtitleList(this List<Subtitle> self)
        {
            if (self == null)
                return null;

            if (self.Count == 0)
                return null;

            self.Sort();
            var ret = new List<ExternalSubtitle>();
            foreach (var item in self)
                ret.Add(item.ToExternalSubtitle());
            return ret;
        }


        public static DetailedMovie ToDetailedMovie(this MediaEntry self, bool playable)
        {

            //Build the response
            var ret = new DetailedMovie
            {
                ArtworkUrl = self.ArtworkUrl,
                BackdropUrl = self.BackdropUrl,
                BifUrl = playable ? self.BifUrl : null,
                Cast = self.GetPeople(Roles.Cast),
                CreditsStartTime = self.CreditsStartTime,
                Date = self.Date.Value,
                Description = self.Description,
                Directors = self.GetPeople(Roles.Director),
                Genres = self.ToGenres(),
                Id = self.Id,
                IntroEndTime = self.IntroEndTime,
                IntroStartTime = self.IntroStartTime,
                Length = self.Length.Value,
                LibraryId = self.LibraryId,
                CanPlay = playable,
                Producers = self.GetPeople(Roles.Producer),
                Rated = self.MovieRating?.ToRatings() ?? Ratings.None,
                Title = self.Title,
                TMDB_Id = self.TMDB_Id,
                VideoUrl = playable ? self.VideoUrl : null,
                Writers = self.GetPeople(Roles.Writer)
            };

            //Subs
            if (playable)
                ret.ExternalSubtitles = self.Subtitles.ToExternalSubtitleList();

            return ret;
        }

        public static List<string> GetPeople(this MediaEntry self, Roles role)
        {
            if (self?.People == null)
                return null;

            return self.People
                .Where(item => item.Role == role)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Person.Name)
                .Select(item => item.Person.Name)
                .ToList();
        }
    }
}
