﻿using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(ProfileId), nameof(AccountId), nameof(EntryType), nameof(TMDB_Id))]
    public class GetRequest
    {
        public int Id { get; set; }

        /// <summary>
        /// ProfileId of requestor
        /// </summary>
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }


        /// <summary>
        /// AccountId that request is sent to
        /// </summary>
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public MediaTypes EntryType { get; set; }

        public int TMDB_Id { get; set; }

        public RequestStatus ParentalStatus { get; set; }

        public RequestStatus Status { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
