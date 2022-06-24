using DustyPig.API.v3.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicTMDB ToBasicTMDBInfo(this TMDB.Models.SearchResult @this) => new BasicTMDB
        {
            TMDB_ID = @this.Id,
            ArtworkUrl = TMDB.Utils.GetFullImagePath(@this.PosterPath, false),
            MediaType = @this.IsMovie ? TMDB_MediaTypes.Movie : TMDB_MediaTypes.Series,
            Title = @this.Title
        };
    }
}
