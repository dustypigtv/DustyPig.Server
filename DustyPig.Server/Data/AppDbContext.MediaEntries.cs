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
            .Where(item => item.AccountId == account.Id);

        public IQueryable<Library> FriendLibrariesSharedWithProfile(Account account, Profile profile) =>
            from library in Libraries
            join share in ProfilLibrarySharesForProfile(profile) on library.Id equals share.LibraryId
            where library.AccountId != account.Id
            select library;

        /// <summary>
        /// Libraries shared with the account
        /// </summary>
        public IQueryable<FriendLibraryShare> FriendLibrarySharesForAccount(Account account) =>
            FriendLibraryShares
            .Include(item => item.Friendship)
            .Where(item => item.Friendship.Account1Id == account.Id || item.Friendship.Account2Id == account.Id);



        /// <summary>
        /// All libraries that have been shared with the profile
        /// </summary>
        public IQueryable<ProfileLibraryShare> ProfilLibrarySharesForProfile(Profile profile) =>
            ProfileLibraryShares
            .Where(item => item.ProfileId == profile.Id);



        /// <summary>
        /// All title overrides that have been granted to the profile
        /// </summary>
        public IQueryable<Models.TitleOverride> TitleOverridesForProfile(Profile profile) =>
            TitleOverrides
            .Where(item => item.ProfileId == profile.Id);





        /// <summary>
        /// All media than can be searched by the specified profile.
        /// </summary>
        public IQueryable<MediaEntry> MediaEntriesSearchableByProfile(Account account, Profile profile)
        {
            /*
                If the profile is the main profile, or is allowed to request titles
                    return all media the profile within the possible access path
                else
                    return all media in libraries shared with profile, withing allowed ratings or with an override
            */


            if (profile.IsMain || profile.TitleRequestPermission != TitleRequestPermissions.Disabled)
            {
                var libs =
                    from library in Libraries
                    join share in ProfileLibraryShares.Include(item => item.Library) on library.Id equals share.LibraryId into LJ
                    from share in LJ.DefaultIfEmpty()
                    where library.AccountId == account.Id ||
                    (
                        share.Library.AccountId != account.Id &&
                        share.ProfileId == profile.Id
                    )
                    select library.Id;

                return
                    from mediaEntry in MediaEntries

                    join libid in libs on mediaEntry.LibraryId equals libid

                    join titleOverride in TitleOverridesForProfile(profile)
                        on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLeftJoin
                    from titleOverride in titleOverridesLeftJoin.DefaultIfEmpty()

                    where
                        titleOverride == null
                        ||
                        titleOverride.State == OverrideState.Allow
                        
                    select mediaEntry;
            }
            else
            {
                return
                    from mediaEntry in MediaEntries

                    join library in Libraries on mediaEntry.LibraryId equals library.Id

                    join share in ProfilLibrarySharesForProfile(profile) on library.Id equals share.LibraryId

                    join titleOverride in TitleOverridesForProfile(profile)
                        on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLeftJoin
                    from titleOverride in titleOverridesLeftJoin.DefaultIfEmpty()

                    where                       
                        profile.AllowedRatings == Ratings.All 
                        ||
                        (
                            mediaEntry.Rated.HasValue 
                            &&
                            (profile.AllowedRatings & mediaEntry.Rated) == mediaEntry.Rated
                        )
                        ||
                        titleOverride.State == OverrideState.Allow
                                              
                    select mediaEntry;
            }
        }

        public IQueryable<MediaEntry> MoviesSearchableByProfile(Account account, Profile profile) =>
            MediaEntriesSearchableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Movie);

        public IQueryable<MediaEntry> SeriesSearchableByProfile(Account account, Profile profile) =>
            MediaEntriesSearchableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Series);







        /// <summary>
        /// All media that can be played by the spcified profile
        /// </summary>
        public IQueryable<MediaEntry> MediaEntriesPlayableByProfile(Account account, Profile profile)
        {
            /*
                If the profile is the main profile
                    return all media the profile within the possible access path
                else
                    return all media in libraries shared with profile, withing allowed ratings or with an override
            */

            if (profile.IsMain)
            {
                var libs = 
                    from library in Libraries
                    join share in ProfileLibraryShares.Include(item => item.Library) on library.Id equals share.LibraryId into LJ
                    from share in LJ.DefaultIfEmpty()
                    where library.AccountId == account.Id || 
                    (
                        share.Library.AccountId != account.Id &&
                        share.ProfileId == profile.Id
                    )
                    select library.Id;

                return
                    from mediaEntry in MediaEntries
                    join libid in libs on mediaEntry.LibraryId equals libid
                    select mediaEntry;
            }
            else
            {
                return
                    from mediaEntry in MediaEntries

                    join library in Libraries on mediaEntry.LibraryId equals library.Id

                    join share in ProfilLibrarySharesForProfile(profile) on library.Id equals share.LibraryId

                    join titleOverride in TitleOverridesForProfile(profile)
                        on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLeftJoin
                    from titleOverride in titleOverridesLeftJoin.DefaultIfEmpty()

                    where
                        profile.AllowedRatings == Ratings.All
                        ||
                        (
                            mediaEntry.Rated.HasValue
                            &&
                            (profile.AllowedRatings & mediaEntry.Rated) == mediaEntry.Rated
                        )
                        ||
                        titleOverride.State == OverrideState.Allow

                    select mediaEntry;
            }
        }

        public IQueryable<MediaEntry> MoviesAndSeriesPlayableByProfile(Account account, Profile profile) =>
            MediaEntriesPlayableByProfile(account, profile)
            .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType));

        public IQueryable<MediaEntry> MoviesPlayableByProfile(Account account, Profile profile) =>
            MediaEntriesPlayableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Movie);

        public IQueryable<MediaEntry> SeriesPlayableByProfile(Account account, Profile profile) =>
            MediaEntriesPlayableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Series);

        public IQueryable<MediaEntry> EpisodesPlayableByProfile(Account account, Profile profile) =>
            MediaEntriesPlayableByProfile(account, profile)
            .Where(item => item.EntryType == MediaTypes.Episode);

        public IQueryable<ProfileMediaProgress> MediaProgress(Profile profile) =>
            ProfileMediaProgresses
            .Where(item => item.ProfileId == profile.Id);

    }
}
