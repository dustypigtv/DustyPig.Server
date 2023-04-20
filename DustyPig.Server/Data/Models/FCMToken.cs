using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), nameof(TokenHash), IsUnique = true)]
    public class FCMToken
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        [MaxLength(API.v3.Models.Constants.MAX_MOBILE_DEVICE_ID_LENGTH)]
        public string Token { get; set; }

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; }

        public DateTime LastSeen { get; set; }
    }
}
