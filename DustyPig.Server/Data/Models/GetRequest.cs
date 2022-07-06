using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(AccountId), nameof(EntryType), nameof(TMDB_Id), IsUnique = true)]
    public class GetRequest
    {
        public int Id { get; set; }

        /// <summary>
        /// AccountId that request is sent to
        /// </summary>
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public TMDB_MediaTypes EntryType { get; set; }

        public int TMDB_Id { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.NotRequested;

        public List<GetRequestSubscription> NotificationSubscriptions { get; set; } = new List<GetRequestSubscription>();
    }
}
