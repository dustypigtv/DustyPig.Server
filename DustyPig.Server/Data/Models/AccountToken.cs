using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class AccountToken
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        [MaxLength(128)]
        public string DeviceId { get; set; }
    }
}
