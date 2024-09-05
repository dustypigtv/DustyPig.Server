using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class TMDB_Person
    {
        [Key]
        public int TMDB_Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string AvatarUrl { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        public List<TMDB_EntryPersonBridge> TMDB_EntryBridges { get; set; }
    }
}
