using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(AccountId), nameof(Name), IsUnique = true)]
    [Index(nameof(AccountId), IsUnique = false)]
    public class Profile : IComparable
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        public bool IsMain { get; set; }

        public short? PinNumber { get; set; }

        public TitleRequestPermissions TitleRequestPermission { get; set; }

        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string AvatarUrl { get; set; }

        public bool Locked { get; set; }

        public List<ProfileLibraryShare> ProfileLibraryShares { get; set; }

        public List<GetRequest> GetRequests { get; set; }

        public List<Notification> Notifications { get; set; }

        public List<Playlist> Playlists { get; set; }

        public List<ProfileMediaProgress> ProfileMediaProgress { get; set; }

        public List<WatchlistItem> WatchlistItems { get; set; }

        public List<Subscription> Subscriptions { get; internal set; }

        public List<TitleOverride> TitleOverrides { get; set; }

        public List<FCMToken> FCMTokens { get; set; }

        public MovieRatings MaxMovieRating { get; set; }

        public TVRatings MaxTVRating { get; set; }

        public int CompareTo(object obj)
        {
            var comp = (Profile)obj;
            int ret = -IsMain.CompareTo(comp.IsMain);
            if (ret == 0)
                ret = Name.CompareTo(comp.Name);
            return ret;
        }

        public static string CalculateS3Key(int Id) => $"{Constants.DEFAULT_PROFILE_PATH}/{Id}.jpg";

        public static string CalculateS3Url(int Id) => $"{Constants.DEFAULT_PROFILE_URL_ROOT}/{Id}.jpg";

    }
}
