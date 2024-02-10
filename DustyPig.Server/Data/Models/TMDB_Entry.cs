using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


/*
    Method to the madness:

    This is a cache of TMDB info. It should be updated about once/day, as long as a MediaEntry links to it.
    
    First, this helps keep popularity up to date, especially when adding new media:
        After updating, Popularity is copied to MediaEntries, and any info missing from MediaEntries but found here is copyied.
            Example:  If MediaEntry.BackdropUrl IS NULL, and TMDB_Entry.BackdropUrl IS NOT NULL, copy to MediaEntry

    Second, this keeps a local copy of Cast/Crew for MediaEntries.  Testing on 500,000 MediaEntries and 250,000 people,
    the join only increased query time by about 34ms.  But it allows other functionality in the apps!
*/

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(TMDB_Id))]
    [Index(nameof(MediaType))]
    [Index(nameof(TMDB_Id), nameof(MediaType), IsUnique = true)]
    public class TMDB_Entry
    {
        //Because movies and series can have the same tmdb_id, we need a database id to make this work correctly
        public int Id { get; set; }

        public int TMDB_Id { get; set; }

        public TMDB_MediaTypes MediaType { get; set; }

        [MaxLength(Constants.MAX_DESCRIPTION_LENGTH)]
        public string Description { get; set; }

        public MovieRatings? MovieRating { get; set; }

        public TVRatings? TVRating { get; set; }

        public DateTime? Date { get; set; }
                
        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string BackdropUrl { get; set; }

        public ulong BackdropSize { get; set; }

        public double Popularity { get; set; }

        public DateTime LastUpdated { get; set; }

        [DeleteBehavior(DeleteBehavior.Cascade)]
        public List<TMDB_EntryPersonBridge> People { get; set; }

        [DeleteBehavior(DeleteBehavior.SetNull)]
        public List<MediaEntry> MediaEntries { get; set; }
    }
}
