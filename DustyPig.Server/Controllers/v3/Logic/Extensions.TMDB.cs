using DustyPig.API.v3.Models;
using System;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicTMDB ToBasicTMDBInfo(this TMDB.Models.SearchResult self) => new BasicTMDB
        {
            TMDB_ID = self.Id,
            ArtworkUrl = TMDB.Utils.GetFullPosterPath(self.PosterPath, false),
            BackdropUrl = TMDB.Utils.GetFullBackdropPath(self.BackdropPath, false),
            MediaType = self.SearchResultType == TMDB.Models.SearchResultTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
            Title = self.Title
        };

        public static API.v3.Models.Person ToTMDBPerson(this TMDB.Models.SearchResult self) => new Person
        {
            TMDB_Id = self.Id,
            Name = self.Title, //For TMDB.Models.SearchResultTypes.Person this is the name
            Initials = self.Title.GetInitials(),
            AvatarUrl = TMDB.Utils.GetFullBackdropPath(self.ProfilePath, false)
        };
    }
}
