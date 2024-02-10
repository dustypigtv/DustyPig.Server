﻿using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using System.Collections.Generic;
using System.Linq;
using APIPerson = DustyPig.API.v3.Models.Person;

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

        public static SRTSubtitle ToSRTSubtitle(this Subtitle self) =>
            new SRTSubtitle
            {
                Name = self.Name,
                Url = self.Url,
                FileSize = self.FileSize
            };


        public static List<SRTSubtitle> ToSRTSubtitleList(this List<Subtitle> self)
        {
            if (self == null)
                return null;

            if (self.Count == 0)
                return null;

            self.Sort();
            var ret = new List<SRTSubtitle>();
            foreach (var item in self)
                ret.Add(item.ToSRTSubtitle());
            return ret;
        }


        public static DetailedMovie ToDetailedMovie(this MediaEntry self, bool playable)
        {

            //Build the response
            var ret = new DetailedMovie
            {
                ArtworkUrl = self.ArtworkUrl,
                ArtworkSize = self.ArtworkSize,
                BackdropUrl = self.BackdropUrl,
                BackdropSize = self.BackdropSize,
                BifUrl = playable ? self.BifUrl : null,
                BifSize = self.BifSize,
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
                Rated = self.MovieRating ?? MovieRatings.None,
                Title = self.Title,
                TMDB_Id = self.TMDB_Id,
                VideoUrl = playable ? self.VideoUrl : null,
                VideoSize = self.VideoSize,
                Writers = self.GetPeople(Roles.Writer)
            };

            //Subs
            if (playable)
                ret.SRTSubtitles = self.Subtitles.ToSRTSubtitleList();

            return ret;
        }

        public static List<APIPerson> GetPeople(this MediaEntry self, Roles role)
        {
            if (self?.TMDB_Entry?.People == null)
                return null;

            if (self.TMDB_Entry.People.Count == 0)
                return null;

            var lst = self.TMDB_Entry.People
                .Where(item => item.Role == role)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.TMDB_Person.Name)
                .ToList();

            if (lst.Count == 0)
                return null;

            return lst
                .Select(item => new APIPerson
                {
                    TMDB_Id = item.TMDB_PersonId,
                    Name = item.TMDB_Person.Name,
                    AvatarUrl = item.TMDB_Person.AvatarUrl
                })
                .ToList();
        }
    }
}
