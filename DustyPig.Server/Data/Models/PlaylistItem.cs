using System;

namespace DustyPig.Server.Data.Models
{
    public class PlaylistItem : IComparable<PlaylistItem>
    {
        public int Id { get; set; }

        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }

        public int Index { get; set; }

        public int CompareTo(PlaylistItem other) => Index.CompareTo(other.Index);
    }
}
