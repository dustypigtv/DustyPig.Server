using DustyPig.API.v3.Models;
using System;

namespace DustyPig.Server.Data.Models
{
    public class OverrideRequest
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public RequestStatus Status { get; set; }

        public bool NotificationCreated { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
