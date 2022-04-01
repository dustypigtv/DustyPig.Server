using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class DeviceToken
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        [MaxLength(1024)]
        public string Token { get; set; }

        public DateTime LastSeen { get; set; }
    }
}
