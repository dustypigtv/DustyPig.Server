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
        [MaxLength(32)]
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
        public string VideoUrl { get; set; }
        public int? VideoServiceCredentialId { get; set; }
        public EncryptedServiceCredential VideoServiceCredential { get; set; }


        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string BifUrl { get; set; }
        public int? BifServiceCredentialId { get; set; }
        public EncryptedServiceCredential BifServiceCredential { get; set; }


        public List<MediaPersonBridge> People { get; set; }

        public DateTime? Added { get; set; }

        public double? Popularity { get; set; }


        public bool NotificationsCreated { get; set; }


        //public List<MediaEntry> LinkedItems { get; set; } = new List<MediaEntry>();

        public List<OverrideRequest> OverrideRequests { get; set; } = new List<OverrideRequest>();

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
                return Crypto.HashEpisode(LinkedToId.Value, Season.Value, Episode.Value);

            if (EntryType == MediaTypes.Movie)
                return Crypto.HashMovieTitle(Title, Date.Value.Year);

            //Series
            return Crypto.NormalizedHash(Title);
        }

        public long? ComputeXid()
        {
            if (EntryType == MediaTypes.Episode)
            {
                int s = Season ?? 0;
                int e = Episode ?? 0;
                return s * int.MaxValue + e;
            }
            return null;
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
