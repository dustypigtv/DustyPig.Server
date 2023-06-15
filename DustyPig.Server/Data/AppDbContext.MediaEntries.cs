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





        private IQueryable<MediaEntry> MediaEntriesPlayableByMainProfile(Profile profile)
        {
            var sharedLibs =
                (
                    from friend in Friendships
                    join share in FriendLibraryShares on friend.Id equals share.FriendshipId
                    join library in Libraries on share.LibraryId equals library.Id
                    where
                        friend.Account1Id == profile.AccountId || friend.Account2Id == profile.AccountId

                    select library
                );

            var ownedLibs =
                (
                    from library in Libraries
                    where library.AccountId == profile.AccountId
                    select library
                );


            return
                from mediaEntry in MediaEntries

                join sharedLib in sharedLibs on mediaEntry.LibraryId equals sharedLib.Id into sharedLibsLJ
                from sharedLib in sharedLibsLJ.DefaultIfEmpty()

                join ownedLib in ownedLibs on mediaEntry.LibraryId equals ownedLib.Id into ownedLibsLJ
                from ownedLib in ownedLibsLJ.DefaultIfEmpty()

                join titleOverride in TitleOverridesForProfile(profile) on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLJ
                from titleOverride in titleOverridesLJ.DefaultIfEmpty()

                where
                    titleOverride.State == OverrideState.Allow 
                    ||
                    (
                        titleOverride.State != OverrideState.Block
                        &&
                        (
                            sharedLib != null
                            || ownedLib != null
                        )
                    )

                select mediaEntry;
        }



        private IQueryable<MediaEntry> MediaEntriesPlayableBySubProfile(Profile profile)
        {
            return
                from mediaEntry in MediaEntries

                join library in Libraries on mediaEntry.LibraryId equals library.Id

                join share in ProfilLibrarySharesForProfile(profile) on library.Id equals share.LibraryId into shareLJ
                from share in shareLJ.DefaultIfEmpty()

                join titleOverride in TitleOverridesForProfile(profile) on mediaEntry.Id equals titleOverride.MediaEntryId into titleOverridesLJ
                from titleOverride in titleOverridesLJ.DefaultIfEmpty()

                where

                    titleOverride.State == OverrideState.Allow
                    ||
                    (
                        share != null
                        && titleOverride.State != OverrideState.Block
                        &&
                        (
                            profile.AllowedRatings == Ratings.All ||
                            (
                                mediaEntry.Rated.HasValue &&
                                (profile.AllowedRatings & mediaEntry.Rated) == mediaEntry.Rated
                            )
                        )
                    )

                select mediaEntry;
        }




        /// <summary>
        /// All media than can be searched by the specified profile.
        /// </summary>
        public IQueryable<MediaEntry> MediaEntriesSearchableByProfile(Profile profile)
        {
            /*
                If the profile is the main profile, or is allowed to request titles
                    return all media the profile within the possible access path, or with an override
                else
                    return all media in libraries shared with profile (within allowed ratings), or with an override
            */


            if (profile.IsMain || profile.TitleRequestPermission != TitleRequestPermissions.Disabled)
                 return MediaEntriesPlayableByMainProfile(profile);
            else
                return MediaEntriesPlayableBySubProfile(profile);
        }

        public IQueryable<MediaEntry> MoviesSearchableByProfile(Profile profile) =>
            MediaEntriesSearchableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Movie);

        public IQueryable<MediaEntry> SeriesSearchableByProfile(Profile profile) =>
            MediaEntriesSearchableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Series);







        /// <summary>
        /// All media that can be played by the spcified profile
        /// </summary>
        public IQueryable<MediaEntry> MediaEntriesPlayableByProfile(Profile profile)
        {
            /*
                If the profile is the main profile
                    return all media the profile within the possible access path, or with an override
                else
                    return all media in libraries shared with profile (within allowed ratings), or with an override
            */

            if (profile.IsMain)
                return MediaEntriesPlayableByMainProfile(profile);
            else
                return MediaEntriesPlayableBySubProfile(profile);
        }



        public IQueryable<MediaEntry> MoviesAndSeriesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => Constants.TOP_LEVEL_MEDIA_TYPES.Contains(item.EntryType));

        public IQueryable<MediaEntry> MoviesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Movie);

        public IQueryable<MediaEntry> SeriesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Series);

        public IQueryable<MediaEntry> EpisodesPlayableByProfile(Profile profile) =>
            MediaEntriesPlayableByProfile(profile)
            .Where(item => item.EntryType == MediaTypes.Episode);

        public IQueryable<ProfileMediaProgress> MediaProgress(Profile profile) =>
            ProfileMediaProgresses
            .Where(item => item.ProfileId == profile.Id);

    }
}
