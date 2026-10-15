namespace DustyPig.Server.Services.TMDB_Service;

public class CastDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FullImagePath { get; set; }
    public int Order { get; set; }

    public static CastDTO FromAPI(TMDB.Models.Common.Cast castObject)
    {
        return new CastDTO
        {
            Id = castObject.Id,
            Name = castObject.Name,
            Order = castObject.Order,
            FullImagePath = TMDBService.GetPosterPath(castObject.ProfilePath)
        };
    }
}
