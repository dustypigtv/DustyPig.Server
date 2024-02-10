using DustyPig.API.v3.Models;

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
    }
}
