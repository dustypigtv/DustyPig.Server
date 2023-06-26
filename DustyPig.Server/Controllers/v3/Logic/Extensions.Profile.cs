using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicProfile ToBasicProfileInfo(this Profile self) => new BasicProfile
        {
            Id = self.Id,
            Name = self.Name,
            AvatarUrl = self.AvatarUrl,
            HasPin = self.PinNumber != null && self.PinNumber >= 1000,
            IsMain = self.IsMain
        };
    }
}
