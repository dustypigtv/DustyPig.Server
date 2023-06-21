using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(MediaEntryId), IsUnique = false)]
    [Index(nameof(SearchTermId), IsUnique = false)]
    public class MediaSearchBridge
    {
        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int SearchTermId { get; set; }
        public SearchTerm SearchTerm { get; set; }
    }
}
