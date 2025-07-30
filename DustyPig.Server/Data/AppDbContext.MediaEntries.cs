using Amazon;
using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Profile = DustyPig.Server.Data.Models.Profile;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext
    {
        public IQueryable<MediaEntry> TopLevelWatchableMediaByProfileQuery(Profile profile)
        {
            return this.MediaEntries
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
                .Where(m =>

                    m.TitleOverrides
                        .Where(t => t.ProfileId == profile.Id)
                        .Where(t => t.State == OverrideState.Allow)
                        .Any()
                    ||
                    (
                        profile.IsMain
                        &&
                        (
                            m.Library.AccountId == profile.AccountId
                            ||
                            (
                                m.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == profile.AccountId || f.Friendship.Account2Id == profile.AccountId)
                                && !m.TitleOverrides
                                    .Where(t => t.ProfileId == profile.Id)
                                    .Where(t => t.State == OverrideState.Block)
                                    .Any()
                            )
                        )
                    )
                    ||
                    (
                        m.Library.ProfileLibraryShares.Any(p => p.ProfileId == profile.Id)
                        &&
                        (
                            (
                                m.EntryType == MediaTypes.Movie
                                && profile.MaxMovieRating >= (m.MovieRating ?? MovieRatings.Unrated)
                            )
                            ||
                            (
                                m.EntryType == MediaTypes.Series
                                && profile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                            )
                        )
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == profile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                );
        }


        public IQueryable<MediaEntry> WatchableMoviesByProfileQuery(Profile profile)
        {
            return this.MediaEntries
                .Where(m => m.EntryType == MediaTypes.Movie)
                .Where(m =>

                    m.TitleOverrides
                        .Where(t => t.ProfileId == profile.Id)
                        .Where(t => t.State == OverrideState.Allow)
                        .Any()
                    ||
                    (
                        profile.IsMain
                        &&
                        (
                            m.Library.AccountId == profile.AccountId
                            ||
                            (
                                m.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == profile.AccountId || f.Friendship.Account2Id == profile.AccountId)
                                && !m.TitleOverrides
                                    .Where(t => t.ProfileId == profile.Id)
                                    .Where(t => t.State == OverrideState.Block)
                                    .Any()
                            )
                        )
                    )
                    ||
                    (
                        m.Library.ProfileLibraryShares.Any(p => p.ProfileId == profile.Id)
                        && profile.MaxMovieRating >= (m.MovieRating ?? MovieRatings.Unrated)
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == profile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                );
        }


        public IQueryable<MediaEntry> WatchableSeriesByProfileQuery(Profile profile)
        {
            return this.MediaEntries
                .Where(m => m.EntryType == MediaTypes.Series)
                .Where(m =>

                    m.TitleOverrides
                        .Where(t => t.ProfileId == profile.Id)
                        .Where(t => t.State == OverrideState.Allow)
                        .Any()
                    ||
                    (
                        profile.IsMain
                        &&
                        (
                            m.Library.AccountId == profile.AccountId
                            ||
                            (
                                m.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == profile.AccountId || f.Friendship.Account2Id == profile.AccountId)
                                && !m.TitleOverrides
                                    .Where(t => t.ProfileId == profile.Id)
                                    .Where(t => t.State == OverrideState.Block)
                                    .Any()
                            )
                        )
                    )
                    ||
                    (
                        m.Library.ProfileLibraryShares.Any(p => p.ProfileId == profile.Id)
                        && profile.MaxTVRating >= (m.TVRating ?? TVRatings.NotRated)
                        && !m.TitleOverrides
                            .Where(t => t.ProfileId == profile.Id)
                            .Where(t => t.State == OverrideState.Block)
                            .Any()
                    )
                );
        }

        public async Task<List<int>> ProfilesWithAccessToLibraryAndRating(int libraryId, MovieRatings rating)
        {
            List<int> ret = [];

            var lib = await this.Libraries
                .AsNoTracking()
                .Include(l => l.Account)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(l => l.FriendLibraryShares)
                .ThenInclude(f => f.Friendship)
                .ThenInclude(f => f.Account1)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(l => l.FriendLibraryShares)
                .ThenInclude(f => f.Friendship)
                .ThenInclude(f => f.Account2)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(l => l.ProfileLibraryShares)
                .ThenInclude(pls => pls.Profile)
                .Where(l => !l.IsTV)
                .FirstOrDefaultAsync();

            if (lib == null)
                return ret;

            ret.Add(lib.Account.Profiles.First(_ => _.IsMain).Id);

            foreach (var friendship in lib.FriendLibraryShares.Select(_ => _.Friendship))
            {
                int id = friendship.Account1.Profiles.First(_ => _.IsMain).Id;
                if (!ret.Contains(id))
                    ret.Add(id);

                id = friendship.Account2.Profiles.First(_ => _.IsMain).Id;
                if (!ret.Contains(id))
                    ret.Add(id);
            }

            foreach(var profile in lib.ProfileLibraryShares.Select(_ => _.Profile).Where(_ => _.MaxMovieRating >= rating))
            {
                if (!ret.Contains(profile.Id))
                    ret.Add(profile.Id);
            }

            return ret;
        }




        public async Task<List<int>> ProfilesWithAccessToLibraryAndRating(int libraryId, TVRatings rating)
        {
            List<int> ret = [];

            var lib = await this.Libraries
                .AsNoTracking()
                .Include(l => l.Account)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(l => l.FriendLibraryShares)
                .ThenInclude(f => f.Friendship)
                .ThenInclude(f => f.Account1)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(l => l.FriendLibraryShares)
                .ThenInclude(f => f.Friendship)
                .ThenInclude(f => f.Account2)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(l => l.ProfileLibraryShares)
                .ThenInclude(pls => pls.Profile)
                .Where(l => l.IsTV)
                .FirstOrDefaultAsync();

            if (lib == null)
                return ret;

            ret.Add(lib.Account.Profiles.First(_ => _.IsMain).Id);

            foreach (var friendship in lib.FriendLibraryShares.Select(_ => _.Friendship))
            {
                int id = friendship.Account1.Profiles.First(_ => _.IsMain).Id;
                if (!ret.Contains(id))
                    ret.Add(id);

                id = friendship.Account2.Profiles.First(_ => _.IsMain).Id;
                if (!ret.Contains(id))
                    ret.Add(id);
            }

            foreach (var profile in lib.ProfileLibraryShares.Select(_ => _.Profile).Where(_ => _.MaxTVRating >= rating))
            {
                if (!ret.Contains(profile.Id))
                    ret.Add(profile.Id);
            }

            return ret;
        }


        public async Task<List<int>> ProfilesWithAccessToTopLevel(int mediaId)
        {
            List<int> ret = [];

            var mediaEntry = await this.MediaEntries
                .AsNoTracking()
                .Include(m => m.Library)
                .ThenInclude(l => l.ProfileLibraryShares)
                .ThenInclude(pls => pls.Profile)
                //.ThenInclude(p => p.Account)
                //.ThenInclude(a => a.Profiles)
                .Include(m => m.Library)
                .ThenInclude(l => l.Account)
                .ThenInclude(a => a.Profiles.Where(p => p.IsMain))
                .Include(m => m.Library)
                .ThenInclude(l => l.FriendLibraryShares)
                .ThenInclude(f => f.Friendship)
                .Include(m => m.TitleOverrides)
                .Where(m => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(m.EntryType))
                .Where(m => m.Id == mediaId)
                .FirstOrDefaultAsync();

            if (mediaEntry == null)
                return ret;

            ret.Add(mediaEntry.Library.Account.Profiles.First(_ => _.IsMain).Id);

            foreach (var pls in mediaEntry.Library.ProfileLibraryShares)
            {
                if (ret.Contains(pls.ProfileId))
                    continue;

                bool add = mediaEntry.TitleOverrides
                    .Where(t => t.ProfileId == pls.ProfileId)
                    .Where(t => t.State == OverrideState.Allow)
                    .Any();

                if (!add)
                    add = pls.Profile.IsMain 
                        && mediaEntry.Library.FriendLibraryShares.Any(f => f.Friendship.Account1Id == pls.Profile.AccountId || f.Friendship.Account2Id == pls.Profile.AccountId)
                        && !mediaEntry.TitleOverrides.Where(t => t.ProfileId == pls.ProfileId).Where(t => t.State == OverrideState.Block).Any();

                if (!add)
                    add = mediaEntry.Library.ProfileLibraryShares.Any(p => p.ProfileId == pls.ProfileId)
                        && pls.Profile.MaxMovieRating >= (mediaEntry.MovieRating ?? MovieRatings.Unrated)
                        && !mediaEntry.TitleOverrides
                            .Where(t => t.ProfileId == pls.ProfileId)
                            .Where(t => t.State == OverrideState.Block)
                            .Any();

                if (add)
                    ret.Add(pls.ProfileId);
            }

            return ret;
        }
    }
}
