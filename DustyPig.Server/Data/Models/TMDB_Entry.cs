using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public class TMDB_Entry
    {
        [Key]
        public int TMDB_Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Title { get; set; }

        [MaxLength(Constants.MAX_DESCRIPTION_LENGTH)]
        public string Description { get; set; }

        public MovieRatings? MovieRating { get; set; }

        public TVRatings? TVRating { get; set; }

        public DateTime? Date { get; set; }

        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string PosterUrl { get; set; }

        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string BackdropUrl { get; set; }

        public double? Popularity { get; set; }

        public DateTime LastUpdated { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        public List<TMDB_EntryPersonBridge> People { get; set; }

        [DeleteBehavior(DeleteBehavior.SetNull)]
        public List<MediaEntry> MediaEntries { get; set; }
    }
}
