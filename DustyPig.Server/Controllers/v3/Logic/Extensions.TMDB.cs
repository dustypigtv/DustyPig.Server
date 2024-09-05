using DustyPig.API.v3.Models;
using DustyPig.Server.Services;
using System;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicTMDB ToBasicTMDBInfo(this TMDB.Models.Search.MultiObject self) => new BasicTMDB
        {
            TMDB_ID = self.Id,
            ArtworkUrl = TMDBClient.GetPosterPath(self.PosterPath),
            BackdropUrl = TMDBClient.GetBackdropPath(self.BackdropPath),
            MediaType = self.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
            Title = self.MediaType == TMDB.Models.Common.CommonMediaTypes.Movie ? self.Title : self.Name
        };

        public static API.v3.Models.BasicPerson ToTMDBPerson(this TMDB.Models.Search.MultiObject self) => new BasicPerson
        {
            TMDB_Id = self.Id,
            Name = self.Name,
            Initials = self.Name.GetInitials(),
            AvatarUrl = TMDBClient.GetAvatarPath(self.ProfilePath)
        };
    }
}
