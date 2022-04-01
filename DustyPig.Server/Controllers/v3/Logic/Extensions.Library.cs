using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicLibrary ToBasicLibraryInfo(this Library @this) => new BasicLibrary
        {
            Id = @this.Id,
            IsTV = @this.IsTV,
            Name = @this.Name
        };

        /// <summary>
        /// Make sure Library was loaded with:
        /// 
        ///     .Include(item => item.Account)
        ///     .ThenInclude(item => item.Profiles)
        ///     
        ///     .Include(item => item.FriendLibraryShares)
        ///     .ThenInclude(item => item.FriendShip)
        ///     .ThenInclude(item => item.Account1)
        ///     .ThenInclude(item => item.Profiles)
        ///     
        ///     .Include(item => item.FriendLibraryShares)
        ///     .ThenInclude(item => item.FriendShip)
        ///     .ThenInclude(item => item.Account2)
        ///     .ThenInclude(item => item.Profiles)
        /// </summary>
        public static DetailedLibrary ToDetailedLibraryInfo(this Library @this)
        {
            //This acct owns the lib
            var ret = new DetailedLibrary
            {
                Id = @this.Id,
                IsTV = @this.IsTV,
                Name = @this.Name
            };

            foreach (var share in @this.ProfileLibraryShares)
                if (@this.Account.Profiles.Select(item => item.Id).Contains(share.ProfileId))
                    ret.Profiles.Add(share.Profile.ToBasicProfileInfo());

            foreach (var friendship in @this.FriendLibraryShares.Select(item => item.Friendship))
                ret.SharedWith.Add(friendship.ToBasicFriendInfo(@this.AccountId));

            return ret;
        }


    }
}
