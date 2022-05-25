using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext
    {
        /*
            This logic is both complex and used in multiple places, so just make it part AppData
        */


        /// <summary>
        /// Libraries owned by the account
        /// </summary>
        public IQueryable<Library> LibrariesForAccount(Account account) =>
            Libraries
            .AsNoTracking()
            .Where(item => item.AccountId == account.Id)
            .Distinct();


        /// <summary>
        /// Libraries shared with the account
        /// </summary>
        public IQueryable<FriendLibraryShare> FriendLibraryForAccount(Account account) =>
            FriendLibraryShares
            .AsNoTracking()
            .Include(item => item.Friendship)
            .Where(item => item.Friendship.Account1Id == account.Id || item.Friendship.Account2Id == account.Id)
            .Distinct();



        /// <summary>
        /// All libraries that have been shared with the profile
        /// </summary>
        public IQueryable<ProfileLibraryShare> LibrariesForProfile(Profile profile) =>
            ProfileLibraryShares
            .AsNoTracking()
            .Include(item => item.Profile)
            .Where(item => item.ProfileId == profile.Id)
            .Distinct();



        /// <summary>
        /// All title overrides that have been granted to the profile
        /// </summary>
        public IQueryable<Models.TitleOverride> TitleOverrideForProfile(Profile profile) =>
            TitleOverrides
            .AsNoTracking()
            .Where(item => item.ProfileId == profile.Id)
            .Distinct();





        /// <summary>
        /// All media than can be searched by the specified profile.
        /// </summary>
        public IQueryable<MediaEntry> MediaEntriesSearchableByProfile(Account account, Profile profile)
        {
            /*
                If the profile is the main profile, or is allowed to request titles
                    return all media the profile within the possible access path
                else
                    return all media in libraries shared with profile, withing allowed ratings

                In both cases, also return any media that an override has been granted for
            */


            var mediaEntriesQ = MediaEntries
                .AsNoTracking()
                .Distinct();


            if (profile.IsMain || profile.TitleRequestPermission != TitleRequestPermissions.Disabled)
            {
                mediaEntriesQ =
                    from mediaEntry in mediaEntriesQ

                    join library in LibrariesForAccount(account)
                        on mediaEntry.LibraryId equals library.Id into libraryLeftJoin
                    from library in libraryLeftJoin.DefaultIfEmpty()

                    join friendLibraryShare in FriendLibraryForAccount(account)
                        on mediaEntry.LibraryId equals friendLibraryShare.LibraryId into friendLibraryShareLeftJoin
                    from friendLibraryShare in friendLibraryShareLeftJoin.DefaultIfEmpty()

                    join titleOverride in TitleOverrideForProfile(profile)
                        on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLeftJoin
                    from titleOverride in titleOverridesLeftJoin.DefaultIfEmpty()

                    where
                        (
                            titleOverride != null &&
                            titleOverride.State == OverrideState.Allow
                        ) ||
                        library != null ||
                        friendLibraryShare != null
                    select mediaEntry;
            }
            else
            {
                mediaEntriesQ =
                    from mediaEntry in mediaEntriesQ

                    join profileLibraryShare in LibrariesForProfile(profile)
                        on mediaEntry.LibraryId equals profileLibraryShare.LibraryId into profileLibraryShareLeftJoin
                    from profileLibraryShare in profileLibraryShareLeftJoin.DefaultIfEmpty()

                    join titleOverride in TitleOverrideForProfile(profile)
                        on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLeftJoin
                    from titleOverride in titleOverridesLeftJoin.DefaultIfEmpty()

                    where
                        (
                            profileLibraryShare != null &&
                            (
                                profileLibraryShare.Profile.AllowedRatings == Ratings.All ||
                                (
                                    mediaEntry.Rated.HasValue &&
                                    (profileLibraryShare.Profile.AllowedRatings & mediaEntry.Rated) == mediaEntry.Rated
                                )
                            )
                        ) ||
                        (
                            titleOverride != null &&
                            titleOverride.State == OverrideState.Allow
                        )
                    select mediaEntry;
            }

            return mediaEntriesQ;
        }

        public IQueryable<MediaEntry> MoviesSearchableByProfile(Account account, Profile profile) =>
            MediaEntriesSearchableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Movie)
            .Distinct();

        public IQueryable<MediaEntry> SeriesSearchableByProfile(Account account, Profile profile) =>
            MediaEntriesSearchableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Series)
            .Distinct();







        /// <summary>
        /// All media that can be played by the spcified profile
        /// </summary>
        public IQueryable<MediaEntry> MediaEntriesPlayableByProfile(Profile profile)
        {
            /*
                Main profile can play all media in a possible path
                    All owned libraries
                    All libraries shared with account                    
                    Since Main may not want to see all titles, limit to ones in their own profile-library shares and ratings limits

                Non-main can play:
                    Media from all libraries shared with profile, within ratings limits

                
                So combined, it's:
                    All media in profile-library shares, limit by ratings
                    + All title overrides 
            */

            var mediaEntriesQ =
                from mediaEntry in MediaEntries
                    .AsNoTracking()

                join profileLibraryShare in LibrariesForProfile(profile)
                    on mediaEntry.LibraryId equals profileLibraryShare.LibraryId into profileLibraryShareLeftJoin
                from profileLibraryShare in profileLibraryShareLeftJoin.DefaultIfEmpty()

                join titleOverride in TitleOverrideForProfile(profile)
                    on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLeftJoin
                from titleOverride in titleOverridesLeftJoin.DefaultIfEmpty()

                where
                    (
                        profileLibraryShare != null &&
                        (
                            profile.AllowedRatings == Ratings.All ||
                            (
                                mediaEntry.Rated.HasValue &&
                                (profile.AllowedRatings & mediaEntry.Rated.Value) == mediaEntry.Rated
                            )
                        )
                    )
                    ||
                    (
                        titleOverride != null &&
                        titleOverride.State == OverrideState.Allow
                    )
                select mediaEntry;


            return mediaEntriesQ;
        }

        public IQueryable<MediaEntry> MoviesAndSeriesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType))
            .Distinct();

        public IQueryable<MediaEntry> MoviesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Movie)
            .Distinct();

        public IQueryable<MediaEntry> SeriesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Series)
            .Distinct();

        public IQueryable<MediaEntry> EpisodesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Include(item => item.Subtitles)
            .Where(item => item.LinkedToId.HasValue)
            .Where(item => item.Xid.HasValue)
            .Where(item => item.EntryType == MediaTypes.Episode)
            .Where(item => item.Season.HasValue)
            .Where(item => item.Episode.HasValue)
            .Distinct();

        public IQueryable<ProfileMediaProgress> MediaProgress(Profile profile) =>
            ProfileMediaProgresses
            .AsNoTracking()
            .Include(item => item.Profile)
            .Where(item => item.ProfileId == profile.Id)
            .Distinct();

    }
}
