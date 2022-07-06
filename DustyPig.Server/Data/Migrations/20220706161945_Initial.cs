using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FirebaseId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Logger = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CallSite = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Exception = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SearchTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Term = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchTerms", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AccountTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActivationCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Account1Id = table.Column<int>(type: "int", nullable: false),
                    Account2Id = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Accepted = table.Column<bool>(type: "tinyint(1)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMain = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowedRatings = table.Column<int>(type: "int", nullable: false),
                    PinNumber = table.Column<short>(type: "smallint", nullable: true),
                    TitleRequestPermission = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    AvatarUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FriendLibraryShares",
                columns: table => new
                {
                    LibraryId = table.Column<int>(type: "int", nullable: false),
                    FriendshipId = table.Column<int>(type: "int", nullable: false),
                    LibraryDisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LibraryId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    TMDB_Id = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Rated = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(10000)", maxLength: 10000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    ArtworkUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BackdropUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VideoUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BifUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Added = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Popularity = table.Column<double>(type: "double", nullable: true),
                    PopularityUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaEntries", x => x.Id);
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    TMDB_Id = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: true)
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
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    table.PrimaryKey("PK_MediaPersonBridges", x => new { x.MediaEntryId, x.PersonId, x.Role });
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProfileMediaProgresses",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Xid = table.Column<long>(type: "bigint", nullable: true),
                    Played = table.Column<double>(type: "double", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Subtitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtitles_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TitleOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleOverrides", x => x.Id);
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WatchListItems",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    Added = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GetRequestSubscriptions",
                columns: table => new
                {
                    GetRequestId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GetRequestSubscriptions", x => new { x.GetRequestId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_GetRequestSubscriptions_GetRequests_GetRequestId",
                        column: x => x.GetRequestId,
                        principalTable: "GetRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GetRequestSubscriptions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    FriendshipId = table.Column<int>(type: "int", nullable: true),
                    MediaEntryId = table.Column<int>(type: "int", nullable: true),
                    GetRequestId = table.Column<int>(type: "int", nullable: true),
                    TitleOverrideId = table.Column<int>(type: "int", nullable: true),
                    Sent = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Seen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                        name: "FK_Notifications_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_TitleOverrides_TitleOverrideId",
                        column: x => x.TitleOverrideId,
                        principalTable: "TitleOverrides",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "FirebaseId" },
                values: new object[] { 1, "TEST ACCOUNT" });

            migrationBuilder.InsertData(
                table: "SearchTerms",
                columns: new[] { "Id", "Hash", "Term" },
                values: new object[,]
                {
                    { 1, "EDBFAC241AA6B765DABD6FB200BC9E96898C4F3579C4EE2531197F61B2375457026416863E6837B68547DC72ECCF590380695908FD2266F69668A891355CB07B", "agent" },
                    { 2, "A2D37ACFEF44E343C8927DC8BB2E034AE595F8BA1CBC4826B83A162B7316AD444CAB756B01588D3EDA4FBEA384CF8D35105D4D49A4A67CCE8A37FCAD24FFE77A", "three" },
                    { 3, "9730E3966623741F4CD74CA2B30C283BA35A7B9A23081E7AA4D418411A65D5280C7FC62B49474AE0F584F08AE4ED81C043DC383E8D448F507A902040696DA694", "hundred" },
                    { 4, "F952314A7CB04DD066E8262BA212ECE3A8E8D389274526BCAB36B583E576CE10B2B1AEA520AE90B44CF966E50181DFD4344CD24B1E970E9155BC75FDA04F3F93", "and" },
                    { 5, "92843A344FC34C6B315DC47DB798D9E5927F2B560C2EEDB72E4532679486F08CBBF239E3EBD963147986A60E27ED9A4303130689D747DFF1F7F5B383C7889941", "twenty" },
                    { 6, "3A36ACD803CD9E0A6B76E6380C0F4644500EF32EE1DDCCA7BEC288D0CB02708E3215FB9B7BDA2A54306A6445DA30F221ABB931B3BB5EE18224FB535470F55060", "seven" },
                    { 7, "C7F49C357C2042E8F32D53E29F33896A4A10A3EDB8135C8E45A8FE0AE4AA59BE0A50485A1CF954EE033A522D2A136224D7F228EC654DF5ACE9D950222615C611", "operation" },
                    { 8, "87EFEC91CEACDC6002B1BD25C1C6F7EA030C7D862A1B7C12B8B0A9990E7A7C2B4ABF89D9ACE5E1DEC117C9110D97BE1C8FAAEB314CC2683E496463E79AECA4A4", "barbershop" },
                    { 9, "E5174FE396755C5A9C2298344B758BF0429CDADBB5CEA9D3657F6AF8F0FF81C5EBD18114D2ACFA551E97A8AF64230CEB56E697E52CA4F122D275D17887CCD234", "big" },
                    { 10, "605D795D46EDE50E397CF9738BC178E160045F24173F7F6B379BBD710F452A442A061A5FEDED1F3C7350563853F2D7F412E2C64A47B4B77956C0C8CF45726B2B", "buck" },
                    { 11, "0B183278B9EC4FBBF9FD7A6CF71191F3C2F9AA2900B272D5FB6D5115DFC9F8E37B73A38C8790C16C88A10EFF89F3D4747971B39B1D9EC295957F365F34A4BAED", "bunny" },
                    { 12, "08E87AC6C87C9E72BA27A00787AF7F41B1E6CF713FF19B82E3BFAF5C2033AE0E9D33360285BFC286923C79678F4AB1961F4A615A547024C1203B39E4CF256627", "coffee" },
                    { 13, "7DE0F9ED5EE7D87C8BA957EAB554BF7FF434F8D7BB3248A86D7970062CE34989D4413303DE0CC1835FEC01A9D8C2C77C5AB1B742F3E6F973C3A783FBB4444FDB", "run" },
                    { 14, "3AAE2745CC1985EF3AB324F9A49A91A97B48878F24F7FB1C9B19B79E4C47CBE5F6C0A0F5FA636A918EE788582EC4ABC942863880803AEC5E663CDD509B646124", "hero" },
                    { 15, "D791311ECD55380BC434619AA3F876C77596A7BFBC4A8C00E39E38D05C5AFE115932877C8758D7284BBB0EDFED0E28AB8318B0D68259EA8BD870C5E1B0BEC4E7", "spring" },
                    { 16, "E47A2E93157FE103F49F71DE1D04CA6E31F47F6DFFADAA2CDA654B71C61BB0E4038235F5C97C09323590BB084EF5BD30C58B555689FE835668A2DC68232C548F", "caminandes" }
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
                values: new object[] { 1, 1, 8191, "https://s3.us-central-1.wasabisys.com/dustypig/media/profile_grey.png", true, false, "Test User", 0, null, (byte)1, false });

            migrationBuilder.InsertData(
                table: "MediaEntries",
                columns: new[] { "Id", "Added", "ArtworkUrl", "BackdropUrl", "BifUrl", "CreditsStartTime", "Date", "Description", "EntryType", "Episode", "ExtraSortOrder", "Genres", "Hash", "IntroEndTime", "IntroStartTime", "Length", "LibraryId", "LinkedToId", "Popularity", "PopularityUpdated", "Rated", "Season", "SortTitle", "TMDB_Id", "Title", "VideoUrl", "Xid" },
                values: new object[,]
                {
                    { 1, new DateTime(2021, 9, 6, 5, 20, 38, 399, DateTimeKind.Unspecified).AddTicks(9293), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.backdrop.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.bif", 205.875, new DateTime(2017, 5, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Agent 327 is investigating a clue that leads him to a shady barbershop in Amsterdam. Little does he know that he is being tailed by mercenary Boris Kloris.", 1, null, null, 4195332L, "4EA15C97603CE91602141FB1D5D04F5705311AEA6BB1FFD3B0AF4801BB7FE5A9B867B08AD8E2E90BBFCF70583E85EC20D9D0816E6B55FD34D16123A9F03624B1", null, null, 231.47999999999999, 1, null, null, null, 1, null, "agent 327: operation barbershop", 457784, "Agent 327: Operation Barbershop", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.mp4", null },
                    { 2, new DateTime(2021, 9, 6, 5, 20, 38, 454, DateTimeKind.Unspecified).AddTicks(1594), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.backdrop.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.bif", 490.25, new DateTime(2008, 4, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Follow a day of the life of Big Buck Bunny when he meets three bullying rodents: Frank, Rinky, and Gamera. The rodents amuse themselves by harassing helpless creatures by throwing fruits, nuts and rocks at them. After the deaths of two of Bunny's favorite butterflies, and an offensive attack on Bunny himself, Bunny sets aside his gentle nature and orchestrates a complex plan for revenge.", 1, null, null, 1092L, "9646997A1E9CDA5FC57353A2F7A6CEE4B58BBCADDBE9D921E09F75396C1F07682CACE518417BB111644EE79D2C0ECBAFB52F21BB652D7FF2B8A231ADB40C8015", null, null, 596.47400000000005, 1, null, null, null, 1, null, "big buck bunny", 10378, "Big Buck Bunny", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Big%20Buck%20Bunny%20%282008%29.mp4", null },
                    { 3, new DateTime(2021, 9, 6, 5, 20, 38, 506, DateTimeKind.Unspecified).AddTicks(3620), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.backdrop.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.bif", 164.083, new DateTime(2020, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fueled by caffeine, a young woman runs through the bittersweet memories of her past relationship.", 1, null, null, 1029L, "6BEC6E139049C49C2FF1C531876D0D7F79D8FFAB42F8F29074388B50FE045A09003E4F87E677A842D6D35A6D3DF30B3274BF17E40A613A5155804566BF2E4DFD", 6.7919999999999998, 0.0, 184.59899999999999, 1, null, null, null, 1, null, "coffee run", 717986, "Coffee Run", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Coffee%20Run%20%282020%29.mp4", null },
                    { 4, new DateTime(2021, 9, 6, 5, 20, 38, 554, DateTimeKind.Unspecified).AddTicks(5793), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.backdrop.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.bif", 147.31399999999999, new DateTime(2018, 4, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hero is a showcase for the updated Grease Pencil tools in Blender 2.80. Grease Pencil means 2D animation tools within a full 3D pipeline.", 1, null, null, 1030L, "B79323E58B71C58E7A256FE320B5791941DE97A3F748EF1AEBA6DA8D7D38E875557D3553F0D1AEAD359F5AA8DDE4DED456484F9F7D5483D05836B17F81C54581", 4.8380000000000001, 0.0, 236.65799999999999, 1, null, null, null, 1, null, "hero", 615324, "Hero", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Hero%20%282018%29.mp4", null },
                    { 5, new DateTime(2021, 9, 6, 5, 20, 38, 601, DateTimeKind.Unspecified).AddTicks(9560), "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.backdrop.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.bif", 427.79199999999997, new DateTime(2019, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "The story of a shepherd girl and her dog who face ancient spirits in order to continue the cycle of life.", 1, null, null, 3076L, "027B24E5D65C5FA431D54AD24F16DD801851CE3F95615CDD3951A5141D79AFD7B01688C0B2E83060948888B339160411DD1E116FFB018E440943BFD99B022291", null, null, 464.09800000000001, 1, null, null, null, 1, null, "spring", 593048, "Spring", "https://s3.us-central-1.wasabisys.com/dustypig/media/Movies/Spring%20%282019%29.mp4", null },
                    { 6, null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/show.jpg", "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/backdrop.jpg", null, null, null, "The Caminandes cartoon series follows our hero Koro the Llama as he explores Patagonia, attempts to overcome various obstacles, and becomes friends with Oti the pesky penguin.", 2, null, null, 1060L, "E47A2E93157FE103F49F71DE1D04CA6E31F47F6DFFADAA2CDA654B71C61BB0E4038235F5C97C09323590BB084EF5BD30C58B555689FE835668A2DC68232C548F", null, null, null, 2, null, null, null, 256, null, "caminandes", 276116, "Caminandes", null, null }
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
                columns: new[] { "Id", "Added", "ArtworkUrl", "BackdropUrl", "BifUrl", "CreditsStartTime", "Date", "Description", "EntryType", "Episode", "ExtraSortOrder", "Genres", "Hash", "IntroEndTime", "IntroStartTime", "Length", "LibraryId", "LinkedToId", "Popularity", "PopularityUpdated", "Rated", "Season", "SortTitle", "TMDB_Id", "Title", "VideoUrl", "Xid" },
                values: new object[,]
                {
                    { 7, new DateTime(2021, 9, 6, 5, 20, 39, 559, DateTimeKind.Unspecified).AddTicks(3737), "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.bif", 139.5, new DateTime(2013, 12, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro meets Oti, a pesky Magellanic penguin, in an epic battle over tasty red berries during the winter.", 3, 3, null, null, "41B2658FEE67627A1F641E585A1AABAE1267C2E2331FD4101A8EE2743A9E9B67A0EC7F20DE6A3CD9E22BD8BFAD3CA3CF2BA27F2236DE25C2413611D2A930DA6A", null, null, 150.048, 2, 6, null, null, null, 1, null, 0, "Llamigos", "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.mp4", 10003L },
                    { 8, new DateTime(2021, 9, 6, 5, 20, 38, 559, DateTimeKind.Unspecified).AddTicks(3102), "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.bif", 119.25, new DateTime(2013, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro hunts for food on the other side of a fence and is once again inspired by the Armadillo but this time to a shocking effect.", 3, 2, null, null, "94A2E1F9E973552DB73220BED6842AD4EED820B3F530A387177EAC70189A4F50A2B6D4A0E705DC728F613E43BF2C87E19E6B44ABE003A12B1F1384A68DFB8614", null, null, 146.00800000000001, 2, 6, null, null, null, 1, null, 0, "Gran Dillama", "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.mp4", 10002L },
                    { 9, new DateTime(2021, 9, 6, 5, 20, 37, 559, DateTimeKind.Unspecified).AddTicks(1031), "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.jpg", null, "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.bif", 87.917000000000002, new DateTime(2013, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro has trouble crossing an apparent desolate road, a problem that an unwitting Armadillo does not share.", 3, 1, null, null, "32B523CD69F065EB37CDE5C1BD8A18E6C1367A27608F450A3479A6AF5F98D0391A3D9ACB869614815F6D0A01954F89412390DC60A85161FC65398C3D91960DD8", null, null, 90.001000000000005, 2, 6, null, null, null, 1, null, 0, "Llama Drama", "https://s3.us-central-1.wasabisys.com/dustypig/media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.mp4", 10001L }
                });

            migrationBuilder.InsertData(
                table: "MediaSearchBridges",
                columns: new[] { "MediaEntryId", "SearchTermId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 2 },
                    { 1, 3 },
                    { 1, 4 },
                    { 1, 5 },
                    { 1, 6 },
                    { 1, 7 },
                    { 1, 8 },
                    { 2, 9 },
                    { 2, 10 },
                    { 2, 11 },
                    { 3, 12 },
                    { 3, 13 },
                    { 4, 14 },
                    { 5, 15 },
                    { 6, 16 }
                });

            migrationBuilder.InsertData(
                table: "ProfileMediaProgresses",
                columns: new[] { "MediaEntryId", "ProfileId", "Played", "Timestamp", "Xid" },
                values: new object[,]
                {
                    { 1, 1, 10.0, new DateTime(2021, 9, 24, 11, 35, 0, 0, DateTimeKind.Unspecified), null },
                    { 2, 1, 180.0, new DateTime(2021, 9, 24, 11, 34, 0, 0, DateTimeKind.Unspecified), null }
                });

            migrationBuilder.InsertData(
                table: "ProfileMediaProgresses",
                columns: new[] { "MediaEntryId", "ProfileId", "Played", "Timestamp", "Xid" },
                values: new object[] { 8, 1, 30.0, new DateTime(2021, 9, 24, 13, 12, 14, 344, DateTimeKind.Unspecified).AddTicks(8000), null });

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
                name: "IX_GetRequests_AccountId_EntryType_TMDB_Id",
                table: "GetRequests",
                columns: new[] { "AccountId", "EntryType", "TMDB_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GetRequests_ProfileId",
                table: "GetRequests",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_GetRequestSubscriptions_ProfileId",
                table: "GetRequestSubscriptions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_AccountId_Name",
                table: "Libraries",
                columns: new[] { "AccountId", "Name" },
                unique: true);

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
                name: "IX_MediaPersonBridges_PersonId",
                table: "MediaPersonBridges",
                column: "PersonId");

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
                name: "IX_Notifications_ProfileId",
                table: "Notifications",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TitleOverrideId",
                table: "Notifications",
                column: "TitleOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_People_Hash",
                table: "People",
                column: "Hash",
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
                name: "IX_SearchTerms_Hash",
                table: "SearchTerms",
                column: "Hash",
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
                name: "IX_TitleOverrides_MediaEntryId",
                table: "TitleOverrides",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleOverrides_ProfileId_MediaEntryId",
                table: "TitleOverrides",
                columns: new[] { "ProfileId", "MediaEntryId" },
                unique: true);

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
                name: "GetRequestSubscriptions");

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
                name: "TitleOverrides");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "MediaEntries");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
