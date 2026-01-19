using System;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Services.TMDB_Service;

internal class CreditsDTO
{
    public List<CrewDTO> CrewMembers { get; } = [];
    public List<CastDTO> CastMembers { get; } = [];

    public static CreditsDTO FromAPI(TMDB.Models.Movies.Credits credits)
    {
        var ret = new CreditsDTO();
        if (credits != null)
        {
            if (credits.Crew != null)
                ret.CrewMembers.AddRange(
                    credits.Crew
                        .Where(item => !item.Adult)
                        .Where(item => TMDBService.CrewJobs.ICContains(item.Job))
                        .Select(item => CrewDTO.FromAPI(item))
                );

            if (credits.Cast != null)
                ret.CastMembers.AddRange(
                    credits.Cast
                    .Where(item => !item.Adult)
                    .Select(item => CastDTO.FromAPI(item))
                );
        }
        return ret;
    }

    public static CreditsDTO FromAPI(TMDB.Models.TvSeries.Credits credits)
    {
        var ret = new CreditsDTO();
        if (credits != null)
        {
            if (credits.Crew != null)
                ret.CrewMembers.AddRange(
                    credits.Crew
                    .Where(item => !item.Adult)
                    .Where(item => TMDBService.CrewJobs.ICContains(item.Job))
                    .Select(item => CrewDTO.FromAPI(item))
            );

            if (credits.Cast != null)
                ret.CastMembers.AddRange(
                    credits.Cast
                    .Where(item => !item.Adult)
                    .Select(item => CastDTO.FromAPI(item))
                );
        }
        return ret;
    }
}
