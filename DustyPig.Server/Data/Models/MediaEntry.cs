using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Extensions;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

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
        public const int MAX_SEARCH_TITLE_SIZE = 1000;


        public int Id { get; set; }

        public int LibraryId { get; set; }
        public Library Library { get; set; }

        public MediaTypes EntryType { get; set; }

        public int? TMDB_Id { get; set; }


        public int? TMDB_EntryId { get; set; }
        public TMDB_Entry TMDB_Entry { get; set; }

        public DateTime? TMDB_Updated { get; set; }

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


        [MaxLength(MAX_SEARCH_TITLE_SIZE)]
        public string SearchTitle { get; set; }







        public List<string> ExtraSearchTerms { get; set; } = [];

        public List<PlaylistItem> PlaylistItems { get; set; } = [];

        public List<ProfileMediaProgress> ProfileMediaProgress { get; set; } = [];

        public List<Subscription> Subscriptions { get; set; } = [];

        public List<WatchlistItem> WatchlistItems { get; set; } = [];

        public List<TitleOverride> TitleOverrides { get; set; } = [];





        public string FormattedTitle()
        {
            string ret = Title;
            if (EntryType == MediaTypes.Movie && Date.HasValue)
                ret += $" ({Date.Value.Year})";
            return ret;
        }


        public Genres GetGenreFlags()
        {
            var ret = Genres.Unknown;
            if (Genre_Action) ret |= Genres.Action;
            if (Genre_Adventure) ret |= Genres.Adventure;
            if (Genre_Animation) ret |= Genres.Animation;
            if (Genre_Anime) ret |= Genres.Anime;
            if (Genre_Awards_Show) ret |= Genres.Awards_Show;
            if (Genre_Children) ret |= Genres.Children;
            if (Genre_Comedy) ret |= Genres.Comedy;
            if (Genre_Crime) ret |= Genres.Crime;
            if (Genre_Documentary) ret |= Genres.Documentary;
            if (Genre_Drama) ret |= Genres.Drama;
            if (Genre_Family) ret |= Genres.Family;
            if (Genre_Fantasy) ret |= Genres.Fantasy;
            if (Genre_Food) ret |= Genres.Food;
            if (Genre_Game_Show) ret |= Genres.Game_Show;
            if (Genre_History) ret |= Genres.History;
            if (Genre_Home_and_Garden) ret |= Genres.Home_and_Garden;
            if (Genre_Horror) ret |= Genres.Horror;
            if (Genre_Indie) ret |= Genres.Indie;
            if (Genre_Martial_Arts) ret |= Genres.Martial_Arts;
            if (Genre_Mini_Series) ret |= Genres.Mini_Series;
            if (Genre_Music) ret |= Genres.Music;
            if (Genre_Musical) ret |= Genres.Musical;
            if (Genre_Mystery) ret |= Genres.Mystery;
            if (Genre_News) ret |= Genres.News;
            if (Genre_Podcast) ret |= Genres.Podcast;
            if (Genre_Political) ret |= Genres.Political;
            if (Genre_Reality) ret |= Genres.Reality;
            if (Genre_Romance) ret |= Genres.Romance;
            if (Genre_Science_Fiction) ret |= Genres.Science_Fiction;
            if (Genre_Soap) ret |= Genres.Soap;
            if (Genre_Sports) ret |= Genres.Sports;
            if (Genre_Suspense) ret |= Genres.Suspense;
            if (Genre_Talk_Show) ret |= Genres.Talk_Show;
            if (Genre_Thriller) ret |= Genres.Thriller;
            if (Genre_Travel) ret |= Genres.Travel;
            if (Genre_TV_Movie) ret |= Genres.TV_Movie;
            if (Genre_War) ret |= Genres.War;
            if (Genre_Western) ret |= Genres.Western;

            return ret;
        }


        private void SetGenreFlags(Genres? genres)
        {
            Genre_Action = genres?.HasFlag(Genres.Action) ?? false;
            Genre_Adventure = genres?.HasFlag(Genres.Adventure) ?? false;
            Genre_Animation = genres?.HasFlag(Genres.Animation) ?? false;
            Genre_Anime = genres?.HasFlag(Genres.Anime) ?? false;
            Genre_Awards_Show = genres?.HasFlag(Genres.Awards_Show) ?? false;
            Genre_Children = genres?.HasFlag(Genres.Children) ?? false;
            Genre_Comedy = genres?.HasFlag(Genres.Comedy) ?? false;
            Genre_Crime = genres?.HasFlag(Genres.Crime) ?? false;
            Genre_Documentary = genres?.HasFlag(Genres.Documentary) ?? false;
            Genre_Drama = genres?.HasFlag(Genres.Drama) ?? false;
            Genre_Family = genres?.HasFlag(Genres.Family) ?? false;
            Genre_Fantasy = genres?.HasFlag(Genres.Fantasy) ?? false;
            Genre_Food = genres?.HasFlag(Genres.Food) ?? false;
            Genre_Game_Show = genres?.HasFlag(Genres.Game_Show) ?? false;
            Genre_History = genres?.HasFlag(Genres.History) ?? false;
            Genre_Home_and_Garden = genres?.HasFlag(Genres.Home_and_Garden) ?? false;
            Genre_Horror = genres?.HasFlag(Genres.Horror) ?? false;
            Genre_Indie = genres?.HasFlag(Genres.Indie) ?? false;
            Genre_Martial_Arts = genres?.HasFlag(Genres.Martial_Arts) ?? false;
            Genre_Mini_Series = genres?.HasFlag(Genres.Mini_Series) ?? false;
            Genre_Music = genres?.HasFlag(Genres.Music) ?? false;
            Genre_Musical = genres?.HasFlag(Genres.Musical) ?? false;
            Genre_Mystery = genres?.HasFlag(Genres.Mystery) ?? false;
            Genre_News = genres?.HasFlag(Genres.News) ?? false;
            Genre_Podcast = genres?.HasFlag(Genres.Podcast) ?? false;
            Genre_Political = genres?.HasFlag(Genres.Political) ?? false;
            Genre_Reality = genres?.HasFlag(Genres.Reality) ?? false;
            Genre_Romance = genres?.HasFlag(Genres.Romance) ?? false;
            Genre_Science_Fiction = genres?.HasFlag(Genres.Science_Fiction) ?? false;
            Genre_Soap = genres?.HasFlag(Genres.Soap) ?? false;
            Genre_Sports = genres?.HasFlag(Genres.Sports) ?? false;
            Genre_Suspense = genres?.HasFlag(Genres.Suspense) ?? false;
            Genre_Talk_Show = genres?.HasFlag(Genres.Talk_Show) ?? false;
            Genre_Thriller = genres?.HasFlag(Genres.Thriller) ?? false;
            Genre_Travel = genres?.HasFlag(Genres.Travel) ?? false;
            Genre_TV_Movie = genres?.HasFlag(Genres.TV_Movie) ?? false;
            Genre_War = genres?.HasFlag(Genres.War) ?? false;
            Genre_Western = genres?.HasFlag(Genres.Western) ?? false;
        }


        public void ComputeHash()
        {
            if (EntryType == MediaTypes.Episode)
                Hash = Crypto.HashEpisode(LinkedToId.Value, Season.Value, Episode.Value);

            if (EntryType == MediaTypes.Movie)
                Hash = Crypto.HashMovieTitle(Title, Date.Value.Year);

            if (EntryType == MediaTypes.Series)
                Hash = Crypto.NormalizedHash(Title);
        }



        private void UpdateFromTMDB(TMDB_Entry info)
        {
            if (info == null)
                return;

            if (!(EntryType == MediaTypes.Movie || EntryType == MediaTypes.Series))
            {
                TMDB_EntryId = null;
                Popularity = null;
                return;
            }

            TMDB_EntryId = info.Id;
            Popularity = info.Popularity;

            if (EntryType == MediaTypes.Movie)
            {
                if (MovieRating == null || MovieRating == MovieRatings.None)
                    MovieRating = info.MovieRating;
            }
            else
            {
                if (TVRating == null || TVRating == TVRatings.None)
                    TVRating = info.TVRating;
            }

            if (Description.IsNullOrWhiteSpace())
                Description = info.Description;

            if (BackdropUrl.IsNullOrWhiteSpace())
                BackdropUrl = info.BackdropUrl;
        }

        private void ComputeXid()
        {
            if (EntryType != MediaTypes.Episode)
            {
                Xid = null;
                return;
            }

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


        private void SetExraSearchTerms(List<string> newTerms)
        {
            ExtraSearchTerms ??= [];

            if (!(EntryType == MediaTypes.Movie || EntryType == MediaTypes.Series))
            {
                ExtraSearchTerms.Clear();
                return;
            }

            ExtraSearchTerms = (newTerms ?? [])
                .Where(term => term.HasValue())
                .Select(term => term.ToLower().Trim())
                .Distinct()
                .ToList();
        }


        /// <summary>
        /// Call this AFTER <see cref="SetExraSearchTerms(List{string})"/>
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void CreateSearchTitle()
        {
            if (!(EntryType == MediaTypes.Movie || EntryType == MediaTypes.Series))
            {
                SearchTitle = null;
                return;
            }

            if (Title.IsNullOrWhiteSpace())
                throw new Exception("Title must have a value before creating the search title");


            var terms = FormattedTitle().NormalizedQueryString().Tokenize();

            //This handles variations like Spider-Man and Agents of S.H.I.E.L.D.
            terms.AddRange
                (
                   Regex.Replace(FormattedTitle(), "[^\\w]", " ", RegexOptions.Compiled)
                        .NormalizedQueryString()
                        .Tokenize()
                        .Distinct()
                );

            //Do the same for extra search terms
            if (ExtraSearchTerms != null)
            {
                terms.AddRange
                    (
                        ExtraSearchTerms.SelectMany(item =>
                           item.Trim()
                            .NormalizedQueryString()
                            .Tokenize()
                            .Distinct()
                    ));


                terms.AddRange
                    (
                        ExtraSearchTerms.SelectMany(item =>
                            Regex.Replace(item, "[^\\w]", " ", RegexOptions.Compiled)
                            .Trim()
                            .NormalizedQueryString()
                            .Tokenize()
                            .Distinct()
                    ));
            }

            terms = terms
                .Where(item => item.HasValue())
                .Distinct()
                .ToList();


            string st = FormattedTitle() + " " + string.Join(" ", terms);
            if (st.Length > MAX_SEARCH_TITLE_SIZE)
                st = st[..MAX_SEARCH_TITLE_SIZE];

            SearchTitle = st;
        }



        



        /// <summary>
        /// Call this after setting all other fields
        /// </summary>
        public void SetComputedInfo(List<string> extraSearchTerms, Genres? genres, TMDB_Entry tmdbInfo)
        {
            SetExraSearchTerms(extraSearchTerms);
            UpdateFromTMDB(tmdbInfo);
            SetGenreFlags(genres);
            CreateSearchTitle();
            ComputeXid();
            ComputeHash();
        }

    }
}
