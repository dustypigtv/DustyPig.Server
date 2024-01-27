﻿using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public enum NotificationType
    {
        /// <summary>
        /// Movie or episode available
        /// </summary>
        Media = 1,

        /// <summary>
        /// Friendship status notification
        /// </summary>
        Friend = 2,

        /// <summary>
        /// Wants or granted access to existing media
        /// </summary>
        OverrideRequest = 3,

        /// <summary>
        /// Request to get new media
        /// </summary>
        GetRequest = 4
    }

    public class Notification
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        [MaxLength(1000)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Message { get; set; }

        public NotificationType NotificationType { get; set; }

        public int? FriendshipId { get; set; }
        public Friendship Friendship { get; set; }

        public int? MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int? GetRequestId { get; set; }
        public GetRequest GetRequest { get; set; }

        public int? TitleOverrideId { get; set; }
        public TitleOverride TitleOverride { get; set; }


        public bool Sent { get; set; }

        public bool Seen { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
