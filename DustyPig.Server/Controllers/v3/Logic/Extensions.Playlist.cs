using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Controllers.v3.Logic
{
    public static partial class Extensions
    {
        public static BasicMedia ToBasicMedia(this Data.Models.Playlist @this)
        {
            //pl must have:
            /*
                .Include(item => item.PlaylistItems)
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo) 
            */

            var ret = new BasicMedia
            {
                Id = @this.Id,
                MediaType = MediaTypes.Playlist,
                Title = @this.Name
            };

            List<string> art = new List<string>();

            for (int i = 0; i < @this.PlaylistItems.Count; i++)
            {
                if (@this.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Movie)
                {
                    if (!art.Contains(@this.PlaylistItems[i].MediaEntry.ArtworkUrl))
                    {
                        art.Add(@this.PlaylistItems[i].MediaEntry.ArtworkUrl);
                        if (art.Count > 3)
                            break;
                    }
                }
                else if (@this.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Episode)
                {
                    if (!art.Contains(@this.PlaylistItems[i].MediaEntry.LinkedTo.ArtworkUrl))
                    {
                        art.Add(@this.PlaylistItems[i].MediaEntry.LinkedTo.ArtworkUrl);
                        if (art.Count > 3)
                            break;
                    }
                }
            }

            if (art.Count == 0)
                art.Add(Constants.DEFAULT_PLAYLIST_IMAGE);

            /*
                Clint Grid:
                    1   2
                    3   4
             */
            ret.ArtworkUrl = art[0];

            if(art.Count == 2)
            {
                ret.ArtworkUrl2 = art[1];
                ret.ArtworkUrl3 = art[0];
                ret.ArtworkUrl4 = art[1];
            }

            if(art.Count== 3)
            {
                ret.ArtworkUrl2 = art[1];
                ret.ArtworkUrl3 = art[2];
                ret.ArtworkUrl4 = art[1];
            }

            if(art.Count == 4)
            {
                ret.ArtworkUrl2 = art[1];
                ret.ArtworkUrl3 = art[2];
                ret.ArtworkUrl4 = art[3];
            }

            return ret;
        }

        public static async Task<List<string>> GetArtwork(Data.Models.Playlist pl, Data.Models.Account userAccount, Data.Models.Profile userProfile)
        {
            //pl must have:
            /*
                .Include(item => item.PlaylistItems)
                .ThenInclude(item => item.MediaEntry)
                .ThenInclude(item => item.LinkedTo) 
            */

            var db = new Data.AppDbContext();
            var playable = await db.MoviesAndSeriesPlayableByProfile(userAccount, userProfile)
                .AsNoTracking()
                .Select(item => item.Id)
                .ToListAsync();

            List<string> art = new List<string>();

            for (int i = 0; i < pl.PlaylistItems.Count; i++)
            {
                if (pl.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Movie)
                {
                    if (playable.Contains(pl.PlaylistItems[i].MediaEntryId))
                    {
                        art.Add(pl. PlaylistItems[i].MediaEntry.ArtworkUrl);
                        if (art.Count > 3)
                            break;
                    }
                }
                else if (pl.PlaylistItems[i].MediaEntry.EntryType == MediaTypes.Episode)
                {
                    if (playable.Contains(pl.PlaylistItems[i].MediaEntry.LinkedToId.Value))
                    {
                        art.Add(pl.PlaylistItems[i].MediaEntry.LinkedTo.ArtworkUrl);
                        if (art.Count > 3)
                            break;
                    }
                }
            }

            if (art.Count == 0)
                art.Add(Constants.DEFAULT_PLAYLIST_IMAGE);

            return art;
        }
    }
}
