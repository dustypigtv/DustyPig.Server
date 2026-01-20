using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirebaseId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TMDB_Entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TMDB_Id = table.Column<int>(type: "integer", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    MovieRating = table.Column<int>(type: "integer", nullable: true),
                    TVRating = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    BackdropUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Popularity = table.Column<double>(type: "double precision", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMDB_Entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TMDB_People",
                columns: table => new
                {
                    TMDB_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMDB_People", x => x.TMDB_Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
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
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Account1Id = table.Column<int>(type: "integer", nullable: false),
                    Account2Id = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayName2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "GetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    TMDB_Id = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Libraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsTV = table.Column<bool>(type: "boolean", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    PinNumber = table.Column<int>(type: "integer", nullable: true),
                    TitleRequestPermission = table.Column<byte>(type: "smallint", nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    MaxMovieRating = table.Column<int>(type: "integer", nullable: false),
                    MaxTVRating = table.Column<int>(type: "integer", nullable: false)
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
                name: "TMDB_EntryPeopleBridges",
                columns: table => new
                {
                    TMDB_EntryId = table.Column<int>(type: "integer", nullable: false),
                    TMDB_PersonId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMDB_EntryPeopleBridges", x => new { x.TMDB_EntryId, x.TMDB_PersonId, x.Role });
                    table.ForeignKey(
                        name: "FK_TMDB_EntryPeopleBridges_TMDB_Entries_TMDB_EntryId",
                        column: x => x.TMDB_EntryId,
                        principalTable: "TMDB_Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TMDB_EntryPeopleBridges_TMDB_People_TMDB_PersonId",
                        column: x => x.TMDB_PersonId,
                        principalTable: "TMDB_People",
                        principalColumn: "TMDB_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FriendLibraryShares",
                columns: table => new
                {
                    LibraryId = table.Column<int>(type: "integer", nullable: false),
                    FriendshipId = table.Column<int>(type: "integer", nullable: false),
                    LibraryDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LibraryId = table.Column<int>(type: "integer", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    TMDB_Id = table.Column<int>(type: "integer", nullable: true),
                    TMDB_EntryId = table.Column<int>(type: "integer", nullable: true),
                    TMDB_Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SortTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    LinkedToId = table.Column<int>(type: "integer", nullable: true),
                    Season = table.Column<int>(type: "integer", nullable: true),
                    Episode = table.Column<int>(type: "integer", nullable: true),
                    Xid = table.Column<long>(type: "bigint", nullable: true),
                    ExtraSortOrder = table.Column<int>(type: "integer", nullable: true),
                    Length = table.Column<double>(type: "double precision", nullable: true),
                    IntroStartTime = table.Column<double>(type: "double precision", nullable: true),
                    IntroEndTime = table.Column<double>(type: "double precision", nullable: true),
                    CreditsStartTime = table.Column<double>(type: "double precision", nullable: true),
                    ArtworkUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BackdropUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BifUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Popularity = table.Column<double>(type: "double precision", nullable: true),
                    MovieRating = table.Column<int>(type: "integer", nullable: true),
                    TVRating = table.Column<int>(type: "integer", nullable: true),
                    Genre_Action = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Adventure = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Animation = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Anime = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Awards_Show = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Children = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Comedy = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Crime = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Documentary = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Drama = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Family = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Fantasy = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Food = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Game_Show = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_History = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Home_and_Garden = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Horror = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Indie = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Martial_Arts = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Mini_Series = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Music = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Musical = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Mystery = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_News = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Podcast = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Political = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Reality = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Romance = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Science_Fiction = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Soap = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Sports = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Suspense = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Talk_Show = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Thriller = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Travel = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_TV_Movie = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_War = table.Column<bool>(type: "boolean", nullable: false),
                    Genre_Western = table.Column<bool>(type: "boolean", nullable: false),
                    EverPlayed = table.Column<bool>(type: "boolean", nullable: false),
                    SearchTitle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExtraSearchTerms = table.Column<List<string>>(type: "text[]", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_MediaEntries_TMDB_Entries_TMDB_EntryId",
                        column: x => x.TMDB_EntryId,
                        principalTable: "TMDB_Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActivationCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationCodes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_ActivationCodes_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FCMTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FCMTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FCMTokens_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GetRequestSubscriptions",
                columns: table => new
                {
                    GetRequestId = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentItemId = table.Column<int>(type: "integer", nullable: false),
                    CurrentProgress = table.Column<double>(type: "double precision", nullable: false),
                    ProgressTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArtworkUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BackdropUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ArtworkUpdateNeeded = table.Column<bool>(type: "boolean", nullable: false)
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
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    LibraryId = table.Column<int>(type: "integer", nullable: false)
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
                name: "ProfileMediaProgresses",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: false),
                    Xid = table.Column<long>(type: "bigint", nullable: true),
                    Played = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: false)
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
                name: "TitleOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "WatchListItems",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: false),
                    Added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "AutoPlaylistSeries",
                columns: table => new
                {
                    PlaylistId = table.Column<int>(type: "integer", nullable: false),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoPlaylistSeries", x => new { x.PlaylistId, x.MediaEntryId });
                    table.ForeignKey(
                        name: "FK_AutoPlaylistSeries_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoPlaylistSeries_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaylistId = table.Column<int>(type: "integer", nullable: false),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NotificationType = table.Column<int>(type: "integer", nullable: false),
                    FriendshipId = table.Column<int>(type: "integer", nullable: true),
                    MediaEntryId = table.Column<int>(type: "integer", nullable: true),
                    GetRequestId = table.Column<int>(type: "integer", nullable: true),
                    TitleOverrideId = table.Column<int>(type: "integer", nullable: true),
                    Sent = table.Column<bool>(type: "boolean", nullable: false),
                    Seen = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Friendships_FriendshipId",
                        column: x => x.FriendshipId,
                        principalTable: "Friendships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_GetRequests_GetRequestId",
                        column: x => x.GetRequestId,
                        principalTable: "GetRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                name: "IX_ActivationCodes_ProfileId",
                table: "ActivationCodes",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPlaylistSeries_MediaEntryId",
                table: "AutoPlaylistSeries",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FCMTokens_Hash",
                table: "FCMTokens",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FCMTokens_ProfileId",
                table: "FCMTokens",
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
                name: "IX_GetRequestSubscriptions_ProfileId",
                table: "GetRequestSubscriptions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_AccountId_Name",
                table: "Libraries",
                columns: new[] { "AccountId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Added",
                table: "MediaEntries",
                column: "Added");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_EntryType",
                table: "MediaEntries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Action",
                table: "MediaEntries",
                column: "Genre_Action");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Adventure",
                table: "MediaEntries",
                column: "Genre_Adventure");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Animation",
                table: "MediaEntries",
                column: "Genre_Animation");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Anime",
                table: "MediaEntries",
                column: "Genre_Anime");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Awards_Show",
                table: "MediaEntries",
                column: "Genre_Awards_Show");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Children",
                table: "MediaEntries",
                column: "Genre_Children");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Comedy",
                table: "MediaEntries",
                column: "Genre_Comedy");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Crime",
                table: "MediaEntries",
                column: "Genre_Crime");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Documentary",
                table: "MediaEntries",
                column: "Genre_Documentary");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Drama",
                table: "MediaEntries",
                column: "Genre_Drama");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Family",
                table: "MediaEntries",
                column: "Genre_Family");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Fantasy",
                table: "MediaEntries",
                column: "Genre_Fantasy");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Food",
                table: "MediaEntries",
                column: "Genre_Food");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Game_Show",
                table: "MediaEntries",
                column: "Genre_Game_Show");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_History",
                table: "MediaEntries",
                column: "Genre_History");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Home_and_Garden",
                table: "MediaEntries",
                column: "Genre_Home_and_Garden");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Horror",
                table: "MediaEntries",
                column: "Genre_Horror");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Indie",
                table: "MediaEntries",
                column: "Genre_Indie");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Martial_Arts",
                table: "MediaEntries",
                column: "Genre_Martial_Arts");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Mini_Series",
                table: "MediaEntries",
                column: "Genre_Mini_Series");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Music",
                table: "MediaEntries",
                column: "Genre_Music");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Musical",
                table: "MediaEntries",
                column: "Genre_Musical");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Mystery",
                table: "MediaEntries",
                column: "Genre_Mystery");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_News",
                table: "MediaEntries",
                column: "Genre_News");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Podcast",
                table: "MediaEntries",
                column: "Genre_Podcast");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Political",
                table: "MediaEntries",
                column: "Genre_Political");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Reality",
                table: "MediaEntries",
                column: "Genre_Reality");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Romance",
                table: "MediaEntries",
                column: "Genre_Romance");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Science_Fiction",
                table: "MediaEntries",
                column: "Genre_Science_Fiction");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Soap",
                table: "MediaEntries",
                column: "Genre_Soap");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Sports",
                table: "MediaEntries",
                column: "Genre_Sports");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Suspense",
                table: "MediaEntries",
                column: "Genre_Suspense");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Talk_Show",
                table: "MediaEntries",
                column: "Genre_Talk_Show");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Thriller",
                table: "MediaEntries",
                column: "Genre_Thriller");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Travel",
                table: "MediaEntries",
                column: "Genre_Travel");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_TV_Movie",
                table: "MediaEntries",
                column: "Genre_TV_Movie");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_War",
                table: "MediaEntries",
                column: "Genre_War");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Western",
                table: "MediaEntries",
                column: "Genre_Western");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_LibraryId",
                table: "MediaEntries",
                column: "LibraryId");

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
                name: "IX_MediaEntries_MovieRating",
                table: "MediaEntries",
                column: "MovieRating");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries",
                column: "Popularity");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TMDB_EntryId",
                table: "MediaEntries",
                column: "TMDB_EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TMDB_Id",
                table: "MediaEntries",
                column: "TMDB_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TVRating",
                table: "MediaEntries",
                column: "TVRating");

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
                name: "IX_PlaylistItems_MediaEntryId",
                table: "PlaylistItems",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistId",
                table: "PlaylistItems",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_ProfileId_Name",
                table: "Playlists",
                columns: new[] { "ProfileId", "Name" },
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
                name: "IX_Subscriptions_MediaEntryId",
                table: "Subscriptions",
                column: "MediaEntryId");

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
                name: "IX_TMDB_Entries_MediaType",
                table: "TMDB_Entries",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_Entries_TMDB_Id",
                table: "TMDB_Entries",
                column: "TMDB_Id");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_Entries_TMDB_Id_MediaType",
                table: "TMDB_Entries",
                columns: new[] { "TMDB_Id", "MediaType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_EntryPeopleBridges_TMDB_PersonId",
                table: "TMDB_EntryPeopleBridges",
                column: "TMDB_PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchListItems_MediaEntryId",
                table: "WatchListItems",
                column: "MediaEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountTokens");

            migrationBuilder.DropTable(
                name: "ActivationCodes");

            migrationBuilder.DropTable(
                name: "AutoPlaylistSeries");

            migrationBuilder.DropTable(
                name: "FCMTokens");

            migrationBuilder.DropTable(
                name: "FriendLibraryShares");

            migrationBuilder.DropTable(
                name: "GetRequestSubscriptions");

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
                name: "TMDB_EntryPeopleBridges");

            migrationBuilder.DropTable(
                name: "WatchListItems");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "GetRequests");

            migrationBuilder.DropTable(
                name: "TitleOverrides");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "TMDB_People");

            migrationBuilder.DropTable(
                name: "MediaEntries");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Libraries");

            migrationBuilder.DropTable(
                name: "TMDB_Entries");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
