using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(ProfileId), nameof(MediaEntryId))]
    public class Subscription
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }
    }
}
