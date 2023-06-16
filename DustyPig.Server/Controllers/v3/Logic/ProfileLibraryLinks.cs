using DustyPig.API.v3.Models;
using DustyPig.Server.Data;
using DustyPig.Server.Data.Models;
using DustyPig.Server.HostedServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static class ProfileLibraryLinks
    {
        public static async Task<ResponseWrapper> LinkLibraryAndProfile(Account account, int profileId, int libraryId)
        {
            //Double check profile is owned by account
            if (!account.Profiles.Any(item => item.Id == profileId))
                return CommonResponses.NotFound("Profile");

            //See if already linked
            using var db = new AppDbContext();
            var rec = await db.ProfileLibraryShares
                .AsNoTracking()
                .Where(item => item.LibraryId == libraryId)
                .Where(item => item.ProfileId == profileId)
                .SingleOrDefaultAsync();

            if (rec != null)
                return CommonResponses.Ok();

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
                    return CommonResponses.NotFound("Library");
            }

            db.ProfileLibraryShares.Add(new ProfileLibraryShare
            {
                LibraryId = libraryId,
                ProfileId = profileId
            });


            //Scenario: Linked lib has items in a playlist. Then
            //Lib is unlinked, artwork is updated, then relinked - need
            //to update the artwork again
            var playlistIds = await db.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == profileId)
                .Select(item => item.Id)
                .Distinct()
                .ToListAsync();

            await db.SaveChangesAsync();

            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return CommonResponses.Ok();
        }

        public static async Task<ResponseWrapper> UnLinkLibraryAndProfile(Account account, int profileId, int libraryId)
        {
            //Double check profile is owned by account
            if (!account.Profiles.Any(item => item.Id == profileId))
                return CommonResponses.NotFound("Profile");

            //Get the link
            using var db = new AppDbContext();
            var rec = await db.ProfileLibraryShares
                .Where(item => item.LibraryId == libraryId)
                .Where(item => item.ProfileId == profileId)
                .SingleOrDefaultAsync();

            if (rec != null)
            {
                var playlistIds = await db.Playlists
                    .AsNoTracking()
                    .Where(item => item.ProfileId == profileId)
                    .Select(item => item.Id)
                    .Distinct()
                    .ToListAsync();

                db.ProfileLibraryShares.Remove(rec);
                await db.SaveChangesAsync();

                await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);
            }

            return CommonResponses.Ok();
        }
    }
}
