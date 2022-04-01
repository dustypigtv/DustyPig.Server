using DustyPig.API.v3.Models;

namespace DustyPig.Server.Data.Models
{
    public class TitleOverride
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public OverrideState State { get; set; }
    }
}
