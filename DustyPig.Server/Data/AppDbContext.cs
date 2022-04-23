using DustyPig.Server.Data.Models;
using DustyPig.Server.Utilities;
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
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<EncryptedServiceCredential> EncryptedServiceCredentials { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendLibraryShare> FriendLibraryShares { get; set; }
        public DbSet<GetRequest> GetRequests { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<MediaEntry> MediaEntries { get; set; }
        public DbSet<MediaPersonBridge> MediaPersonBridges { get; set; }
        public DbSet<MediaSearchBridge> MediaSearchBridges { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OverrideRequest> OverrideRequests { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistItem> PlaylistItems { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<ProfileLibraryShare> ProfileLibraryShares { get; set; }
        public DbSet<ProfileMediaProgress> ProfileMediaProgresses { get; set; }
        public DbSet<SearchTerm> SearchTerms { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Subtitle> Subtitles { get; set; }
        public DbSet<TitleOverride> TitleOverrides { get; set; }
        public DbSet<WatchlistItem> WatchListItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder
                //.UseMySQL(_connectionString)
                .UseMySql(_connectionString, ServerVersion.Create(8,0,28, Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql))
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



            //Composite Keys
            modelBuilder.Entity<FriendLibraryShare>().HasKey(e => new { e.FriendshipId, e.LibraryId });
            modelBuilder.Entity<MediaPersonBridge>().HasKey(e => new { e.MediaEntryId, e.PersonId });
            modelBuilder.Entity<MediaSearchBridge>().HasKey(e => new { e.MediaEntryId, e.SearchTermId });
            modelBuilder.Entity<ProfileLibraryShare>().HasKey(e => new { e.ProfileId, e.LibraryId });
            modelBuilder.Entity<ProfileMediaProgress>().HasKey(e => new { e.ProfileId, e.MediaEntryId });
            modelBuilder.Entity<Subscription>().HasKey(e => new { e.ProfileId, e.MediaEntryId });
            modelBuilder.Entity<TitleOverride>().HasKey(e => new { e.ProfileId, e.MediaEntryId });
            modelBuilder.Entity<WatchlistItem>().HasKey(e => new { e.ProfileId, e.MediaEntryId });


            //Manually set OnDelete = Cascade for tables with optional links
            modelBuilder.Entity<ActivationCode>().HasOne(p => p.Account).WithMany().OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MediaEntry>(e =>
            {
                e.HasOne(p => p.LinkedTo).WithMany().OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.BifServiceCredential).WithMany().OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.VideoServiceCredential).WithMany().OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<Notification>(e =>
            {
                e.HasOne(p => p.Friendship).WithMany().OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.GetRequest).WithMany().OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.MediaEntry).WithMany().OnDelete(DeleteBehavior.Cascade);
                e.HasOne(p => p.OverrideRequest).WithMany().OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Subtitle>().HasOne(p => p.ServiceCredential).WithMany().OnDelete(DeleteBehavior.Cascade);



            //Not sure what the deal is with MediaPersonBridge, but if I don't do this then it tries to create a unique index on PersonId - which is bad
            modelBuilder.Entity<MediaPersonBridge>().HasIndex(e => e.PersonId).IsUnique(false);



            //Seed Data
            modelBuilder.Entity<Account>().HasData(new Account[]
            {
                //Test Account
                new Account
                {
                    Id = TestAccount.AccountId,
                    FirebaseId = TestAccount.FirebaseId
                }
            });

            modelBuilder.Entity<Profile>().HasData(new Profile[]
            {
                new Profile
                {
                    Id = TestAccount.ProfileId,
                    AccountId = TestAccount.AccountId,
                    Name = TestAccount.Name,
                    IsMain = true,
                    AllowedRatings = (API.v3.MPAA.Ratings)8191,
                    TitleRequestPermission = API.v3.Models.TitleRequestPermissions.Disabled,
                    AvatarUrl = TestAccount.AvatarUrl
                }
            });

            modelBuilder.Entity<Library>().HasData(new Library[]
            {
                new Library { Id = 1, AccountId = TestAccount.AccountId, Name = "Movies" },
                new Library { Id = 2, AccountId = TestAccount.AccountId, Name = "TV Shows", IsTV = true }
            });

            modelBuilder.Entity<ProfileLibraryShare>().HasData(new ProfileLibraryShare[]
            {
                new ProfileLibraryShare{ LibraryId = 1, ProfileId = TestAccount.ProfileId },
                new ProfileLibraryShare{ LibraryId = 2, ProfileId = TestAccount.ProfileId }
            });

            modelBuilder.Entity<MediaEntry>().HasData(new MediaEntry[]
            {
                new MediaEntry
                {
                    Id = 1,
                    Added = DateTime.Parse("2021-09-06T05:20:38.3999293"),
                    LibraryId = 1,
                    EntryType = API.v3.Models.MediaTypes.Movie,
                    TMDB_Id = 457784,
                    Title = "Agent 327: Operation Barbershop",
                    Hash = Crypto.HashMovieTitle("Agent 327: Operation Barbershop", 2017),
                    SortTitle = "Agent 327: Operation Barbershop",
                    Date = DateTime.Parse("2017-05-12"),
                    Rated = API.v3.MPAA.Ratings.G,
                    Description = "Agent 327 is investigating a clue that leads him to a shady barbershop in Amsterdam. Little does he know that he is being tailed by mercenary Boris Kloris.",
                    Genres = (API.v3.MPAA.Genres)4195332,
                    Length = 231.480,
                    CreditsStartTime = 205.875,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.mp4",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.jpg",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.bif",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 2,
                    Added = DateTime.Parse("2021-09-06T05:20:38.4541594"),
                    LibraryId = 1,
                    EntryType = API.v3.Models.MediaTypes.Movie,
                    TMDB_Id = 10378,
                    Title = "Big Buck Bunny",
                    Hash = Crypto.HashMovieTitle("Big Buck Bunny", 2008),
                    SortTitle = "Big Buck Bunny",
                    Date = DateTime.Parse("2008-04-10"),
                    Rated = API.v3.MPAA.Ratings.G,
                    Description = "Follow a day of the life of Big Buck Bunny when he meets three bullying rodents: Frank, Rinky, and Gamera. The rodents amuse themselves by harassing helpless creatures by throwing fruits, nuts and rocks at them. After the deaths of two of Bunny's favorite butterflies, and an offensive attack on Bunny himself, Bunny sets aside his gentle nature and orchestrates a complex plan for revenge.",
                    Genres = (API.v3.MPAA.Genres)1092,
                    Length = 596.474,
                    CreditsStartTime = 490.250,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.mp4",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.jpg",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.bif",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 3,
                    Added = DateTime.Parse("2021-09-06T05:20:38.506362"),
                    LibraryId = 1,
                    EntryType = API.v3.Models.MediaTypes.Movie,
                    TMDB_Id = 717986,
                    Title = "Coffee Run",
                    Hash = Crypto.HashMovieTitle("Coffee Run", 2020),
                    SortTitle = "Coffee Run",
                    Date = DateTime.Parse("2020-05-29"),
                    Rated = API.v3.MPAA.Ratings.G,
                    Description = "Fueled by caffeine, a young woman runs through the bittersweet memories of her past relationship.",
                    Genres = (API.v3.MPAA.Genres)1029,
                    Length = 184.599,
                    IntroStartTime = 0,
                    IntroEndTime = 6.792,
                    CreditsStartTime = 164.083,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.mp4",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.jpg",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.bif",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 4,
                    Added = DateTime.Parse("2021-09-06T05:20:38.5545793"),
                    LibraryId = 1,
                    EntryType = API.v3.Models.MediaTypes.Movie,
                    TMDB_Id = 615324,
                    Title = "Hero",
                    Hash = Crypto.HashMovieTitle("Hero", 2018),
                    SortTitle = "Hero",
                    Date = DateTime.Parse("2018-04-16"),
                    Rated = API.v3.MPAA.Ratings.G,
                    Description = "Hero is a showcase for the updated Grease Pencil tools in Blender 2.80. Grease Pencil means 2D animation tools within a full 3D pipeline.",
                    Genres = (API.v3.MPAA.Genres)1030,
                    Length = 236.658,
                    IntroStartTime = 0,
                    IntroEndTime = 4.838,
                    CreditsStartTime = 147.314,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.mp4",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.jpg",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.bif",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 5,
                    Added = DateTime.Parse("2021-09-06T05:20:38.601956"),
                    LibraryId = 1,
                    EntryType = API.v3.Models.MediaTypes.Movie,
                    TMDB_Id = 593048,
                    Title = "Spring",
                    Hash = Crypto.HashMovieTitle("Spring", 2019),
                    SortTitle = "Spring",
                    Date = DateTime.Parse("2019-04-04"),
                    Rated = API.v3.MPAA.Ratings.G,
                    Description = "The story of a shepherd girl and her dog who face ancient spirits in order to continue the cycle of life.",
                    Genres = (API.v3.MPAA.Genres)3076,
                    Length = 464.098,
                    CreditsStartTime = 427.792,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.mp4",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.jpg",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.bif",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 6,
                    LibraryId = 2,
                    EntryType = API.v3.Models.MediaTypes.Series,
                    TMDB_Id = 276116,
                    Title = "Caminandes",
                    Hash = Crypto.HashTitle("Caminandes"),
                    SortTitle = "Caminandes",
                    Rated = API.v3.MPAA.Ratings.TV_G,
                    Description = "The Caminandes cartoon series follows our hero Koro the Llama as he explores Patagonia, attempts to overcome various obstacles, and becomes friends with Oti the pesky penguin.",
                    Genres = (API.v3.MPAA.Genres)1060,
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/show.jpg",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 7,
                    Added = DateTime.Parse("2021-09-06T05:20:39.5593737"),
                    LibraryId = 2,
                    EntryType = API.v3.Models.MediaTypes.Episode,
                    TMDB_Id = 0,
                    Title = "Llamigos",
                    Hash = Crypto.HashEpisode(6, 1, 3),
                    Date = DateTime.Parse("2013-12-20"),
                    Description = "Koro meets Oti, a pesky Magellanic penguin, in an epic battle over tasty red berries during the winter.",
                    LinkedToId = 6,
                    Season = 1,
                    Episode = 3,
                    Xid = 10003,
                    Length = 150.048,
                    CreditsStartTime = 139.500,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.mp4",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.bif",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.jpg",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 8,
                    Added = DateTime.Parse("2021-09-06T05:20:38.5593102"),
                    LibraryId = 2,
                    EntryType = API.v3.Models.MediaTypes.Episode,
                    TMDB_Id = 0,
                    Title = "Gran Dillama",
                    Hash = Crypto.HashEpisode(6, 1, 2),
                    Date = DateTime.Parse("2013-11-22"),
                    Description = "Koro hunts for food on the other side of a fence and is once again inspired by the Armadillo but this time to a shocking effect.",
                    LinkedToId = 6,
                    Season = 1,
                    Episode = 2,
                    Xid = 10002,
                    Length = 146.008,
                    CreditsStartTime = 119.250,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.mp4",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.bif",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.jpg",
                    NotificationsCreated = true
                },
                new MediaEntry
                {
                    Id = 9,
                    Added = DateTime.Parse("2021-09-06T05:20:37.5591031"),
                    LibraryId = 2,
                    EntryType = API.v3.Models.MediaTypes.Episode,
                    TMDB_Id = 0,
                    Title = "Llama Drama",
                    Hash = Crypto.HashEpisode(6, 1, 1),
                    Date = DateTime.Parse("2013-09-29"),
                    Description = "Koro has trouble crossing an apparent desolate road, a problem that an unwitting Armadillo does not share.",
                    LinkedToId = 6,
                    Season = 1,
                    Episode = 1,
                    Xid = 10001,
                    Length = 90.001,
                    CreditsStartTime = 87.917,
                    VideoUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.mp4",
                    BifUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.bif",
                    ArtworkUrl = "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.jpg",
                    NotificationsCreated = true
                }
            });


            modelBuilder.Entity<SearchTerm>().HasData(new SearchTerm[]
            {
                new SearchTerm { Id = 1, Term = "agent" },
                new SearchTerm { Id = 2, Term = "327" },
                new SearchTerm { Id = 3, Term = "operation" },
                new SearchTerm { Id = 4, Term = "barbershop" },
                new SearchTerm { Id = 5, Term = "big" },
                new SearchTerm { Id = 6, Term = "buck" },
                new SearchTerm { Id = 7, Term = "bunny" },
                new SearchTerm { Id = 8, Term = "coffee" },
                new SearchTerm { Id = 9, Term = "run" },
                new SearchTerm { Id = 10, Term = "hero" },
                new SearchTerm { Id = 11, Term = "spring" },
                new SearchTerm { Id = 12, Term = "caminandes" }
            });


            modelBuilder.Entity<MediaSearchBridge>().HasData(new MediaSearchBridge[]
            {
                new MediaSearchBridge { MediaEntryId = 1, SearchTermId = 1 },
                new MediaSearchBridge { MediaEntryId = 1, SearchTermId = 2 },
                new MediaSearchBridge { MediaEntryId = 1, SearchTermId = 3 },
                new MediaSearchBridge { MediaEntryId = 1, SearchTermId = 4 },
                new MediaSearchBridge { MediaEntryId = 2, SearchTermId = 5 },
                new MediaSearchBridge { MediaEntryId = 2, SearchTermId = 6 },
                new MediaSearchBridge { MediaEntryId = 2, SearchTermId = 7 },
                new MediaSearchBridge { MediaEntryId = 3, SearchTermId = 8 },
                new MediaSearchBridge { MediaEntryId = 3, SearchTermId = 9 },
                new MediaSearchBridge { MediaEntryId = 4, SearchTermId = 10 },
                new MediaSearchBridge { MediaEntryId = 5, SearchTermId = 11 },
                new MediaSearchBridge { MediaEntryId = 6, SearchTermId = 12 }
            });


            modelBuilder.Entity<ProfileMediaProgress>().HasData(new ProfileMediaProgress[]
            {
                new ProfileMediaProgress
                {
                    ProfileId = 1,
                    MediaEntryId = 2,
                    Played = 180.000,
                    Timestamp= DateTime.Parse("2021-09-24T11:34:00.0000000")
                },
                new ProfileMediaProgress
                {
                    ProfileId = 1,
                    MediaEntryId = 1,
                    Played = 10.000,
                    Timestamp= DateTime.Parse("2021-09-24T11:35:00.0000000")
                },
                new ProfileMediaProgress
                {
                    ProfileId = 1,
                    MediaEntryId = 8,
                    Played = 30.000,
                    Timestamp= DateTime.Parse("2021-09-24T13:12:14.3448")
                }
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

