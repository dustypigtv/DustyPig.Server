using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(FirebaseId), IsUnique = true)]
    public class Account
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string FirebaseId { get; set; }

        public List<Profile> Profiles { get; set; } = new List<Profile>();

        public List<Library> Libraries { get; set; } = new List<Library>();

        public List<GetRequest> GetRequests { get; set; } = new List<GetRequest>();

        public List<AccountToken> AccountTokens { get; set; } = new List<AccountToken>();

        //EF Core can't seem to handle this
        //public List<Friendship> Friendships { get; set; }
    }
}

