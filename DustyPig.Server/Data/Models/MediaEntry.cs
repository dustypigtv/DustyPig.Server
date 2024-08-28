using Amazon.S3.Model;
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
    [Index(nameof(LibraryId))]
    [Index(nameof(TMDB_Id))]
    [Index(nameof(MovieRating))]
    [Index(nameof(TVRating))]
    [Index(nameof(LinkedToId))]
    [Index(nameof(Popularity))]
    [Index(nameof(TMDB_EntryId))]
    [Index(nameof(EntryType))]
    [Index(nameof(Added))]
    [Index(nameof(Genre_Action))]
    [Index(nameof(Genre_Adventure))]
    [Index(nameof(Genre_Animation))]
    [Index(nameof(Genre_Anime))]
    [Index(nameof(Genre_Awards_Show))]
    [Index(nameof(Genre_Children))]
    [Index(nameof(Genre_Comedy))]
    [Index(nameof(Genre_Crime))]
    [Index(nameof(Genre_Documentary))]
    [Index(nameof(Genre_Drama))]
    [Index(nameof(Genre_Family))]
    [Index(nameof(Genre_Fantasy))]
    [Index(nameof(Genre_Food))]
    [Index(nameof(Genre_Game_Show))]
    [Index(nameof(Genre_History))]
    [Index(nameof(Genre_Home_and_Garden))]
    [Index(nameof(Genre_Horror))]
    [Index(nameof(Genre_Indie))]
    [Index(nameof(Genre_Martial_Arts))]
    [Index(nameof(Genre_Mini_Series))]
    [Index(nameof(Genre_Music))]
    [Index(nameof(Genre_Musical))]
    [Index(nameof(Genre_Mystery))]
    [Index(nameof(Genre_News))]
    [Index(nameof(Genre_Podcast))]
    [Index(nameof(Genre_Political))]
    [Index(nameof(Genre_Reality))]
    [Index(nameof(Genre_Romance))]
    [Index(nameof(Genre_Science_Fiction))]
    [Index(nameof(Genre_Soap))]
    [Index(nameof(Genre_Sports))]
    [Index(nameof(Genre_Suspense))]
    [Index(nameof(Genre_Talk_Show))]
    [Index(nameof(Genre_Thriller))]
    [Index(nameof(Genre_Travel))]
    [Index(nameof(Genre_TV_Movie))]
    [Index(nameof(Genre_War))]
    [Index(nameof(Genre_Western))]
    public class MediaEntry
    {
        public int Id { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }

        public MediaTypes EntryType { get; set; }

        public int? TMDB_Id { get; set; }


        public int? TMDB_EntryId { get; set; }
        public TMDB_Entry TMDB_Entry { get; set; }



        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Title { get; set; }

        [Required]
        [MaxLength(128)]
        public string Hash { get; set; }

        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string SortTitle { get; set; }

        public DateOnly? Date { get; set; }

        [MaxLength(Constants.MAX_DESCRIPTION_LENGTH)]
        public string Description { get; set; }

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

        public DateTime Added { get; set; }

        public double? Popularity { get; set; }

        public MovieRatings? MovieRating { get; set; }

        public TVRatings? TVRating { get; set; }

        public bool Genre_Action { get; set; }

        public bool Genre_Adventure { get; set; }

        public bool Genre_Animation { get; set; }

        public bool Genre_Anime { get; set; }

        public bool Genre_Awards_Show { get; set; }

        public bool Genre_Children { get; set; }

        public bool Genre_Comedy { get; set; }

        public bool Genre_Crime { get; set; }

        public bool Genre_Documentary { get; set; }

        public bool Genre_Drama { get; set; }

        public bool Genre_Family { get; set; }

        public bool Genre_Fantasy { get; set; }

        public bool Genre_Food { get; set; }

        public bool Genre_Game_Show { get; set; }

        public bool Genre_History { get; set; }

        public bool Genre_Home_and_Garden { get; set; }

        public bool Genre_Horror { get; set; }

        public bool Genre_Indie { get; set; }

        public bool Genre_Martial_Arts { get; set; }

        public bool Genre_Mini_Series { get; set; }

        public bool Genre_Music { get; set; }

        public bool Genre_Musical { get; set; }

        public bool Genre_Mystery { get; set; }

        public bool Genre_News { get; set; }

        public bool Genre_Podcast { get; set; }

        public bool Genre_Political { get; set; }

        public bool Genre_Reality { get; set; }

        public bool Genre_Romance { get; set; }

        public bool Genre_Science_Fiction { get; set; }

        public bool Genre_Soap { get; set; }

        public bool Genre_Sports { get; set; }

        public bool Genre_Suspense { get; set; }

        public bool Genre_Talk_Show { get; set; }

        public bool Genre_Thriller { get; set; }

        public bool Genre_Travel { get; set; }

        public bool Genre_TV_Movie { get; set; }

        public bool Genre_War { get; set; }

        public bool Genre_Western { get; set; }

        /// <summary>
        /// Stores whether this entry has ever been played
        /// </summary>
        public bool EverPlayed { get; set; }


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

            if (EntryType == MediaTypes.Series)
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
