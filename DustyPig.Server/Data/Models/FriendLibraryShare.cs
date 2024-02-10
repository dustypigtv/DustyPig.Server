using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(FriendshipId), nameof(LibraryId))]
    public class FriendLibraryShare
    {
        public int LibraryId { get; set; }
        public Library Library { get; set; }

        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string LibraryDisplayName { get; set; }

        public int FriendshipId { get; set; }
        public Friendship Friendship { get; set; }
    }
}
