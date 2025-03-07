﻿using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(FriendsController))]
    public class FriendsController : _BaseProfileController
    {
        public FriendsController(AppDbContext db) : base(db) { }


        /// <summary>
        /// Requires profile
        /// </summary>
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<List<BasicFriend>>))]
        public async Task<Result<List<BasicFriend>>> List()
        {
            var friends = await DB.Friendships
                .AsNoTracking()

                .Include(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .ToListAsync();

            var ret = friends.Select(item => item.ToBasicFriendInfo(UserAccount.Id)).ToList();
            ret.Sort();

            return ret;
        }


        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpGet("{id}")]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result<DetailedFriend>))]
        public async Task<Result<DetailedFriend>> Details(int id)
        {
            var friend = await DB.Friendships
                .AsNoTracking()

                .Include(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.FriendLibraryShares)
                .ThenInclude(item => item.Library)

                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .Where(item => item.Id == id)
                .SingleOrDefaultAsync();

            if (friend == null)
                return CommonResponses.ValueNotFound(nameof(id));

            var displayName = friend.GetFriendDisplayNameForAccount(UserAccount.Id);
            var ret = new DetailedFriend
            {
                Id = id,
                Accepted = friend.Accepted,
                FriendRequestDirection = friend.Account1Id == UserAccount.Id ? RequestDirection.Sent : RequestDirection.Received,
                DisplayName = displayName,
                Initials = displayName.GetInitials(),
                AvatarUrl = friend.GetFriendAvatar(UserAccount.Id)
            };
            ret.Initials = ret.DisplayName.GetInitials();


            foreach (var share in friend.FriendLibraryShares)
            {
                var lib = share.Library.ToBasicLibraryInfo();
                if (share.Library.AccountId == UserAccount.Id)
                {
                    ret.SharedWithFriend ??= new();
                    ret.SharedWithFriend.Add(lib);
                }
                else
                {
                    lib.Name = share.LibraryDisplayName;
                    ret.SharedWithMe ??= new();
                    ret.SharedWithMe.Add(lib);
                }
            }

            return ret;
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Invite another user to be friends</remarks>
        /// <param name="email"># This _MUST_ be a JSON encoded string</param>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Invite(StringValue email)
        {
            try { email.Validate(); }
            catch (ModelValidationException ex) { return ex; }

            if (string.IsNullOrWhiteSpace(email.Value))
                return CommonResponses.InvalidValue(nameof(email));

            email.Value = email.Value.Trim();


            UserRecord fbRec = null;
            try
            {
                fbRec = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email.Value);
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.Message.StartsWith("Failed to get user with email:"))
                    return "Account does not exist";
                throw;
            }


            var friendAccount = await DB.Accounts
                .AsNoTracking()
                .Include(item => item.Profiles)
                .Where(item => item.FirebaseId == fbRec.Uid)
                .SingleOrDefaultAsync();

            if (friendAccount == null)
            {
                using var db2 = new AppDbContext();
                friendAccount = await db2.GetOrCreateAccountAsync(fbRec.Uid, email.Value);

                if (friendAccount == null)
                    return "Account does not exist";
            }



            if (friendAccount.Id == UserAccount.Id)
                return "Cannot friend yourself";

            string uniqueFriendId = LogicUtils.UniqueFriendId(UserAccount.Id, friendAccount.Id);
            var friendship = await DB.Friendships
                .Where(item => item.Hash == uniqueFriendId)
                .SingleOrDefaultAsync();

            if (friendship != null)
            {
                if (friendship.Accepted)
                {
                    return "You are already friends";
                }
                else
                {
                    if (friendship.Account1Id == UserAccount.Id)
                        return "You already sent a friendship request to this account";
                    else
                        return "This account already sent you a friendship request, and is waiting for you to accept it";
                }
            }

            //Request
            friendship = DB.Friendships.Add(new Friendship
            {
                Account1Id = UserAccount.Id,
                Account2Id = friendAccount.Id,
                Hash = uniqueFriendId
            }).Entity;

            //Notification
            //Make sure friend account has a main profile (prev beta version created a scenario with an account but no profile)
            var friendMainProfile = friendAccount.Profiles.FirstOrDefault(item => item.IsMain);
            if (friendMainProfile == null)
            {
                using var db2 = new AppDbContext();
                friendMainProfile = await db2.GetOrCreateMainProfileAsync(friendAccount, email.Value);
            }

            DB.Notifications.Add(new Data.Models.Notification
            {
                Friendship = friendship,
                Title = "Friend Request",
                Message = $"{UserProfile.Name} has sent you a friend request",
                NotificationType = NotificationTypes.FriendshipInvited,
                ProfileId = friendMainProfile.Id,
                Timestamp = DateTime.UtcNow
            });

            await DB.SaveChangesAsync();

            FirebaseNotificationsManager.QueueProfileForNotifications(friendMainProfile.Id);

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        /// <remarks>Use this to accept friend requests and update display names</remarks>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Update(UpdateFriend info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return ex; }


            var friendship = await DB.Friendships
                .Include(item => item.Account1)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Account2)
                .ThenInclude(item => item.Profiles)
                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .Where(item => item.Id == info.Id)
                .SingleOrDefaultAsync();

            if (friendship == null)
                return CommonResponses.ValueNotFound(nameof(info.Id));

            //Note: Invites always go from Account1Id to Account2Id

            //Cannot accept on behalf of other person
            if (friendship.Account1Id == UserAccount.Id && !friendship.Accepted && info.Accepted)
                return "Cannot accept friend request on behalf of another person";

            //Cannot go from Accepted to Invited
            if (friendship.Accepted && !info.Accepted)
                return "Cannot go from Accepted to Invited. Please use the Unfriend method";

            int notifyProfileId = -1;
            if (info.Accepted && !friendship.Accepted)
            {
                friendship.Accepted = true;

                notifyProfileId = friendship.Account1.Profiles.Single(item => item.IsMain).Id;
                DB.Notifications.Add(new Data.Models.Notification
                {
                    FriendshipId = friendship.Id,
                    Title = "You have a new friend!",
                    Message = $"{UserProfile.Name} has accepted your friend request",
                    NotificationType = NotificationTypes.FriendshipAccepted,
                    ProfileId = notifyProfileId,
                    Timestamp = DateTime.UtcNow
                });

            }


            //Set display name
            if (friendship.Account1Id == UserAccount.Id)
                friendship.DisplayName2 = info.DisplayName;
            else
                friendship.DisplayName1 = info.DisplayName;

            await DB.SaveChangesAsync();

            if(notifyProfileId > 0)
                FirebaseNotificationsManager.QueueProfileForNotifications(notifyProfileId);

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpDelete("{id}")]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public async Task<Result> Unfriend(int id)
        {
            var friend = await DB.Friendships
                .Where(item => item.Id == id)
                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .SingleOrDefaultAsync();

            if (friend != null)
            {
                int friendAcctId = friend.Account1Id == UserAccount.Id ? friend.Account2Id : friend.Account1Id;

                var q =
                    from me in DB.MediaEntries
                    join lib in DB.Libraries.Where(item => item.AccountId == friendAcctId) on me.LibraryId equals lib.Id
                    join pi in DB.PlaylistItems on me.Id equals pi.MediaEntryId
                    select pi.PlaylistId;

                var playlistIds = await q.Distinct().ToListAsync();

                DB.Friendships.Remove(friend);
                await DB.SaveChangesAsync();

                await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);
            }

            return Result.BuildSuccess();
        }



        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> ShareLibrary(LibraryFriendLink lnk) => FriendLibraryLinkLogic.LinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);



        /// <summary>
        /// Requires main profile
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [RequireMainProfile]
        [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Result))]
        public Task<Result> UnShareLibrary(LibraryFriendLink lnk) => FriendLibraryLinkLogic.UnLinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);

    }
}
