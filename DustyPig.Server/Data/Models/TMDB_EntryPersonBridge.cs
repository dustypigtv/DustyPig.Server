using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models
{
    public enum Roles : int
    {
        Cast = 1,
        Director = 2,
        Producer = 3,
        Writer = 4
    }



    [Index(nameof(TMDB_EntryId), IsUnique = false)]
    [Index(nameof(TMDB_PersonId), IsUnique = false)]
    [PrimaryKey(nameof(TMDB_EntryId), nameof(TMDB_PersonId))]
    public class TMDB_EntryPersonBridge : IEquatable<TMDB_EntryPersonBridge>
    {
        public int TMDB_EntryId { get; set; }
        public TMDB_Entry TMDB_Entry { get; set; }

        public int TMDB_PersonId { get; set; }
        public TMDB_Person TMDB_Person { get; set; }

        public Roles Role { get; set; }

        public int SortOrder { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as TMDB_EntryPersonBridge);
        }

        public bool Equals(TMDB_EntryPersonBridge other)
        {
            return other is not null &&
                   TMDB_EntryId == other.TMDB_EntryId &&
                   TMDB_PersonId == other.TMDB_PersonId &&
                   Role == other.Role &&
                   SortOrder == other.SortOrder;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TMDB_EntryId, TMDB_PersonId, Role, SortOrder);
        }

        public static bool operator ==(TMDB_EntryPersonBridge left, TMDB_EntryPersonBridge right)
        {
            return EqualityComparer<TMDB_EntryPersonBridge>.Default.Equals(left, right);
        }

        public static bool operator !=(TMDB_EntryPersonBridge left, TMDB_EntryPersonBridge right)
        {
            return !(left == right);
        }
    }
}
