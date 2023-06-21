using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), IsUnique = false)]
    [Index(nameof(MediaEntryId), IsUnique = false)]
    public class Subscription
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }
    }
}
