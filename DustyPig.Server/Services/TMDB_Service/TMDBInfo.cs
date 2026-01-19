using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Services.TMDB_Service;

internal class TMDBInfo
{
    /// <summary>
    /// DB Id, not TMDB_ID
    /// </summary>
    public int Id { get; set; }
    public double Popularity { get; set; }
    public string BackdropUrl { get; set; }
    public MovieRatings? MovieRating { get; set; }
    public TVRatings? TVRating { get; set; }
    public string Overview { get; set; }
    public bool Changed { get; set; }

    public static TMDBInfo FromEntry(TMDB_Entry entry, bool changed = false)
    {
        return new TMDBInfo
        {
            Id = entry.Id,
            BackdropUrl = entry.BackdropUrl,
            MovieRating = entry.MovieRating,
            Overview = entry.Description,
            Popularity = entry.Popularity,
            TVRating = entry.TVRating,
            Changed = changed
        };
    }
}
