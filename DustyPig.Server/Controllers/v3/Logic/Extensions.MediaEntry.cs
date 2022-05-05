using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicMedia ToBasicMedia(this MediaEntry @this)
        {
            var ret = new BasicMedia
            {
                Id = @this.Id,
                ArtworkUrl = @this.ArtworkUrl,
                MediaType = @this.EntryType,
                Title = @this.Title
            };

            if (ret.MediaType == MediaTypes.Movie && @this.Date.HasValue)
                ret.Title += $" ({@this.Date.Value.Year})";

            return ret;
        }

        public static ExternalSubtitle ToExternalSubtitle(this Subtitle @this) =>
            new ExternalSubtitle
            {
                Name = @this.Name,
                Url = @this.Url
            };


        public static List<ExternalSubtitle> ToExternalSubtitleList(this List<Subtitle> @this)
        {
            if (@this == null)
                return null;

            if (@this.Count == 0)
                return null;

            @this.Sort();
            var ret = new List<ExternalSubtitle>();
            foreach (var item in @this)
                ret.Add(item.ToExternalSubtitle());
            return ret;
        }


        public static DetailedMovie ToDetailedMovie(this MediaEntry @this, bool playable)
        {

            //Build the response
            var ret = new DetailedMovie
            {
                ArtworkUrl = @this.ArtworkUrl,
                BifUrl = playable ? @this.BifUrl : null,
                Cast = @this.GetPeople(Roles.Cast),
                CreditsStartTime = @this.CreditsStartTime,
                Date = @this.Date.Value,
                Description = @this.Description,
                Directors = @this.GetPeople(Roles.Director),
                Genres = @this.Genres.Value,
                Id = @this.Id,
                IntroEndTime = @this.IntroEndTime,
                IntroStartTime = @this.IntroStartTime,
                Length = @this.Length.Value,
                LibraryId = @this.LibraryId,
                Producers = @this.GetPeople(Roles.Producer),
                Rated = (@this.Rated ?? Ratings.None),
                Title = @this.Title + $" ({@this.Date.Value.Year})",
                TMDB_Id = @this.TMDB_Id,
                VideoUrl = playable ? @this.VideoUrl : null,
                Writers = @this.GetPeople(Roles.Writer)
            };

            //Subs
            if (playable)
                ret.ExternalSubtitles = @this.Subtitles.ToExternalSubtitleList();

            return ret;
        }

        public static List<string> GetPeople(this MediaEntry @this, Roles role)
        {
            if (@this?.People == null)
                return null;

            return @this.People
                .Where(item => item.Role == role)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Person.Name)
                .Select(item => item.Person.Name)
                .ToList();
        }
    }
}
