namespace DustyPig.Server.Services.TMDB_Service;

public class CrewDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FullImagePath { get; set; }
    public string Job { get; set; }

    public static CrewDTO FromAPI(TMDB.Models.Common.CommonCrew crewObject)
    {
        return new CrewDTO
        {
            Id = crewObject.Id,
            Job = crewObject.Job,
            Name = crewObject.Name,
            FullImagePath = TMDBService.GetPosterPath(crewObject.ProfilePath)
        };
    }

    public static CrewDTO FromAPI(TMDB.Models.TvSeries.Crew crewObject)
    {
        return new CrewDTO
        {
            Id = crewObject.Id,
            Job = crewObject.Job,
            Name = crewObject.Name,
            FullImagePath = TMDBService.GetPosterPath(crewObject.ProfilePath)
        };
    }
}