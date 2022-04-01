using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace DustyPig.Server.Data.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FirebaseId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    Logger = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    CallSite = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    Level = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true),
                    Message = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    Exception = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Term = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountTokens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivationCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationCodes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_ActivationCodes_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EncryptedServiceCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CredentialType = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptedServiceCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncryptedServiceCredentials_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Account1Id = table.Column<int>(type: "int", nullable: false),
                    Account2Id = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    DisplayName1 = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    DisplayName2 = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Accepted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NotificationCreated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friendships_Accounts_Account1Id",
                        column: x => x.Account1Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friendships_Accounts_Account2Id",
                        column: x => x.Account2Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsTV = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Libraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Libraries_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsMain = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowedRatings = table.Column<int>(type: "int", nullable: false),
                    PinNumber = table.Column<short>(type: "smallint", nullable: true),
                    TitleRequestPermission = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    AvatarUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    WeeklySummary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NotificationMethods = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FriendLibraryShares",
                columns: table => new
                {
                    LibraryId = table.Column<int>(type: "int", nullable: false),
                    FriendshipId = table.Column<int>(type: "int", nullable: false),
                    LibraryDisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendLibraryShares", x => new { x.FriendshipId, x.LibraryId });
                    table.ForeignKey(
                        name: "FK_FriendLibraryShares_Friendships_FriendshipId",
                        column: x => x.FriendshipId,
                        principalTable: "Friendships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FriendLibraryShares_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LibraryId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    TMDB_Id = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Hash = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    SortTitle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    Rated = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(2500)", maxLength: 2500, nullable: true),
                    Genres = table.Column<long>(type: "bigint", nullable: true),
                    LinkedToId = table.Column<int>(type: "int", nullable: true),
                    Season = table.Column<int>(type: "int", nullable: true),
                    Episode = table.Column<int>(type: "int", nullable: true),
                    Xid = table.Column<long>(type: "bigint", nullable: true),
                    ExtraSortOrder = table.Column<int>(type: "int", nullable: true),
                    Length = table.Column<double>(type: "double", nullable: true),
                    IntroStartTime = table.Column<double>(type: "double", nullable: true),
                    IntroEndTime = table.Column<double>(type: "double", nullable: true),
                    CreditsStartTime = table.Column<double>(type: "double", nullable: true),
                    ArtworkUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    VideoUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    VideoServiceCredentialId = table.Column<int>(type: "int", nullable: true),
                    BifUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    BifServiceCredentialId = table.Column<int>(type: "int", nullable: true),
                    Added = table.Column<DateTime>(type: "datetime", nullable: true),
                    Popularity = table.Column<double>(type: "double", nullable: true),
                    NotificationsCreated = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaEntries_EncryptedServiceCredentials_BifServiceCredentia~",
                        column: x => x.BifServiceCredentialId,
                        principalTable: "EncryptedServiceCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaEntries_EncryptedServiceCredentials_VideoServiceCredent~",
                        column: x => x.VideoServiceCredentialId,
                        principalTable: "EncryptedServiceCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaEntries_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaEntries_MediaEntries_LinkedToId",
                        column: x => x.LinkedToId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceTokens_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    TMDB_Id = table.Column<int>(type: "int", nullable: false),
                    ParentalStatus = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GetRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GetRequests_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GetRequests_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ArtworkUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    CurrentIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playlists_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileLibraryShares",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    LibraryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileLibraryShares", x => new { x.ProfileId, x.LibraryId });
                    table.ForeignKey(
                        name: "FK_ProfileLibraryShares_Libraries_LibraryId",
                        column: x => x.LibraryId,
                        principalTable: "Libraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileLibraryShares_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaPersonBridges",
                columns: table => new
                {
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaPersonBridges", x => new { x.MediaEntryId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_MediaPersonBridges_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaPersonBridges_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaSearchBridges",
                columns: table => new
                {
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    SearchTermId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaSearchBridges", x => new { x.MediaEntryId, x.SearchTermId });
                    table.ForeignKey(
                        name: "FK_MediaSearchBridges_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaSearchBridges_SearchTerms_SearchTermId",
                        column: x => x.SearchTermId,
                        principalTable: "SearchTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OverrideRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NotificationCreated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverrideRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverrideRequests_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OverrideRequests_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileMediaProgresses",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Played = table.Column<double>(type: "double", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileMediaProgresses", x => new { x.ProfileId, x.MediaEntryId });
                    table.ForeignKey(
                        name: "FK_ProfileMediaProgresses_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileMediaProgresses_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => new { x.ProfileId, x.MediaEntryId });
                    table.ForeignKey(
                        name: "FK_Subscriptions_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subtitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    ServiceCredentialId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtitles_EncryptedServiceCredentials_ServiceCredentialId",
                        column: x => x.ServiceCredentialId,
                        principalTable: "EncryptedServiceCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitles_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TitleOverrides",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleOverrides", x => new { x.ProfileId, x.MediaEntryId });
                    table.ForeignKey(
                        name: "FK_TitleOverrides_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TitleOverrides_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchListItems",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Added = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchListItems", x => new { x.ProfileId, x.MediaEntryId });
                    table.ForeignKey(
                        name: "FK_WatchListItems_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WatchListItems_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PlaylistId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistItems_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaylistItems_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    FriendshipId = table.Column<int>(type: "int", nullable: true),
                    MediaEntryId = table.Column<int>(type: "int", nullable: true),
                    OverrideRequestId = table.Column<int>(type: "int", nullable: true),
                    GetRequestId = table.Column<int>(type: "int", nullable: true),
                    Sent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Seen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Friendships_FriendshipId",
                        column: x => x.FriendshipId,
                        principalTable: "Friendships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_GetRequests_GetRequestId",
                        column: x => x.GetRequestId,
                        principalTable: "GetRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_OverrideRequests_OverrideRequestId",
                        column: x => x.OverrideRequestId,
                        principalTable: "OverrideRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "FirebaseId" },
                values: new object[] { 1, "qVp7jUVVTgS0HAQLd33HCsTmUeI2" });

            migrationBuilder.InsertData(
                table: "SearchTerms",
                columns: new[] { "Id", "Term" },
                values: new object[,]
                {
                    { 1, "agent" },
                    { 2, "327" },
                    { 3, "operation" },
                    { 4, "barbershop" },
                    { 5, "big" },
                    { 6, "buck" },
                    { 7, "bunny" },
                    { 8, "coffee" },
                    { 9, "run" },
                    { 10, "hero" },
                    { 11, "spring" },
                    { 12, "caminandes" }
                });

            migrationBuilder.InsertData(
                table: "Libraries",
                columns: new[] { "Id", "AccountId", "IsTV", "Name" },
                values: new object[] { 1, 1, false, "Movies" });

            migrationBuilder.InsertData(
                table: "Libraries",
                columns: new[] { "Id", "AccountId", "IsTV", "Name" },
                values: new object[] { 2, 1, true, "TV Shows" });

            migrationBuilder.InsertData(
                table: "Profiles",
                columns: new[] { "Id", "AccountId", "AllowedRatings", "AvatarUrl", "IsMain", "Locked", "Name", "NotificationMethods", "PinNumber", "TitleRequestPermission", "WeeklySummary" },
                values: new object[] { 1, 1, 8191, null, true, false, "Test User", 0, null, (byte)1, false });

            migrationBuilder.InsertData(
                table: "MediaEntries",
                columns: new[] { "Id", "Added", "ArtworkUrl", "BifServiceCredentialId", "BifUrl", "CreditsStartTime", "Date", "Description", "EntryType", "Episode", "ExtraSortOrder", "Genres", "Hash", "IntroEndTime", "IntroStartTime", "Length", "LibraryId", "LinkedToId", "NotificationsCreated", "Popularity", "Rated", "Season", "SortTitle", "TMDB_Id", "Title", "VideoServiceCredentialId", "VideoUrl", "Xid" },
                values: new object[,]
                {
                    { 1, new DateTime(2021, 9, 6, 5, 20, 38, 399, DateTimeKind.Unspecified).AddTicks(9293), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.bif", 205.875, new DateTime(2017, 5, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Agent 327 is investigating a clue that leads him to a shady barbershop in Amsterdam. Little does he know that he is being tailed by mercenary Boris Kloris.", 1, null, null, 4195332L, "4D517D0FE23F491E9898B2C4036633DD", null, null, 231.47999999999999, 1, null, true, null, 1, null, "Agent 327: Operation Barbershop", 457784, "Agent 327: Operation Barbershop", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.mp4", null },
                    { 2, new DateTime(2021, 9, 6, 5, 20, 38, 454, DateTimeKind.Unspecified).AddTicks(1594), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.bif", 490.25, new DateTime(2008, 4, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Follow a day of the life of Big Buck Bunny when he meets three bullying rodents: Frank, Rinky, and Gamera. The rodents amuse themselves by harassing helpless creatures by throwing fruits, nuts and rocks at them. After the deaths of two of Bunny's favorite butterflies, and an offensive attack on Bunny himself, Bunny sets aside his gentle nature and orchestrates a complex plan for revenge.", 1, null, null, 1092L, "FD348F25B50C620489368C39114C546B", null, null, 596.47400000000005, 1, null, true, null, 1, null, "Big Buck Bunny", 10378, "Big Buck Bunny", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.mp4", null },
                    { 3, new DateTime(2021, 9, 6, 5, 20, 38, 506, DateTimeKind.Unspecified).AddTicks(3620), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.bif", 164.083, new DateTime(2020, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fueled by caffeine, a young woman runs through the bittersweet memories of her past relationship.", 1, null, null, 1029L, "36E38939B821448B673B7282430414A4", 6.7919999999999998, 0.0, 184.59899999999999, 1, null, true, null, 1, null, "Coffee Run", 717986, "Coffee Run", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.mp4", null },
                    { 4, new DateTime(2021, 9, 6, 5, 20, 38, 554, DateTimeKind.Unspecified).AddTicks(5793), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.bif", 147.31399999999999, new DateTime(2018, 4, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hero is a showcase for the updated Grease Pencil tools in Blender 2.80. Grease Pencil means 2D animation tools within a full 3D pipeline.", 1, null, null, 1030L, "D9034F1547910D922076D71505EF9630", 4.8380000000000001, 0.0, 236.65799999999999, 1, null, true, null, 1, null, "Hero", 615324, "Hero", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.mp4", null },
                    { 5, new DateTime(2021, 9, 6, 5, 20, 38, 601, DateTimeKind.Unspecified).AddTicks(9560), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.bif", 427.79199999999997, new DateTime(2019, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "The story of a shepherd girl and her dog who face ancient spirits in order to continue the cycle of life.", 1, null, null, 3076L, "1C318602D6D38D28DC00041FB05601C1", null, null, 464.09800000000001, 1, null, true, null, 1, null, "Spring", 593048, "Spring", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.mp4", null },
                    { 6, null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/show.jpg", null, null, null, null, "The Caminandes cartoon series follows our hero Koro the Llama as he explores Patagonia, attempts to overcome various obstacles, and becomes friends with Oti the pesky penguin.", 2, null, null, 1060L, "DCC9FD6C0133457A194DAAA1E54B0713", null, null, null, 2, null, true, null, 256, null, "Caminandes", 276116, "Caminandes", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "ProfileLibraryShares",
                columns: new[] { "LibraryId", "ProfileId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 }
                });

            migrationBuilder.InsertData(
                table: "MediaEntries",
                columns: new[] { "Id", "Added", "ArtworkUrl", "BifServiceCredentialId", "BifUrl", "CreditsStartTime", "Date", "Description", "EntryType", "Episode", "ExtraSortOrder", "Genres", "Hash", "IntroEndTime", "IntroStartTime", "Length", "LibraryId", "LinkedToId", "NotificationsCreated", "Popularity", "Rated", "Season", "SortTitle", "TMDB_Id", "Title", "VideoServiceCredentialId", "VideoUrl", "Xid" },
                values: new object[,]
                {
                    { 8, new DateTime(2021, 9, 6, 5, 20, 38, 559, DateTimeKind.Unspecified).AddTicks(3102), "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.bif", 119.25, new DateTime(2013, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro hunts for food on the other side of a fence and is once again inspired by the Armadillo but this time to a shocking effect.", 3, 2, null, null, "13BEA3CDE590997C1094F9BBA14D719A", null, null, 146.00800000000001, 2, 6, true, null, null, 1, null, 0, "Gran Dillama", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.mp4", 10002L },
                    { 7, new DateTime(2021, 9, 6, 5, 20, 39, 559, DateTimeKind.Unspecified).AddTicks(3737), "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.bif", 139.5, new DateTime(2013, 12, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro meets Oti, a pesky Magellanic penguin, in an epic battle over tasty red berries during the winter.", 3, 3, null, null, "1EBABA3465F39106375BE623A0EBAB45", null, null, 150.048, 2, 6, true, null, null, 1, null, 0, "Llamigos", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.mp4", 10003L },
                    { 9, new DateTime(2021, 9, 6, 5, 20, 37, 559, DateTimeKind.Unspecified).AddTicks(1031), "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.bif", 87.917000000000002, new DateTime(2013, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro has trouble crossing an apparent desolate road, a problem that an unwitting Armadillo does not share.", 3, 1, null, null, "81582BDB254A94E4464424087C6479A8", null, null, 90.001000000000005, 2, 6, true, null, null, 1, null, 0, "Llama Drama", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.mp4", 10001L }
                });

            migrationBuilder.InsertData(
                table: "MediaSearchBridges",
                columns: new[] { "MediaEntryId", "SearchTermId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 5, 11 },
                    { 4, 10 },
                    { 3, 9 },
                    { 3, 8 },
                    { 6, 12 },
                    { 2, 6 },
                    { 2, 5 },
                    { 1, 4 },
                    { 1, 3 },
                    { 1, 2 },
                    { 2, 7 }
                });

            migrationBuilder.InsertData(
                table: "ProfileMediaProgresses",
                columns: new[] { "MediaEntryId", "ProfileId", "Played", "Timestamp" },
                values: new object[,]
                {
                    { 1, 1, 10.0, new DateTime(2021, 9, 24, 11, 35, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 1, 180.0, new DateTime(2021, 9, 24, 11, 34, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "ProfileMediaProgresses",
                columns: new[] { "MediaEntryId", "ProfileId", "Played", "Timestamp" },
                values: new object[] { 8, 1, 30.0, new DateTime(2021, 9, 24, 13, 12, 14, 344, DateTimeKind.Unspecified).AddTicks(8000) });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_FirebaseId",
                table: "Accounts",
                column: "FirebaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountTokens_AccountId",
                table: "AccountTokens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationCodes_AccountId",
                table: "ActivationCodes",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_ProfileId",
                table: "DeviceTokens",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EncryptedServiceCredentials_AccountId_Name",
                table: "EncryptedServiceCredentials",
                columns: new[] { "AccountId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FriendLibraryShares_LibraryId",
                table: "FriendLibraryShares",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_Account1Id",
                table: "Friendships",
                column: "Account1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_Account2Id",
                table: "Friendships",
                column: "Account2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_Hash",
                table: "Friendships",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GetRequests_AccountId",
                table: "GetRequests",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GetRequests_ProfileId_AccountId_EntryType_TMDB_Id",
                table: "GetRequests",
                columns: new[] { "ProfileId", "AccountId", "EntryType", "TMDB_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_AccountId_Name",
                table: "Libraries",
                columns: new[] { "AccountId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_BifServiceCredentialId",
                table: "MediaEntries",
                column: "BifServiceCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_LibraryId_EntryType_TMDB_Id_Hash",
                table: "MediaEntries",
                columns: new[] { "LibraryId", "EntryType", "TMDB_Id", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_LinkedToId",
                table: "MediaEntries",
                column: "LinkedToId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_VideoServiceCredentialId",
                table: "MediaEntries",
                column: "VideoServiceCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPersonBridges_MediaEntryId_PersonId_Role",
                table: "MediaPersonBridges",
                columns: new[] { "MediaEntryId", "PersonId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaPersonBridges_PersonId",
                table: "MediaPersonBridges",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaSearchBridges_SearchTermId",
                table: "MediaSearchBridges",
                column: "SearchTermId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_FriendshipId",
                table: "Notifications",
                column: "FriendshipId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_GetRequestId",
                table: "Notifications",
                column: "GetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MediaEntryId",
                table: "Notifications",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OverrideRequestId",
                table: "Notifications",
                column: "OverrideRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProfileId",
                table: "Notifications",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_OverrideRequests_MediaEntryId",
                table: "OverrideRequests",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_OverrideRequests_ProfileId",
                table: "OverrideRequests",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_People_Name",
                table: "People",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_MediaEntryId",
                table: "PlaylistItems",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId",
                table: "PlaylistItems",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_ProfileId_Name_CurrentIndex",
                table: "Playlists",
                columns: new[] { "ProfileId", "Name", "CurrentIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileLibraryShares_LibraryId",
                table: "ProfileLibraryShares",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMediaProgresses_MediaEntryId",
                table: "ProfileMediaProgresses",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_AccountId_Name",
                table: "Profiles",
                columns: new[] { "AccountId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchTerms_Term",
                table: "SearchTerms",
                column: "Term",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_MediaEntryId",
                table: "Subscriptions",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitles_MediaEntryId_Name",
                table: "Subtitles",
                columns: new[] { "MediaEntryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subtitles_ServiceCredentialId",
                table: "Subtitles",
                column: "ServiceCredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleOverrides_MediaEntryId",
                table: "TitleOverrides",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchListItems_MediaEntryId",
                table: "WatchListItems",
                column: "MediaEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountTokens");

            migrationBuilder.DropTable(
                name: "ActivationCodes");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "FriendLibraryShares");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "MediaPersonBridges");

            migrationBuilder.DropTable(
                name: "MediaSearchBridges");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlaylistItems");

            migrationBuilder.DropTable(
                name: "ProfileLibraryShares");

            migrationBuilder.DropTable(
                name: "ProfileMediaProgresses");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Subtitles");

            migrationBuilder.DropTable(
                name: "TitleOverrides");

            migrationBuilder.DropTable(
                name: "WatchListItems");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "SearchTerms");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "GetRequests");

            migrationBuilder.DropTable(
                name: "OverrideRequests");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "MediaEntries");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "EncryptedServiceCredentials");

            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
