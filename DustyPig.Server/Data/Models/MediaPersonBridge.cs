namespace DustyPig.Server.Data.Models
{
    public enum Roles : int
    {
        Cast = 1,
        Director = 2,
        Producer = 3,
        Writer = 4
    }


    public class MediaPersonBridge
    {
        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int PersonId { get; set; }
        public Person Person { get; set; }

        public Roles Role { get; set; }

        public int SortOrder { get; set; }
    }
}
