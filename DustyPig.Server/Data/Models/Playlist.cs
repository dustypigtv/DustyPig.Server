using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), nameof(Name), nameof(CurrentIndex), IsUnique = true)]
    public class Playlist
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }
        
        /// <summary>
        /// Current Item Index
        /// </summary>
        public int CurrentIndex { get; set; }

        public List<PlaylistItem> PlaylistItems { get; set; }
    }
}
