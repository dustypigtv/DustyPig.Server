using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(MediaEntryId), nameof(SearchTermId))]
    public class MediaSearchBridge
    {
        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int SearchTermId { get; set; }
        public SearchTerm SearchTerm { get; set; }
    }
}
