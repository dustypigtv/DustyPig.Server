using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(AccountId), nameof(Name), IsUnique = true)]
    public class Library : IComparable
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        public bool IsTV { get; set; }

        public List<FriendLibraryShare> FriendLibraryShares { get; set; }

        public List<ProfileLibraryShare> ProfileLibraryShares { get; set; }

        public List<MediaEntry> MediaEntries { get; set; }

        public int CompareTo(object obj) => Name.CompareTo(((Library)obj).Name);

        public override string ToString() => Name;
    }
}
