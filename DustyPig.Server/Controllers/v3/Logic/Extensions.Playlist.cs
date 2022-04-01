using DustyPig.API.v3.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicMedia ToBasicMedia(this Data.Models.Playlist @this)
        {
            var ret = new BasicMedia
            {
                Id = @this.Id,
                ArtworkUrl = @this.ArtworkUrl,
                MediaType = MediaTypes.Playlist,
                Title = @this.Name
            };

            return ret;
        }


    }
}
