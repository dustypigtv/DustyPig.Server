using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(LibraryId), nameof(EntryType), nameof(TMDB_Id), nameof(Hash), IsUnique = true)]
    [Index(nameof(LibraryId), IsUnique = false)]
    [Index(nameof(TMDB_Id), IsUnique = false)]
    public class MediaEntry
    {
        public int Id { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }

        public MediaTypes EntryType { get; set; }

        public int? TMDB_Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Title { get; set; }

        [Required]
        [MaxLength(128)]
        public string Hash { get; set; }

        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string SortTitle { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        public Ratings? Rated { get; set; }

        [MaxLength(Constants.MAX_DESCRIPTION_LENGTH)]
        public string Description { get; set; }

        public Genres? Genres { get; set; }

        public int? LinkedToId { get; set; }
        public MediaEntry LinkedTo { get; set; }

        public int? Season { get; set; }

        public int? Episode { get; set; }

        public long? Xid { get; set; }

        /// <summary>
        /// Future Use
        /// </summary>
        public int? ExtraSortOrder { get; set; }

        /// <summary>
        /// Seconds
        /// </summary>
        public double? Length { get; set; }

        /// <summary>
        /// Seconds
        /// </summary>
        public double? IntroStartTime { get; set; }

        /// <summary>
        /// Seconds
        /// </summary>
        public double? IntroEndTime { get; set; }

        /// <summary>
        /// Seconds
        /// </summary>
        public double? CreditsStartTime { get; set; }

        [Required]
        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string ArtworkUrl { get; set; }

        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string BackdropUrl { get; set; }


        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string VideoUrl { get; set; }


        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string BifUrl { get; set; }


        public List<MediaPersonBridge> People { get; set; }

        public DateTime? Added { get; set; }

        public double? Popularity { get; set; }

        public DateTime? PopularityUpdated { get; set; }

        public List<PlaylistItem> PlaylistItems { get; set; } = new List<PlaylistItem>();

        public List<ProfileMediaProgress> ProfileMediaProgress { get; set; } = new List<ProfileMediaProgress>();

        public List<MediaSearchBridge> MediaSearchBridges { get; set; } = new List<MediaSearchBridge>();

        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public List<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();

        public List<TitleOverride> TitleOverrides { get; set; } = new List<TitleOverride>();

        public List<Subtitle> Subtitles { get; set; } = new List<Subtitle>();


        [NotMapped]
        public string QueryTitle
        {
            get
            {
                if (_queryTitle == null)
                    _queryTitle = Title.NormalizedQueryString();
                return _queryTitle;
            }
        }
        private string _queryTitle;


        public string ComputeHash()
        {
            if (EntryType == MediaTypes.Episode)
                Hash = Crypto.HashEpisode(LinkedToId.Value, Season.Value, Episode.Value);

            if (EntryType == MediaTypes.Movie)
                Hash = Crypto.HashMovieTitle(Title, Date.Value.Year);

            if(EntryType == MediaTypes.Series)
                Hash = Crypto.NormalizedHash(Title);

            return Hash;
        }

        public long? ComputeXid()
        {
            Xid = null;

            if (EntryType == MediaTypes.Episode)
            {
                long s = Season ?? 0;
                long e = Episode ?? 0;

                //To make sure specials are sorted AFTER all other seasons,
                //treat season 0 as season 10,000
                //There aren't 10k seasons of any show, and even ones that
                //Go by year max out at < 3,000
                if (s < 1)
                    s = 10000;

                Xid = s * int.MaxValue + e;
            }

            return Xid;
        }

        public string FormattedTitle()
        {
            string ret = Title;
            if (EntryType == MediaTypes.Movie && Date.HasValue)
                ret += $" ({Date.Value.Year})";
            return ret;
        }
    }
}
