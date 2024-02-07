using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public class FriendLibraryLinkLogic
    {
        public static async Task<IActionResult> LinkLibraryAndFriend(Account account, int friendId, int libraryId)
        {
            //Get friendship
            using var db = new AppDbContext();
            var friend = await GetFriend(db, account, friendId);

            if (friend == null)
                return CommonResponses.ValueNotFound("Friend");

            //Check if already shared
            if (friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
                return new OkResult();

            //Check if this account owns the library
            var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
            if (!myAcct.Libraries.Any(item => item.Id == libraryId))
                return CommonResponses.ValueNotFound("Library");

            db.FriendLibraryShares.Add(new FriendLibraryShare
            {
                FriendshipId = friend.Id,
                LibraryId = libraryId
            });

            await db.SaveChangesAsync();


            //Scenario: Shared lib has items in a playlist. Then
            //Lib is unshared, artwork is updated, then reshared - need
            //to update the artwork again
            var playlistIds = await GetPlaylistIds(db, account, friend, libraryId);
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return new OkResult();
        }

        public static async Task<IActionResult> UnLinkLibraryAndFriend(Account account, int friendId, int libraryId)
        {
            //Get friendship
            using var db = new AppDbContext();
            var friend = await GetFriend(db, account, friendId);

            if (friend == null)
                return new OkResult();

            //Check if link exists
            if (!friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
                return new OkResult();

            //Check if this account owns the library
            var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
            if (!myAcct.Libraries.Any(item => item.Id == libraryId))
                return new OkResult();

            var share = new FriendLibraryShare
            {
                FriendshipId = friendId,
                LibraryId = libraryId
            };

            db.FriendLibraryShares.Remove(share);
            await db.SaveChangesAsync();

            var playlistIds = await GetPlaylistIds(db, account, friend, libraryId);
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return new OkResult();
        }

        static Task<Friendship> GetFriend(AppDbContext db, Account account, int friendId) =>
            db.Friendships
                .AsNoTracking()
                .Include(item => item.FriendLibraryShares)
                .Include(item => item.Account1)
                .ThenInclude(item => item.Libraries)
                .Include(item => item.Account2)
                .ThenInclude(item => item.Libraries)
                .Where(item => item.Id == friendId)
                .Where(item => item.Account1Id == account.Id || item.Account2Id == account.Id)
                .FirstOrDefaultAsync();


        static Task<List<int>> GetPlaylistIds(AppDbContext db, Account account, Friendship friend, int libraryId)
        {
            var friendAcct = friend.Account1Id == account.Id ? friend.Account2 : friend.Account1;

            return db.Playlists
                .AsNoTracking()
                .Where(item => item.Profile.AccountId == friendAcct.Id)
                .Where(item => item.PlaylistItems.Any(item2 => item2.MediaEntry.LibraryId == libraryId))
                .Select(item => item.Id)
                .Distinct()
                .ToListAsync();
        }

    }
}
