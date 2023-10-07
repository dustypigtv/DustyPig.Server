using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public class FriendLibraryLinkLogic
    {
        public static async Task<ResponseWrapper> LinkLibraryAndFriend(Account account, int friendId, int libraryId)
        {
            //Get friendship
            using var db = new AppDbContext();
            var friend = await GetFriend(db, account);

            if (friend == null)
                return CommonResponses.NotFound("Friend");

            //Check if already shared
            if (friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
                return CommonResponses.Ok();

            //Check if this account owns the library
            var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
            if (!myAcct.Libraries.Any(item => item.Id == libraryId))
                return CommonResponses.NotFound("Library");

            db.FriendLibraryShares.Add(new FriendLibraryShare
            {
                FriendshipId = friend.Id,
                LibraryId = libraryId
            });

            //Scenario: Shared lib has items in a playlist. Then
            //Lib is unshared, artwork is updated, then reshared - need
            //to update the artwork again
            var playlistIds = await GetPlaylistIds(db, account, friend, libraryId);
            await db.SaveChangesAsync();
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return CommonResponses.Ok();
        }

        public static async Task<ResponseWrapper> UnLinkLibraryAndFriend(Account account, int friendId, int libraryId)
        {
            //Get friendship
            using var db = new AppDbContext();
            var friend = await GetFriend(db, account);

            if (friend == null)
                return CommonResponses.Ok();

            //Check if link exists
            if (!friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
                return CommonResponses.Ok();

            //Check if this account owns the library
            var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
            if (myAcct.Libraries.Any(item => item.Id == libraryId))
                return CommonResponses.Ok();

            var share = new FriendLibraryShare
            {
                FriendshipId = friendId,
                LibraryId = libraryId
            };

            db.Entry(share).State = EntityState.Deleted;
            var playlistIds = await GetPlaylistIds(db, account, friend, libraryId);
            await db.SaveChangesAsync();
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return CommonResponses.Ok();
        }

        static Task<Friendship> GetFriend(AppDbContext db, Account account) =>
            db.Friendships
                .AsNoTracking()
                .Include(item => item.FriendLibraryShares)
                .Include(item => item.Account1)
                .ThenInclude(item => item.Libraries)
                .Include(item => item.Account2)
                .ThenInclude(item => item.Libraries)
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
