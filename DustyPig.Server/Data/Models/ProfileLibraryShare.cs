using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(ProfileId), nameof(LibraryId))]
    public class ProfileLibraryShare
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }
    }
}
