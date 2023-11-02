using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using System.Linq;

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





    }
}
