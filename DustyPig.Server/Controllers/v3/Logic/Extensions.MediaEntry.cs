using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using APIPerson = DustyPig.API.v3.Models.BasicPerson;

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
                SortOrder.Released => self.OrderBy(item => item.Date),
                SortOrder.Released_Descending => self.OrderByDescending(item => item.Date),
                _ => self.OrderByDescending(item => item.Popularity)
            };
        }


        public static BasicMedia ToBasicMedia(this MediaEntry self, bool includeDescription = false)
        {
            var ret = new BasicMedia
            {
                Id = self.Id,
                ArtworkUrl = self.ArtworkUrl,
                BackdropUrl = self.BackdropUrl,
                MediaType = self.EntryType,
                Title = self.FormattedTitle()
            };

            if (includeDescription)
                ret.Description = self.Description;

            return ret;
        }


        public static DetailedMovie ToDetailedMovie(this MediaEntry self, bool playable)
        {
            //Build the response
            return new DetailedMovie
            {
                Added = self.Added,
                ArtworkUrl = self.ArtworkUrl,
                BackdropUrl = self.BackdropUrl,
                BifUrl = playable ? self.BifUrl : null,
                Credits = self.GetPeople(),
                CreditsStartTime = self.CreditsStartTime,
                Date = self.Date.Value,
                Description = self.Description,
                Genres = self.GetGenreFlags(),
                Id = self.Id,
                IntroEndTime = self.IntroEndTime,
                IntroStartTime = self.IntroStartTime,
                Length = self.Length.Value,
                LibraryId = self.LibraryId,
                CanPlay = playable,
                Rated = self.MovieRating ?? MovieRatings.None,
                Title = self.Title,
                TMDB_Id = self.TMDB_Id,
                VideoUrl = playable ? self.VideoUrl : null,
                ExtraSearchTerms = (self.ExtraSearchTerms ?? []).Select(item => item.Term).Order().ToList()
            };
        }


        public static DetailedSeries ToAdminDetailedSeries(this MediaEntry self)
        {
            return new DetailedSeries
            {
                Added = self.Added,
                ArtworkUrl = self.ArtworkUrl,
                BackdropUrl = self.BackdropUrl,
                Credits = self.GetPeople(),
                Description = self.Description,
                Genres = self.GetGenreFlags(),
                Id = self.Id,
                LibraryId = self.LibraryId,
                Rated = self.TVRating ?? TVRatings.None,
                Title = self.Title,
                TMDB_Id = self.TMDB_Id,
                ExtraSearchTerms = (self.ExtraSearchTerms ?? []).Select(_ => _.Term).Order().ToList(),
                CanManage = true
            };
        }

        public static DetailedEpisode ToAdminDetailedEpisode(this MediaEntry self)
        {
            var ret = new DetailedEpisode
            {
                Added = self.Added,
                ArtworkUrl = self.ArtworkUrl,
                BifUrl = self.BifUrl,
                CreditsStartTime = self.CreditsStartTime,
                Date = self.Date.Value,
                Description = self.Description,
                EpisodeNumber = (ushort)self.Episode.Value,
                Id = self.Id,
                IntroEndTime = self.IntroEndTime,
                IntroStartTime = self.IntroStartTime,
                Length = self.Length.Value,
                SeasonNumber = (ushort)self.Season.Value,
                SeriesId = self.LinkedToId.Value,
                Title = self.Title,
                TMDB_Id = self.TMDB_Id,
                VideoUrl = self.VideoUrl,
            };

            return ret;
        }

        public static List<DetailedEpisode> ToAdminDetailedEpisodeList(this IEnumerable<MediaEntry> self)
        {
            var ret = new List<DetailedEpisode>();

            foreach (var ep in self)
                ret.Add(ep.ToAdminDetailedEpisode());

            return ret;
        }



        public static List<APIPerson> GetPeople(this MediaEntry self)
        {
            if (self?.TMDB_Entry?.People == null)
                return null;

            if (self.TMDB_Entry.People.Count == 0)
                return null;

            List<APIPerson> ret = null;

            foreach (CreditRoles role in Enum.GetValues(typeof(CreditRoles)))
            {
                var bridges = self.TMDB_Entry.People
                    .Where(item => item.Role == role)
                    .OrderBy(item => item.SortOrder)
                    .ToList();

                foreach (var bridge in bridges)
                {
                    ret ??= [];

                    string avatarUrl = bridge.TMDB_Person.AvatarUrl;
                    if (string.IsNullOrWhiteSpace(avatarUrl))
                        avatarUrl = Constants.DEFAULT_PROFILE_IMAGE_GREY;
                    ret.Add(new APIPerson
                    {
                        AvatarUrl = avatarUrl,
                        Initials = bridge.TMDB_Person.Name.GetInitials(),
                        Name = bridge.TMDB_Person.Name,
                        Order = bridge.SortOrder,
                        Role = role,
                        TMDB_Id = bridge.TMDB_PersonId
                    });
                }
            }

            return ret;
        }


        public static void SortSearchResults(this List<MediaEntry> self, string normQuery)
        {
            var qt = new Dictionary<int, string>();
            foreach (var me in self)
                qt.Add(me.Id, me.Title.NormalizedQueryString());

            self.Sort((x, y) =>
            {
                int ret = -qt[x.Id].ICEquals(normQuery).CompareTo(qt[y.Id].ICEquals(normQuery));
         
                if (ret == 0 && qt[x.Id].ICEquals(qt[y.Id]))
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                
                if (ret == 0)
                    ret = -qt[x.Id].ICStartsWith(normQuery).CompareTo(qt[y.Id].ICStartsWith(normQuery));
                
                if (ret == 0)
                    ret = -qt[x.Id].ICContains(normQuery).CompareTo(qt[y.Id].ICContains(normQuery));
                
                if (ret == 0)
                    ret = x.SortTitle.CompareTo(y.SortTitle);
                
                if (ret == 0)
                    ret = (x.Popularity ?? 0).CompareTo(y.Popularity ?? 0);
                
                return ret;
            });
        }
    }
}
