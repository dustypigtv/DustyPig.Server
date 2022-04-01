using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class ActivationCode
    {
        [Key]
        [MaxLength(5)]
        public string Code { get; set; }

        public int? AccountId { get; set; }
        public Account Account { get; set; }
    }
}
