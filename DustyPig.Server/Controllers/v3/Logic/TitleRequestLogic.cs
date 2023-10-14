using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    internal static class TitleRequestLogic
    {
        internal static TitleRequestPermissions GetTitleRequestPermissions(Account account, Profile profile, bool hasFriends)
        {
            if (profile.IsMain)
            {
                if (hasFriends || account.Profiles.Count > 1)
                    return TitleRequestPermissions.Enabled;
                return TitleRequestPermissions.Disabled;
            }
            return profile.TitleRequestPermission;
        }
    }
}
