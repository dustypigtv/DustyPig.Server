using DustyPig.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace DustyPig.Server.Data
{
    public partial class AppDbContext : DbContext
    {
        private static readonly LoggerFactory MyLoggerFactory = new(new[] { new NLog.Extensions.Logging.NLogLoggerProvider() });

        private static string _connectionString;

        public static void Configure(string connectionString)
        {
            _connectionString = connectionString;
        }



        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountToken> AccountTokens { get; set; }
        public DbSet<ActivationCode> ActivationCodes { get; set; }
        public DbSet<AutoPlaylistSeries> AutoPlaylistSeries { get; set; }
        public DbSet<FCMToken> FCMTokens { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendLibraryShare> FriendLibraryShares { get; set; }
        public DbSet<GetRequest> GetRequests { get; set; }
        public DbSet<GetRequestSubscription> GetRequestSubscriptions { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<MediaEntry> MediaEntries { get; set; }
        public DbSet<MediaSearchBridge> MediaSearchBridges { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistItem> PlaylistItems { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<ProfileLibraryShare> ProfileLibraryShares { get; set; }
        public DbSet<ProfileMediaProgress> ProfileMediaProgresses { get; set; }
        public DbSet<S3ArtFileToDelete> S3ArtFilesToDelete { get; set; }
        public DbSet<SearchTerm> SearchTerms { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Subtitle> Subtitles { get; set; }
        public DbSet<TitleOverride> TitleOverrides { get; set; }
        public DbSet<WatchlistItem> WatchListItems { get; set; }

        public DbSet<TMDB_Entry> TMDB_Entries { get; set; }
        public DbSet<TMDB_Person> TMDB_People { get; set; }
        public DbSet<TMDB_EntryPersonBridge> TMDB_EntryPeopleBridges { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder
                //.UseMySql(_connectionString, ServerVersion.Create(8, 0, 28, Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql))
                .UseMySql(_connectionString, ServerVersion.Create(8, 0, 28, Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql), opts =>
                {
                    opts.EnableRetryOnFailure();
                })
                .UseLoggerFactory(MyLoggerFactory)

#if DEBUG
                        .EnableSensitiveDataLogging(true)
                        .EnableDetailedErrors(true)
                        .LogTo(Console.WriteLine)
#endif
                        ;

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Manually set OnDelete = Cascade for tables with optional one-to-many links
            modelBuilder.Entity<ActivationCode>().HasOne(p => p.Account).WithMany().OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MediaEntry>().HasOne(p => p.LinkedTo).WithMany().OnDelete(DeleteBehavior.Cascade);

            //Set notifications to null
            modelBuilder.Entity<Notification>(e =>
            {
                e.HasOne(p => p.Friendship).WithMany().OnDelete(DeleteBehavior.SetNull);
                e.HasOne(p => p.GetRequest).WithMany().OnDelete(DeleteBehavior.SetNull);
                e.HasOne(p => p.MediaEntry).WithMany().OnDelete(DeleteBehavior.SetNull);
                e.HasOne(p => p.TitleOverride).WithMany().OnDelete(DeleteBehavior.SetNull);
            });

        }












        public static (bool Valid, string Fixed, string Error) CheckStringConstrants(string name, bool required, int maxLen, string val)
        {
            val += string.Empty;
            while (val.Contains("  "))
            {
                val = val.Replace("  ", " ");
            }
            val = val.Trim();

            if (val == string.Empty)
            {
                if (required)
                    return (false, val, $"Missing {name}");
                else
                    return (true, null, null);
            }

            if (val.Length > maxLen)
                return (false, val, $"{name} too long: max length is {maxLen} characters");

            return (true, val, null);
        }







    }
}

