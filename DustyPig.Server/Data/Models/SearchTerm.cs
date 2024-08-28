using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(Term), IsUnique = true)]
    public class SearchTerm
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Term { get; set; }

        public List<MediaSearchBridge> SearchTermBridges { get; set; } = new List<MediaSearchBridge>();
    }
}
