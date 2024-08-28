using Microsoft.EntityFrameworkCore;
using System;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(ProfileId), nameof(MediaEntryId))]
    public class ProfileMediaProgress
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public long? Xid { get; set; }


        /// <summary>
        /// Seconds
        /// </summary>
        public double Played { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
