using DustyPig.API.v3.Models;
using DustyPig.API.v3.MPAA;
using DustyPig.Server.Controllers.v3.Logic;
using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataFCMToken = DustyPig.Server.Data.Models.FCMToken;
using DataNotifications = DustyPig.Server.Data.Models.Notification;
using DataPlaylistItem = DustyPig.Server.Data.Models.PlaylistItem;
using DataTMDB_Person = DustyPig.Server.Data.Models.TMDB_Person;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext : DbContext
    {
        public static bool Ready { get; private set; }

        private static string _connectionString;

        public static void Configure(string connectionString)
        {
            _connectionString = connectionString;
        }



        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountToken> AccountTokens { get; set; }
        public DbSet<ActivationCode> ActivationCodes { get; set; }
        public DbSet<AutoPlaylistSeries> AutoPlaylistSeries { get; set; }
        public DbSet<ExtraSearchTerm> ExtraSearchTerms { get; set; }
        public DbSet<DataFCMToken> FCMTokens { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendLibraryShare> FriendLibraryShares { get; set; }
        public DbSet<GetRequest> GetRequests { get; set; }
        public DbSet<GetRequestSubscription> GetRequestSubscriptions { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<MediaEntry> MediaEntries { get; set; }
        public DbSet<DataNotifications> Notifications { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<DataPlaylistItem> PlaylistItems { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<ProfileLibraryShare> ProfileLibraryShares { get; set; }
        public DbSet<ProfileMediaProgress> ProfileMediaProgresses { get; set; }
        public DbSet<S3ArtFileToDelete> S3ArtFilesToDelete { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<TitleOverride> TitleOverrides { get; set; }
        public DbSet<WatchlistItem> WatchListItems { get; set; }

        public DbSet<TMDB_Entry> TMDB_Entries { get; set; }
        public DbSet<DataTMDB_Person> TMDB_People { get; set; }
        public DbSet<TMDB_EntryPersonBridge> TMDB_EntryPeopleBridges { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder
                .UseMySql(_connectionString, ServerVersion.Create(8, 0, 28, Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql), opts =>
                {
                    opts.EnableRetryOnFailure();
                });


        }

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

        }









        /// <summary>
        /// This calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
        /// </summary>
        public async Task<Account> GetOrCreateAccountAsync(string localId, string email)
        {
            var account = await this.Accounts
                .AsNoTracking()
                .Include(item => item.Profiles)
                .Where(item => item.FirebaseId == localId)
                .FirstOrDefaultAsync();

            if (account == null)
            {
                account = new Account { FirebaseId = localId };
                this.Accounts.Add(account);
                await this.SaveChangesAsync();
            }

            await this.GetOrCreateMainProfileAsync(account, email);

            return account;
        }





        /// <summary>
        /// This calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
        /// </summary>
        public async Task<Profile> GetOrCreateMainProfileAsync(Account account, string email, CancellationToken cancellationToken = default)
        {
            var acctProfiles = new List<Profile>();
            if (account.Profiles == null || account.Profiles.Count == 0)
            {
                var profiles = await this.Profiles
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
                mainProfile = this.Profiles.Add(new Profile
                {
                    AccountId = account.Id,
                    MaxMovieRating = MovieRatings.Unrated,
                    MaxTVRating = TVRatings.NotRated,
                    AvatarUrl = LogicUtils.EnsureProfilePic(null),
                    IsMain = true,
                    Name = email[..email.IndexOf("@")].Trim().ToLower(),
                    TitleRequestPermission = TitleRequestPermissions.Enabled
                }).Entity;

                int idx = 0;
                while (acctProfiles.Count(_ => _.Name.ICEquals(mainProfile.Name)) > 0)
                {
                    idx++;
                    mainProfile.Name = email[..email.IndexOf("@")].Trim().ToLower() + idx.ToString();
                }

                await SaveChangesAsync(cancellationToken);
            }

            return mainProfile;
        }


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





        public async Task Migrate(CancellationToken cancellationToken = default)
        {
            try
            {
                await Database.MigrateAsync(cancellationToken);

                //Ensure Test account exists
                var tstAcct = Accounts.FirstOrDefault(_ => _.Id == 1);

                bool add = false;
                if (tstAcct == null)
                {
                    add = true;
                }
                else if (tstAcct.FirebaseId != "TEST ACCOUNT")
                {
                    Accounts.Remove(tstAcct);
                    await SaveChangesAsync(cancellationToken);
                    add = true;
                }

                if (add)
                {
                    tstAcct = Accounts.Add(new Account
                    {
                        Id = 1,
                        FirebaseId = "TEST ACCOUNT"
                    }).Entity;
                    await SaveChangesAsync(cancellationToken);
                }


                var tstProfile = Profiles.FirstOrDefault(_ => _.Id == 1);

                add = false;
                if (tstProfile == null)
                {
                    add = true;
                }
                else if (tstProfile.AccountId != 1)
                {
                    Profiles.Remove(tstProfile);
                    await SaveChangesAsync(cancellationToken);
                    add = true;
                }

                if (add)
                {
                    Profiles.Add(new Profile
                    {
                        AccountId = 1,
                        Id = 1,
                        IsMain = true,
                        Name = "Test User",
                        AvatarUrl = DustyPig.API.v3.Models.Constants.DEFAULT_PROFILE_IMAGE_GREY,
                        MaxMovieRating = API.v3.MPAA.MovieRatings.Unrated,
                        MaxTVRating = API.v3.MPAA.TVRatings.NotRated
                    });
                    await SaveChangesAsync(cancellationToken);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error migrating database");
                Console.WriteLine(ex.ToString());
                Thread.Sleep(1000);
            }
        }
    }
}

