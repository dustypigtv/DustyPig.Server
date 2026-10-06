using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Data.Models;
using DustyPig.Server.Extensions;
using DustyPig.Server.HostedServices;
using DustyPig.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataFCMToken = DustyPig.Server.Data.Models.FCMToken;
using DataNotifications = DustyPig.Server.Data.Models.Notification;
using DataPlaylistItem = DustyPig.Server.Data.Models.PlaylistItem;
using DataTMDB_Person = DustyPig.Server.Data.Models.TMDB_Person;

namespace DustyPig.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<AccountToken> AccountTokens { get; set; }
    public DbSet<ActivationCode> ActivationCodes { get; set; }
    public DbSet<AutoPlaylistSeries> AutoPlaylistSeries { get; set; }
    public DbSet<DataFCMToken> FCMTokens { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<FriendLibraryShare> FriendLibraryShares { get; set; }
    public DbSet<GetRequest> GetRequests { get; set; }
    public DbSet<GetRequestSubscription> GetRequestSubscriptions { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<MediaEntry> MediaEntries { get; set; }
    public DbSet<DataNotifications> Notifications { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<DataPlaylistItem> PlaylistItems { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<ProfileLibraryShare> ProfileLibraryShares { get; set; }
    public DbSet<ProfileMediaProgress> ProfileMediaProgresses { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<TitleOverride> TitleOverrides { get; set; }
    public DbSet<WatchlistItem> WatchListItems { get; set; }
    public DbSet<TMDB_Entry> TMDB_Entries { get; set; }
    public DbSet<DataTMDB_Person> TMDB_People { get; set; }
    public DbSet<TMDB_EntryPersonBridge> TMDB_EntryPeopleBridges { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Manually set OnDelete = Cascade for tables with optional one-to-many links
        modelBuilder.Entity<ActivationCode>().HasOne(p => p.Profile).WithMany().OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MediaEntry>().HasOne(p => p.LinkedTo).WithMany().OnDelete(DeleteBehavior.Cascade);

        //Set notifications to null
        modelBuilder.Entity<DataNotifications>(e =>
        {
            e.HasOne(p => p.Friendship).WithMany().OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.GetRequest).WithMany().OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.MediaEntry).WithMany().OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.TitleOverride).WithMany().OnDelete(DeleteBehavior.SetNull);
        });

        ////Full Text Search
        //modelBuilder.Entity<MediaEntry>()
        //    .HasIndex(e => e.SearchTitle)
        //    .HasMethod("GIN")
        //    .IsTsVectorExpressionIndex("english");

        //Popularity index
        modelBuilder.Entity<MediaEntry>()
            .HasIndex(e => e.Popularity)
            .HasFilter($"\"{nameof(MediaEntry.Popularity)}\" IS NOT NULL")
            .IsDescending()
            .HasNullSortOrder(Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NullSortOrder.NullsLast);

        //Speed up genre queries
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Action));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Adventure));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Animation));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Anime));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Awards_Show));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Children));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Comedy));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Crime));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Documentary));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Drama));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Family));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Fantasy));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Food));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Game_Show));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_History));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Home_and_Garden));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Horror));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Indie));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Martial_Arts));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Mini_Series));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Music));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Musical));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Mystery));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_News));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Podcast));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Political));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Reality));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Romance));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Science_Fiction));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Soap));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Sports));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Suspense));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Talk_Show));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Thriller));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Travel));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_TV_Movie));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_War));
        CreateGenreIndex(modelBuilder, nameof(MediaEntry.Genre_Western));
    }

    static void CreateGenreIndex(ModelBuilder modelBuilder, string genre)
    {
        modelBuilder.Entity<MediaEntry>()
            .HasIndex(e => e.Popularity, $"IX_MediaEntries_Popularity_{genre}")
            .HasFilter($"\"{nameof(MediaEntry.Popularity)}\" IS NOT NULL AND \"{genre}\" = TRUE")
            .IsDescending()
            .HasNullSortOrder(Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NullSortOrder.NullsLast);
    }













    /// <summary>
    /// This calls <see cref="DbContext.SaveChangesAsync(bool, CancellationToken)"/>
    /// </summary>
    public async Task MarkPlaylistArtworkNeedsupdate(List<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return;

        var playlists = await Playlists
            .Where(_ => ids.Contains(_.Id))
            .ToListAsync(cancellationToken);

        if (playlists.Count == 0)
            return;

        playlists.ForEach(_ => _.ArtworkUpdateNeeded = true);
        await SaveChangesAsync(cancellationToken);
    }






    public async Task<List<int>> GetLibraryIdsAccessableByAccount(int accountId)
    {
        //Libs owned by the account
        var libs = await Libraries
            .AsNoTracking()
            .Where(item => item.AccountId == accountId)
            .Select(item => item.Id)
            .ToListAsync();


        //Libs shared with the account
        var sharedLibs = await FriendLibraryShares
            .AsNoTracking()

            .Include(item => item.Friendship)
            .ThenInclude(item => item.Account1)
            .ThenInclude(item => item.Profiles)

            .Include(item => item.Friendship)
            .ThenInclude(item => item.Account2)
            .ThenInclude(item => item.Profiles)

            .Include(item => item.Library)

            .Where(item => item.Friendship.Account1Id == accountId || item.Friendship.Account2Id == accountId)
            .Where(item => item.Friendship.Accepted)
            .Where(item => !libs.Contains(item.LibraryId))

            .Select(item => item.LibraryId)
            .ToListAsync();

        libs.AddRange(sharedLibs);

        return libs;
    }



    /// <summary>
    /// This calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    /// </summary>
    public async Task<Account> GetOrCreateAccountAsync(string localId, string email, string displayName)
    {
        var account = await Accounts
            .AsNoTracking()
            .Include(item => item.Profiles)
            .Where(item => item.FirebaseId == localId)
            .FirstOrDefaultAsync();

        if (account == null)
        {
            account = new Account { FirebaseId = localId };
            Accounts.Add(account);
            await SaveChangesAsync();
        }

        await GetOrCreateMainProfileAsync(account, email, displayName);

        if (account.Profiles.Count == 0)
        {
            //Reload
            account = await Accounts
                .AsNoTracking()
                .Include(item => item.Profiles)
                .Where(item => item.FirebaseId == localId)
                .FirstOrDefaultAsync();
        }

        return account;
    }





    /// <summary>
    /// This calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    /// </summary>
    public async Task<Profile> GetOrCreateMainProfileAsync(Account account, string email, string displayName, CancellationToken cancellationToken = default)
    {
        var acctProfiles = new List<Profile>();
        if (account.Profiles == null || account.Profiles.Count == 0)
        {
            var profiles = await Profiles
                .AsNoTracking()
                .Where(item => item.AccountId == account.Id)
                .ToListAsync(cancellationToken);
            acctProfiles.AddRange(profiles);
        }
        else
        {
            acctProfiles.AddRange(account.Profiles);
        }

        var mainProfile = acctProfiles.FirstOrDefault(item => item.IsMain);
        if (mainProfile == null)
        {
            mainProfile = Profiles.Add(new Profile
            {
                AccountId = account.Id,
                MaxMovieRating = MovieRatings.Unrated,
                MaxTVRating = TVRatings.NotRated,
                AvatarUrl = Misc.EnsureProfilePic(null),
                IsMain = true,
                Name = displayName.HasValue() ? displayName.Trim() : email[..email.IndexOf("@")].Trim().ToLower(),
                TitleRequestPermission = TitleRequestPermissions.Enabled
            }).Entity;

            await SaveChangesAsync(cancellationToken);
        }

        return mainProfile;
    }


    public IQueryable<MediaEntry> TopLevelWatchableMediaByProfileQuery(Profile profile)
    {
        return MediaEntries
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
        return MediaEntries
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
        return MediaEntries
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

        var lib = await Libraries
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

        foreach (var profile in lib.ProfileLibraryShares.Select(_ => _.Profile).Where(_ => _.MaxMovieRating >= rating))
        {
            if (!ret.Contains(profile.Id))
                ret.Add(profile.Id);
        }

        return ret;
    }


    public async Task<List<int>> ProfilesWithAccessToLibraryAndRating(int libraryId, TVRatings rating)
    {
        List<int> ret = [];

        var lib = await Libraries
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

        var mediaEntry = await MediaEntries
            .AsNoTracking()
            .Include(m => m.Library)
            .ThenInclude(l => l.ProfileLibraryShares)
            .ThenInclude(pls => pls.Profile)
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


    public async Task<Result> LinkLibraryAndFriend(Account account, int friendId, int libraryId)
    {
        //Get friendship
        var friend = await GetFriend(account, friendId);

        if (friend == null)
            return CommonResponses.ValueNotFound("Friend");

        //Check if already shared
        if (friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
            return Result.BuildSuccess();

        //Check if this account owns the library
        var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
        if (!myAcct.Libraries.Any(item => item.Id == libraryId))
            return CommonResponses.ValueNotFound("Library");

        FriendLibraryShares.Add(new FriendLibraryShare
        {
            FriendshipId = friend.Id,
            LibraryId = libraryId
        });

        await SaveChangesAsync();


        //Scenario: Shared lib has items in a playlist. Then
        //Lib is unshared, artwork is updated, then reshared - need
        //to update the artwork again
        var playlistIds = await GetPlaylistIds(account, friend, libraryId);
        await MarkPlaylistArtworkNeedsupdate(playlistIds);

        return Result.BuildSuccess();
    }


    public async Task<Result> UnLinkLibraryAndFriend(Account account, int friendId, int libraryId)
    {
        //Get friendship
        var friend = await GetFriend(account, friendId);

        if (friend == null)
            return Result.BuildSuccess();

        //Check if link exists
        if (!friend.FriendLibraryShares.Any(item => item.LibraryId == libraryId))
            return Result.BuildSuccess();

        //Check if this account owns the library
        var myAcct = friend.Account1Id == account.Id ? friend.Account1 : friend.Account2;
        if (!myAcct.Libraries.Any(item => item.Id == libraryId))
            return Result.BuildSuccess();

        var share = new FriendLibraryShare
        {
            FriendshipId = friendId,
            LibraryId = libraryId
        };

        FriendLibraryShares.Remove(share);
        await SaveChangesAsync();

        var playlistIds = await GetPlaylistIds(account, friend, libraryId);
        await MarkPlaylistArtworkNeedsupdate(playlistIds);

        return Result.BuildSuccess();
    }


    private Task<Friendship> GetFriend(Account account, int friendId) =>
        Friendships
            .AsNoTracking()
            .Include(item => item.FriendLibraryShares)
            .Include(item => item.Account1)
            .ThenInclude(item => item.Libraries)
            .Include(item => item.Account2)
            .ThenInclude(item => item.Libraries)
            .Where(item => item.Id == friendId)
            .Where(item => item.Account1Id == account.Id || item.Account2Id == account.Id)
            .FirstOrDefaultAsync();


    private Task<List<int>> GetPlaylistIds(Account account, Friendship friend, int libraryId)
    {
        var friendAcct = friend.Account1Id == account.Id ? friend.Account2 : friend.Account1;

        return Playlists
            .AsNoTracking()
            .Where(item => item.Profile.AccountId == friendAcct.Id)
            .Where(item => item.PlaylistItems.Any(item2 => item2.MediaEntry.LibraryId == libraryId))
            .Select(item => item.Id)
            .Distinct()
            .ToListAsync();
    }


    /// <summary>
    /// This calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    /// </summary>
    public async Task<Result> LinkLibraryAndProfile(Account account, int profileId, int libraryId)
    {
        //Double check profile is owned by account
        var profile = account.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return CommonResponses.ValueNotFound("Profile");

        //See if already linked
        var library = await Libraries
            .AsNoTracking()
            .Include(l => l.ProfileLibraryShares.Where(p => p.ProfileId == profileId))
            .Include(l => l.FriendLibraryShares.Where(f => f.Friendship.Account1Id == account.Id || f.Friendship.Account2Id == account.Id))
            .ThenInclude(item => item.Friendship)
            .Where(l => l.Id == libraryId)
            .SingleOrDefaultAsync();

        if (library == null)
            return CommonResponses.ValueNotFound("Library");

        if (library.AccountId != account.Id)
            if (!library.FriendLibraryShares.Any(item => item.Friendship.Account1Id == account.Id))
                if (!library.FriendLibraryShares.Any(item => item.Friendship.Account2Id == account.Id))
                    return CommonResponses.ValueNotFound("Library");


        //Main profile has access to everything at this point without links
        if (profile.IsMain)
            return Result.BuildSuccess();

        ProfileLibraryShares.Add(new ProfileLibraryShare
        {
            LibraryId = libraryId,
            ProfileId = profileId
        });


        await SaveChangesAsync();

        MediaChangedTriggerManager.QueueHomeScreen(profileId);

        //Scenario: Linked lib has items in a playlist. Then
        //Lib is unlinked, artwork is updated, then relinked - need
        //to update the artwork again
        var playlistIds = await GetPlaylistIds(profileId, libraryId);
        await MarkPlaylistArtworkNeedsupdate(playlistIds);

        return Result.BuildSuccess();
    }



    public async Task<Result> UnLinkLibraryAndProfile(Account account, int profileId, int libraryId)
    {
        //Double check profile is owned by account
        var profile = account.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
            return Result.BuildSuccess();

        //Main profile has access to libs without links, so nothing to delete
        if (profile.IsMain)
            return Result.BuildSuccess();

        //Get the link
        var rec = await ProfileLibraryShares
            .Where(item => item.LibraryId == libraryId)
            .Where(item => item.ProfileId == profileId)
            .SingleOrDefaultAsync();

        if (rec != null)
        {
            ProfileLibraryShares.Remove(rec);
            await SaveChangesAsync();

            MediaChangedTriggerManager.QueueHomeScreen(profileId);

            var playlistIds = await GetPlaylistIds(profileId, libraryId);
            await MarkPlaylistArtworkNeedsupdate(playlistIds);
        }

        return Result.BuildSuccess();
    }

    private Task<List<int>> GetPlaylistIds(int profileId, int libraryId)
    {
        return Playlists
            .AsNoTracking()
            .Where(item => item.ProfileId == profileId)
            .Where(item => item.PlaylistItems.Any(item2 => item2.MediaEntry.LibraryId == libraryId))
            .Select(item => item.Id)
            .Distinct()
            .ToListAsync();
    }



    public TitleRequestPermissions GetTitleRequestPermissions(Account account, Profile profile, bool hasFriends)
    {
        if (profile.IsMain)
        {
            if (hasFriends || account.Profiles.Count > 1)
                return TitleRequestPermissions.Enabled;
            return TitleRequestPermissions.Disabled;
        }
        return profile.TitleRequestPermission;
    }


}

