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
    public static class ProfileLibraryLinks
    {
        public static async Task<Result> LinkLibraryAndProfile(Account account, int profileId, int libraryId)
        {
            //Double check profile is owned by account
            var profile = account.Profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
                return CommonResponses.ValueNotFound("Profile");

            //See if already linked
            using var db = new AppDbContext();

            var library = await db.Libraries
                .AsNoTracking()
                .Include(l => l.ProfileLibraryShares.Where(p => p.ProfileId == profileId))
                .Include(l => l.FriendLibraryShares.Where(f => f.Friendship.Account1Id == account.Id || f.Friendship.Account2Id == account.Id))
                .ThenInclude(item => item.Friendship)
                .Where(l => l.Id == libraryId)
                .SingleOrDefaultAsync();

            if (library == null)
                return CommonResponses.ValueNotFound("Library");

            if (library.AccountId != account.Id)
                if (!library.FriendLibraryShares.Any(item => item.Friendship.Account1Id == account.Id))
                    if (!library.FriendLibraryShares.Any(item => item.Friendship.Account2Id == account.Id))
                        return CommonResponses.ValueNotFound("Library");


            //Main profile has access to everything at this point without links
            if (profile.IsMain)
                return Result.BuildSuccess();

            db.ProfileLibraryShares.Add(new ProfileLibraryShare
            {
                LibraryId = libraryId,
                ProfileId = profileId
            });


            await db.SaveChangesAsync();


            //Scenario: Linked lib has items in a playlist. Then
            //Lib is unlinked, artwork is updated, then relinked - need
            //to update the artwork again
            var playlistIds = await GetPlaylistIds(db, profileId, libraryId);
            await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);

            return Result.BuildSuccess();
        }

        public static async Task<Result> UnLinkLibraryAndProfile(Account account, int profileId, int libraryId)
        {
            //Double check profile is owned by account
            var profile = account.Profiles.FirstOrDefault(p => p.Id == profileId);
            if (profile == null)
                return Result.BuildSuccess();

            //Main profile has access to libs without links, so nothing to delete
            if (profile.IsMain)
                return Result.BuildSuccess();

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

                var playlistIds = await GetPlaylistIds(db, profileId, libraryId);
                await ArtworkUpdater.SetNeedsUpdateAsync(playlistIds);
            }

            return Result.BuildSuccess();
        }

        static Task<List<int>> GetPlaylistIds(AppDbContext db, int profileId, int libraryId)
        {
            return db.Playlists
                .AsNoTracking()
                .Where(item => item.ProfileId == profileId)
                .Where(item => item.PlaylistItems.Any(item2 => item2.MediaEntry.LibraryId == libraryId))
                .Select(item => item.Id)
                .Distinct()
                .ToListAsync();
        }
    }
}
