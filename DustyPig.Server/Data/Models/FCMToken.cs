using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class FCMToken
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        [MaxLength(API.v3.Models.Constants.MAX_MOBILE_DEVICE_ID_LENGTH)]
        public string Token { get; set; }

        public DateTime LastSeen { get; set; }
    }
}
