using DustyPig.API.v3.Models;
using DustyPig.Server.Services;
using System;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicTMDB ToBasicTMDBInfo(this TMDB.Models.Search.MultiResponse.ResultsObject self) => new BasicTMDB
        {
            TMDB_ID = self.Id,
            ArtworkUrl = TMDBClient.GetPosterPath(self.PosterPath),
            BackdropUrl =TMDBClient.GetBackdropPath(self.BackdropPath),
            MediaType = self.MediaType.ICEquals("movie") ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
            Title = self.MediaType.ICEquals("movie") ? self.Title : self.Name
        };

        public static API.v3.Models.Person ToTMDBPerson(this TMDB.Models.Search.MultiResponse.ResultsObject self) => new Person
        {
            TMDB_Id = self.Id,
            Name = self.Name, 
            Initials = self.Name.GetInitials(),
            AvatarUrl = TMDBClient.GetPosterPath(self.ProfilePath)
        };
    }
}
