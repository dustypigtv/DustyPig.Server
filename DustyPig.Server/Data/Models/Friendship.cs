using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    /// <summary>
    /// Invites ALWAYS go from Account1 to Account2
    /// </summary>
    [Index(nameof(Hash), IsUnique = true)]
    public class Friendship
    {
        public int Id { get; set; }

        public int Account1Id { get; set; }
        public Account Account1 { get; set; }

        public int Account2Id { get; set; }
        public Account Account2 { get; set; }

        /// <summary>
        /// MD5(string.Join("+", new List&lt;int&gt;{id1, id2}.OrderBy(x => x))
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string Hash { get; set; }

        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string DisplayName1 { get; set; }

        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string DisplayName2 { get; set; }

        public bool Accepted { get; set; }

        public bool NotificationCreated { get; set; }

        public DateTime Timestamp { get; set; }

        public List<FriendLibraryShare> FriendLibraryShares { get; set; }
    }
}
