using Microsoft.EntityFrameworkCore;

namespace DustyPig.Server.Data.Models
{
    [PrimaryKey(nameof(PlaylistId), nameof(MediaEntryId))]
    public class AutoPlaylistSeries
    {
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; }

        public int MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; }
    }
}
