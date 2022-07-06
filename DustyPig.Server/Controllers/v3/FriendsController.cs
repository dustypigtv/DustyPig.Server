using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Controllers.v3.Filters;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3
{
    [ApiController]
    [ExceptionLogger(typeof(FriendsController))]
    [RequireMainProfile]
    [SwaggerResponse((int)HttpStatusCode.Forbidden)]
    public class FriendsController : _BaseProfileController
    {
        public FriendsController(AppDbContext db) : base(db) { }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult<List<BasicFriend>>> List()
        {
            var friends = await DB.Friendships
                .AsNoTracking()

                .Include(item => item.Account1)
                .ThenInclude(item => item.Profiles)

                .Include(item => item.Account2)
                .ThenInclude(item => item.Profiles)

                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .ToListAsync();

            var ret = friends
                .Select(item => item.ToBasicFriendInfo(UserAccount.Id))
                .ToList();

            ret.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
            return ret;
        }


        /// <summary>
        /// Level 3
        /// </summary>
        [HttpGet("{id}")]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<DetailedFriend>> Details(int id)
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
                return NotFound();

            var ret = new DetailedFriend
            {
                Id = id,
                Accepted = friend.Accepted,
                DisplayName = friend.GetFriendDisplayNameForAccount(UserAccount.Id)
            };

            foreach (var share in friend.FriendLibraryShares)
            {
                var lib = share.Library.ToBasicLibraryInfo();
                if (share.Library.AccountId == UserAccount.Id)
                {
                    ret.SharedWithFriend.Add(lib);
                }
                else
                {
                    lib.Name = share.LibraryDisplayName;
                    ret.SharedWithMe.Add(lib);
                }
            }

            return ret;
        }



        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Invite another user to be friends</remarks>
        [HttpPost]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.Accepted)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Invite(SimpleValue<string> email)
        {
            if (string.IsNullOrWhiteSpace(email.Value))
                return BadRequest("email must be spcified");

            email.Value = email.Value.Trim();


            UserRecord fbRec = null;
            try
            {
                fbRec = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email.Value);
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.Message.StartsWith("Failed to get user with email:"))
                    return NotFound("Account does not exist");
                throw;
            }


            var friendAccount = await DB.Accounts
                .AsNoTracking()
                .Include(item => item.Profiles)
                .Where(item => item.FirebaseId == fbRec.Uid)
                .SingleOrDefaultAsync();

            if (friendAccount == null)
                return BadRequest("THe specified email doesn't appear to be a Dusty Pig account");

            if (friendAccount.Id == UserAccount.Id)
                return BadRequest("Cannot friend yourself");

            string uniqueFriendId = Utils.UniqueFriendId(UserAccount.Id, friendAccount.Id);
            var friendship = await DB.Friendships
                .Where(item => item.Hash == uniqueFriendId)
                .SingleOrDefaultAsync();

            if (friendship != null)
            {
                if (friendship.Accepted)
                {
                    return BadRequest("You are already friends");
                }
                else
                {
                    if (friendship.Account1Id == UserAccount.Id)
                        return BadRequest("You already sent a friendship request to this account");
                    else
                        return BadRequest("This account already sent you a friendship request, and is waiting for you to accept it");
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
            DB.Notifications.Add(new Data.Models.Notification
            {
                Friendship = friendship,
                Title = "Friend Request",
                Message = $"{UserProfile.Name} has sent you a friend request",
                NotificationType = NotificationType.Friend,
                ProfileId = friendAccount.Profiles.Single(item => item.IsMain).Id,
                Timestamp = DateTime.UtcNow
            });

            await DB.SaveChangesAsync();

            return Accepted();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        /// <remarks>Use this to accept friend requests and update display names</remarks>
        [HttpPost]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.BadRequest)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult> Update(UpdateFriend info)
        {
            try { info.Validate(); }
            catch (ModelValidationException ex) { return BadRequest(ex.ToString()); }


            var friendship = await DB.Friendships
                .Include(item => item.Account1)
                .ThenInclude(item => item.Profiles)
                .Include(item => item.Account2)
                .ThenInclude(item => item.Profiles)
                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .Where(item => item.Id == info.Id)
                .SingleOrDefaultAsync();

            if (friendship == null)
                return NotFound();

            //Note: Invites always go from Account1Id to Account2Id

            //Cannot accept on behalf of other person
            if (friendship.Account1Id == UserAccount.Id && !friendship.Accepted && info.Accepted)
                return BadRequest("Cannot accept friend request on behalf of another person");

            //Cannot go from Accepted to Invited
            if (friendship.Accepted && !info.Accepted)
                return BadRequest("Cannot go from Accepted to Invited. Please use the delete method");


            if (info.Accepted && !friendship.Accepted)
            {
                friendship.Accepted = true;
               
                DB.Notifications.Add(new Data.Models.Notification
                {
                    FriendshipId = friendship.Id,
                    Title = "You have a new friend!",
                    Message = $"{UserProfile.Name} has accepted your friend request",
                    NotificationType = NotificationType.Friend,
                    ProfileId = friendship.Account1.Profiles.Single(item => item.IsMain).Id,
                    Timestamp = DateTime.UtcNow
                });

            }


            //Set display name
            if (friendship.Account1Id == UserAccount.Id)
                friendship.DisplayName2 = info.DisplayName;
            else
                friendship.DisplayName1 = info.DisplayName;

            await DB.SaveChangesAsync();

            return Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<ActionResult> Unfriend(int id)
        {
            var friend = await DB.Friendships
                .Where(item => item.Id == id)
                .Where(item => item.Account1Id == UserAccount.Id || item.Account2Id == UserAccount.Id)
                .SingleOrDefaultAsync();

            if (friend != null)
            {
                DB.Friendships.Remove(friend);
                await DB.SaveChangesAsync();
            }

            return Ok();
        }



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        [SwaggerResponse((int)HttpStatusCode.NotFound)]
        public Task<ActionResult> ShareLibrary(LibraryFriendLink lnk) => FriendLibraryLinkLogic.LinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);



        /// <summary>
        /// Level 3
        /// </summary>
        [HttpPost]
        [ProhibitTestUser]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public Task<ActionResult> UnShareLibrary(LibraryFriendLink lnk) => FriendLibraryLinkLogic.UnLinkLibraryAndFriend(UserAccount, lnk.FriendId, lnk.LibraryId);

    }
}
