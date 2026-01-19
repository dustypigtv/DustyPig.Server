using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
using System;
using System.Linq;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        /// <summary>
        /// Make sure Friendship was loaded with:
        /// 
        ///     .Include(item => item.Account1)
        ///     .ThenInclude(item => item.Profiles)
        ///     
        ///     .Include(item => item.Account2)
        ///     .ThenInclude(item => item.Profiles)
        /// </summary>
        public static BasicFriend ToBasicFriendInfo(this Friendship self, int accountId)
        {
            var displayName = self.GetFriendDisplayNameForAccount(accountId);
            return new BasicFriend
            {
                Id = self.Id,
                DisplayName = displayName,
                Initials = displayName.GetInitials(),
                AvatarUrl = self.GetFriendAvatar(accountId),
                Accepted = self.Accepted,
                FriendRequestDirection = self.Account1Id == accountId ? RequestDirection.Sent : RequestDirection.Received
            };
        }



        /// <summary>
        /// Make sure friend was called with:
        ///     .Include(item => item.Account1)
        ///     .ThenInclude(item => item.Profiles)
        ///     .Include(item => item.Account2)
        ///     .ThenInclude(item => item.Profiles)
        /// </summary>
        public static string GetFriendDisplayNameForAccount(this Friendship friend, int accountId)
        {
            string displayName = friend.Account1Id == accountId ? friend.DisplayName2 : friend.DisplayName1;
            string acctName = friend.Account1Id == accountId
                ? friend.Account2.Profiles.Where(item => item.IsMain).First().Name
                : friend.Account1.Profiles.Where(item => item.IsMain).First().Name;

            return Misc.Coalesce(displayName, acctName);
        }

        /// <summary>
        /// Make sure friend was called with:
        ///     .Include(item => item.Account1)
        ///     .ThenInclude(item => item.Profiles)
        ///     .Include(item => item.Account2)
        ///     .ThenInclude(item => item.Profiles)
        /// </summary>
        public static string GetFriendAvatar(this Friendship friend, int accountId)
        {
            return friend.Account1Id == accountId
                ? friend.Account2.Profiles.Where(item => item.IsMain).First().AvatarUrl
                : friend.Account1.Profiles.Where(item => item.IsMain).First().AvatarUrl;
        }



    }
}
