using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicMedia ToBasicMedia(this MediaEntry @this)
        {
            var ret = new BasicMedia
            {
                Id = @this.Id,
                ArtworkUrl = @this.ArtworkUrl,
                MediaType = @this.EntryType,
                Title = @this.Title
            };

            if (ret.MediaType == MediaTypes.Movie && @this.Date.HasValue)
                ret.Title += $" ({@this.Date.Value.Year})";

            return ret;
        }

       public static List<string> GetPeople(this MediaEntry @this, Roles role)
        {
            if (@this?.People == null)
                return null;

            return @this.People
                .Where(item => item.Role == role)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Person.Name)
                .Select(item => item.Person.Name)
                .ToList();
        }
    }
}
