using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicMedia ToBasicMedia(this Data.Models.Playlist @this)
        {
            var ret = new BasicMedia
            {
                Id = @this.Id,
                MediaType = MediaTypes.Playlist,
                Title = @this.Name,
                ArtworkUrl = @this.ArtworkUrl,
            };

            
            return ret;
        }        
    }
}
