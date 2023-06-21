using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), nameof(MediaEntryId), IsUnique = true)]
    [Index(nameof(ProfileId), IsUnique = false)]
    [Index(nameof(MediaEntryId), IsUnique = false)]
    public class TitleOverride
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public OverrideState State { get; set; }

        public OverrideRequestStatus Status { get; set; }
    }
}
