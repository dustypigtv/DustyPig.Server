using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicLibrary ToBasicLibraryInfo(this FriendLibraryShare @this, int accountId) => new BasicLibrary
        {
            Name = Utils.Coalesce(@this.LibraryDisplayName, @this.Library.Name),
            Id = @this.LibraryId,
            IsTV = @this.Library.IsTV,
            Owner = @this.Friendship.GetFriendDisplayNameForAccount(accountId)
        };

    }
}
