namespace DustyPig.Server.Data.Models
{
    public class MediaSearchBridge
    {
        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int SearchTermId { get; set; }
        public SearchTerm SearchTerm { get; set; }
    }
}
