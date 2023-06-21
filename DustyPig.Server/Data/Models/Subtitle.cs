using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(MediaEntryId), nameof(Name), IsUnique = true)]
    [Index(nameof(MediaEntryId), IsUnique = false)]
    public class Subtitle : IComparable
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(Constants.MAX_NAME_LENGTH)]
        public string Name { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        [Required]
        [MaxLength(Constants.MAX_URL_LENGTH)]
        public string Url { get; set; }

        public int CompareTo(object obj)
        {
            return Name.CompareTo(((Subtitle)obj).Name);
        }
    }
}
