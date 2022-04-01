using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class ProfileLibraryLinks
    {
        public static async Task<ActionResult> LinkLibraryAndProfile(Account account, int profileId, int libraryId)
        {
            //Double check profile is owned by account
            if (!account.Profiles.Any(item => item.Id == profileId))
                return CommonResponses.NotFoundObject("Profile not found");

            //See if already linked
            using var db = new AppDbContext();
            var rec = await db.ProfileLibraryShares
                .AsNoTracking()
                .Where(item => item.LibraryId == libraryId)
                .Where(item => item.ProfileId == profileId)
                .SingleOrDefaultAsync();

            if (rec != null)
                return new OkResult();

            //See if the lib is owned by the account
            bool owned = await db.Libraries
                .AsNoTracking()
                .Where(item => item.AccountId == account.Id)
                .Where(item => item.Id == libraryId)
                .AnyAsync();

            if (!owned)
            {
                //See if the library is shared with the account
                bool shared = await db.FriendLibraryShares
                    .AsNoTracking()
                    .Where(item => item.LibraryId == libraryId)
                    .Include(item => item.Friendship)
                    .Select(item => item.Friendship)
                    .Where(item => item.Account1Id == account.Id || item.Account2Id == account.Id)
                    .AnyAsync();

                if (!shared)
                    return CommonResponses.NotFoundObject("Library not found");
            }

            db.ProfileLibraryShares.Add(new ProfileLibraryShare
            {
                LibraryId = libraryId,
                ProfileId = profileId
            });

            await db.SaveChangesAsync();

            return new OkResult();
        }

        public static async Task<ActionResult> UnLinkLibraryAndProfile(Account account, int profileId, int libraryId)
        {
            //Double check profile is owned by account
            if (!account.Profiles.Any(item => item.Id == profileId))
                return CommonResponses.NotFoundObject("Profile not found");

            //Get the link
            using var db = new AppDbContext();
            var rec = await db.ProfileLibraryShares
                .Where(item => item.LibraryId == libraryId)
                .Where(item => item.ProfileId == profileId)
                .SingleOrDefaultAsync();

            if (rec != null)
            {
                db.ProfileLibraryShares.Remove(rec);
                await db.SaveChangesAsync();
            }

            return new OkResult();
        }
    }
}
