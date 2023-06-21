using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), IsUnique = false)]
    [Index(nameof(LibraryId), IsUnique = false)]
    public class ProfileLibraryShare
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }
    }
}
