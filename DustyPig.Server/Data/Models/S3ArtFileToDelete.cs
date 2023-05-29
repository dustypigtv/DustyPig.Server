using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DustyPig.Server.Data.Models
{
    [Table("S3ArtFilesToDelete")]
    [Index(nameof(Url), IsUnique = true)]
    public class S3ArtFileToDelete
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string Url { get; set; }
    }
}
