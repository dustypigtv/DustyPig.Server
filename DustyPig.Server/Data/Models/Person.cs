using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Person
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        public MediaPersonBridge MediaBridges { get; set; }
    }
}
