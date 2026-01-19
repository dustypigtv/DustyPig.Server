using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicLibrary ToBasicLibraryInfo(this FriendLibraryShare self, int accountId) => new BasicLibrary
        {
            Name = Misc.Coalesce(self.LibraryDisplayName, self.Library.Name),
            Id = self.LibraryId,
            IsTV = self.Library.IsTV,
            Owner = self.Friendship.GetFriendDisplayNameForAccount(accountId)
        };

    }
}
