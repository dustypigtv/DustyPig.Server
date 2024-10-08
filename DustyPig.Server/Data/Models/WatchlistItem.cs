﻿using Microsoft.EntityFrameworkCore;
using System;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(ProfileId), nameof(MediaEntryId))]
    public class WatchlistItem
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public DateTime Added { get; set; }
    }
}
