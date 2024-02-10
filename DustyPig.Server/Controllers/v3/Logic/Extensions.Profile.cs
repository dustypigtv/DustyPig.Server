using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using System;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicProfile ToBasicProfileInfo(this Profile self) => new BasicProfile
        {
            Id = self.Id,
            Name = self.Name,
            Initials = self.Name.GetInitials(),
            AvatarUrl = self.AvatarUrl,
            HasPin = self.PinNumber != null && self.PinNumber >= 1000,
            IsMain = self.IsMain
        };
    }
}
