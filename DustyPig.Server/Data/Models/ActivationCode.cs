using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class ActivationCode
    {
        [Key]
        [MaxLength(API.v3.Models.Constants.DEVICE_ACTIVATION_CODE_LENGTH)]
        public string Code { get; set; }

        public int? ProfileId { get; set; }
        public Profile Profile { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
