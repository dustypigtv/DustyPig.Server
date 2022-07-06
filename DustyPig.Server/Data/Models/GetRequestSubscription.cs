namespace DustyPig.Server.Data.Models
{
    public class GetRequestSubscription
    {
        public int GetRequestId { get; set; }
        public GetRequest GetRequest { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
    }
}
