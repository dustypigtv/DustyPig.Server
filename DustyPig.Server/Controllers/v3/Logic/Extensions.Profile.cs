using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicProfile ToBasicProfileInfo(this Profile @this) => new BasicProfile
        {
            Id = @this.Id,
            Name = @this.Name,
            AvatarUrl = @this.AvatarUrl,
            HasPin = @this.PinNumber != null && @this.PinNumber >= 1000,
            IsMain = @this.IsMain
        };
    }
}
