using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), nameof(Name), IsUnique = true)]
    [Index(nameof(ProfileId), IsUnique = false)]
    public class Playlist
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        /// <summary>
        /// Don't create a reference to the PlaylistItem, or the playlist could be accidentally deleted if
        /// the PlaylistItem or the MediaItem is deleted. Handle it manually
        /// </summary>        
        public int CurrentItemId { get; set; }



        public double CurrentProgress { get; set; }

        [Required]
        [DefaultValue(Constants.DEFAULT_PLAYLIST_IMAGE)]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string ArtworkUrl { get; set; }

        public bool ArtworkUpdateNeeded { get; set; }

        public List<PlaylistItem> PlaylistItems { get; set; }
    }
}
