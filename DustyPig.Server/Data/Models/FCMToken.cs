using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(Hash), IsUnique = true)]
    public class FCMToken
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        [MaxLength(API.v3.Models.Constants.MAX_MOBILE_DEVICE_ID_LENGTH)]
        public string Token { get; set; }

        //The hash is required because Token is too big for a unique index
        [Required]
        [MaxLength(128)]
        public string Hash { get; set; }

        public DateTime LastSeen { get; set; }


        public void ComputeHash()
        {
            Hash = Crypto.NormalizedString(Token);
        }
    }
}
