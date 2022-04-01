using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public class FriendLibraryLinkLogic
    {
        public static async Task<ActionResult> LinkLibraryAndFriend(Account account, int friendId, int libraryId)
        {
            //Get friendship
            using var db = new AppDbContext();
            var friend = await db.Friendships
                .AsNoTracking()
                .Include(item => item.FriendLibraryShares)
                .Include(item => item.Account1)
                .ThenInclude(item => item.Libraries)
                .Include(item => item.Account2)
                .ThenInclude(item => item.Libraries)
                .Where(item => item.Account1Id == account.Id || item.Account2Id == account.Id)
                .SingleOrDefaultAsync();

            if (friend == null)
                return CommonResponses.NotFoundObject("Friend not found");

            //Check if already shared
            if (friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
                return CommonResponses.Ok;

            //Check if this account owns the library
            var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
            if (myAcct.Libraries.Any(item => item.Id == libraryId))
                return CommonResponses.NotFoundObject("Library not found");

            db.FriendLibraryShares.Add(new FriendLibraryShare
            {
                FriendshipId = friend.Id,
                LibraryId = libraryId
            });

            await db.SaveChangesAsync();

            return CommonResponses.Ok;
        }

        public static async Task<ActionResult> UnLinkLibraryAndFriend(Account account, int friendId, int libraryId)
        {
            //Get friendship
            using var db = new AppDbContext();
            var friend = await db.Friendships
                .AsNoTracking()
                .Include(item => item.FriendLibraryShares)
                .Include(item => item.Account1)
                .ThenInclude(item => item.Libraries)
                .Include(item => item.Account2)
                .ThenInclude(item => item.Libraries)
                .Where(item => item.Account1Id == account.Id || item.Account2Id == account.Id)
                .SingleOrDefaultAsync();

            if (friend == null)
                return CommonResponses.Ok;

            //Check if link exists
            if (!friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
                return new OkResult();

            //Check if this account owns the library
            var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
            if (myAcct.Libraries.Any(item => item.Id == libraryId))
                return CommonResponses.Ok;

            var share = new FriendLibraryShare
            {
                FriendshipId = friendId,
                LibraryId = libraryId
            };

            db.Entry(share).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return new OkResult();
        }
    }
}
