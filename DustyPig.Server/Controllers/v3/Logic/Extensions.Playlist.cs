using DustyPig.API.v3.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicMedia ToBasicMedia(this Data.Models.Playlist self)
        {
            var ret = new BasicMedia
            {
                Id = self.Id,
                MediaType = MediaTypes.Playlist,
                Title = self.Name,
                ArtworkUrl = self.ArtworkUrl,
                BackdropUrl = self.BackdropUrl,
            };


            return ret;
        }
    }
}
