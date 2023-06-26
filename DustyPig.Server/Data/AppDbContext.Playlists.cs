using DustyPig.API.v3;
using DustyPig.API.v3.Models;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext
    {
        public async Task UpdatePlaylist(Account UserAccount, Profile UserProfile, int id, string newName = null)
        {
            var playlist = await Playlists
                .AsNoTracking()
                .Include(item => item.PlaylistItems)
                .Where(item => item.Id == id)
                .Where(item => item.ProfileId == UserProfile.Id)
                .FirstOrDefaultAsync();

            if (playlist == null || playlist.PlaylistItems.Count == 0)
                return;

            bool changed = false;
            if (!string.IsNullOrWhiteSpace(newName))
            {
                newName = newName.Trim();
                if (playlist.Name != newName)
                {
                    var otherNames = await Playlists
                        .AsNoTracking()
                        .Where(item => item.Id != id)
                        .Where(item => item.ProfileId == UserProfile.Id)
                        .Select(item => item.Name)
                        .ToListAsync();
                    if (otherNames.Any(item => item.ICEquals(newName)))
                        throw new ModelValidationException("Playlist name already exists");
                    playlist.Name = newName;
                    changed = true;
                }
            }


            var q =
                from me in MediaEntries
                join lib in Libraries on me.LibraryId equals lib.Id
                join pli in PlaylistItems on me.Id equals pli.MediaEntryId

                join fls in FriendLibraryShares
                    .Where(t => t.Friendship.Account1Id == UserAccount.Id || t.Friendship.Account2Id == UserAccount.Id)
                    .Select(t => (int?)t.LibraryId)
                    on lib.Id equals fls into fls_lj
                from fls in fls_lj.DefaultIfEmpty()

                join pls in ProfileLibraryShares
                    on new { LibraryId = lib.Id, ProfileId = UserProfile.Id }
                    equals new { pls.LibraryId, pls.ProfileId }
                    into pls_lj
                from pls in pls_lj.DefaultIfEmpty()

                join ovrride in TitleOverrides
                    on new { MediaEntryId = me.EntryType == MediaTypes.Episode ? me.LinkedToId.Value : me.Id, ProfileId = UserProfile.Id, Valid = true }
                    equals new { ovrride.MediaEntryId, ovrride.ProfileId, Valid = new OverrideState[] { OverrideState.Allow, OverrideState.Block }.Contains(ovrride.State) }
                    into ovrride_lj
                from ovrride in ovrride_lj.DefaultIfEmpty()

                where

                    //Allow to play filters
                    pli.PlaylistId == id
                    && Constants.PLAYABLE_MEDIA_TYPES.Contains(me.EntryType)
                    &&
                    (
                        ovrride.State == OverrideState.Allow
                        ||
                        (
                            UserProfile.IsMain
                            &&
                            (
                                lib.AccountId == UserAccount.Id
                                ||
                                (
                                    fls.HasValue
                                    && ovrride.State != OverrideState.Block
                                )
                            )
                        )
                        ||
                        (
                            pls != null
                            && ovrride.State != OverrideState.Block
                            &&
                            (
                                (
                                    me.EntryType == MediaTypes.Movie
                                    && UserProfile.MaxMovieRating >= (me.MovieRating ?? MovieRatings.NotRated)
                                )
                                ||
                                (
                                    me.EntryType == MediaTypes.Episode
                                    && UserProfile.MaxTVRating >= (me.TVRating ?? TVRatings.NotRated)
                                )
                            )
                        )
                    )

                select me.Id;

            var playableIds = await q
                .Distinct()
                .ToListAsync();


            //Use stubs for updates/deletes. Insane normally, but based on frequency and types of changes that happen here,
            //it ends up being faster

            playlist.PlaylistItems.Sort();
            for (int i = 0; i < playlist.PlaylistItems.Count; i++)
            {
                if (playableIds.Contains(playlist.PlaylistItems[i].MediaEntryId))
                {
                    if (playlist.PlaylistItems[i].Index != i)
                    {
                        //Stub
                        PlaylistItems.Update(new Data.Models.PlaylistItem
                        {
                            Id = playlist.PlaylistItems[i].Id,
                            Index = i,
                            MediaEntryId = playlist.PlaylistItems[i].MediaEntryId,
                            PlaylistId = playlist.Id
                        });

                        if (playlist.CurrentIndex == playlist.PlaylistItems[i].Index)
                            playlist.CurrentIndex = i;
                        playlist.PlaylistItems[i].Index = i;
                        changed = true;
                    }
                }
                else
                {
                    //Stub
                    PlaylistItems.Remove(new Data.Models.PlaylistItem { Id = playlist.PlaylistItems[i].Id });

                    playlist.ArtworkUpdateNeeded = true;
                    playlist.PlaylistItems.Remove(playlist.PlaylistItems[i]);
                    changed = true;
                    i--;
                }
            }

            if (!playlist.PlaylistItems.Any(item => item.Index == playlist.CurrentIndex))
            {
                playlist.CurrentIndex = 0;
                playlist.CurrentProgress = 0;
                changed = true;
            }

            if (changed)
            {
                Playlists.Update(new Data.Models.Playlist
                {
                    Id = playlist.Id,
                    ArtworkUpdateNeeded = playlist.ArtworkUpdateNeeded,
                    ArtworkUrl = playlist.ArtworkUrl,
                    CurrentIndex = playlist.CurrentIndex,
                    CurrentProgress = playlist.CurrentProgress,
                    Name = playlist.Name,
                    ProfileId = playlist.ProfileId
                });

                await SaveChangesAsync();
            }

        }
    }
}
