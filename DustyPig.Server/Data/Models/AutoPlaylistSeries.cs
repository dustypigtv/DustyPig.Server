using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [Index(nameof(PlaylistId), IsUnique = false)]
    [Index(nameof(MediaEntryId), IsUnique = false)]
    [PrimaryKey(nameof(PlaylistId), nameof(MediaEntryId))]
    public class AutoPlaylistSeries
    {
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }
    }
}
