using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace DustyPig.Server.Data.Models
{
    public enum Roles : int
    {
        Cast = 1,
        Director = 2,
        Producer = 3,
        Writer = 4
    }

    [Index(nameof(MediaEntryId), IsUnique = false)]
    [Index(nameof(PersonId), IsUnique = false)]
    public class MediaPersonBridge : IEquatable<MediaPersonBridge>
    {
        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int PersonId { get; set; }
        public Person Person { get; set; }

        public Roles Role { get; set; }

        public int SortOrder { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as MediaPersonBridge);
        }

        public bool Equals(MediaPersonBridge other)
        {
            return other is not null &&
                   MediaEntryId == other.MediaEntryId &&
                   PersonId == other.PersonId &&
                   Role == other.Role;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MediaEntryId, PersonId, Role);
        }

        public static bool operator ==(MediaPersonBridge left, MediaPersonBridge right)
        {
            return EqualityComparer<MediaPersonBridge>.Default.Equals(left, right);
        }

        public static bool operator !=(MediaPersonBridge left, MediaPersonBridge right)
        {
            return !(left == right);
        }
    }
}
