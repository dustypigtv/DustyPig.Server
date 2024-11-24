using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Data;

public partial class AppDbContext
{
    public async Task<List<int>> GetLibraryIdsAccessableByAccount(int accountId)
    {
        //Libs owned by the account
        var libs = await this.Libraries
            .AsNoTracking()
            .Where(item => item.AccountId == accountId)
            .Select(item => item.Id)
            .ToListAsync();


        //Libs shared with the account
        var sharedLibs = await this.FriendLibraryShares
            .AsNoTracking()

            .Include(item => item.Friendship)
            .ThenInclude(item => item.Account1)
            .ThenInclude(item => item.Profiles)

            .Include(item => item.Friendship)
            .ThenInclude(item => item.Account2)
            .ThenInclude(item => item.Profiles)

            .Include(item => item.Library)

            .Where(item => item.Friendship.Account1Id == accountId || item.Friendship.Account2Id == accountId)
            .Where(item => item.Friendship.Accepted)
            .Where(item => !libs.Contains(item.LibraryId))

            .Select(item => item.LibraryId)
            .ToListAsync();

        libs.AddRange(sharedLibs);

        return libs;
    }
}
