using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(TMDB_Id), IsUnique = false)]
    [Index(nameof(MediaType), IsUnique = false)]
    [Index(nameof(TMDB_Id), nameof(MediaType), IsUnique = true)]
    public class TMDB_Entry
    {
        //Because movies and series can have the same tmdb_id, we need a database id to make this work correctly
        public int Id { get; set; }

        public int TMDB_Id { get; set; }

        public TMDB_MediaTypes MediaType { get; set; }

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

        public double Popularity { get; set; }

        public DateTime LastUpdated { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        public List<TMDB_EntryPersonBridge> People { get; set; }

        [DeleteBehavior(DeleteBehavior.SetNull)]
        public List<MediaEntry> MediaEntries { get; set; }
    }
}
