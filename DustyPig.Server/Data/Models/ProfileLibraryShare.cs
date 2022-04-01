namespace DustyPig.Server.Data.Models
{
    public class ProfileLibraryShare
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }
    }
}
