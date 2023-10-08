using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Genres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 4 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 5 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 6 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 7 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 1, 8 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 2, 9 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 2, 10 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 2, 11 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 3, 12 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 3, 13 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 4, 14 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 5, 15 });

            migrationBuilder.DeleteData(
                table: "MediaSearchBridges",
                keyColumns: new[] { "MediaEntryId", "SearchTermId" },
                keyValues: new object[] { 6, 16 });

            migrationBuilder.DeleteData(
                table: "ProfileLibraryShares",
                keyColumns: new[] { "LibraryId", "ProfileId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "ProfileLibraryShares",
                keyColumns: new[] { "LibraryId", "ProfileId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "ProfileMediaProgresses",
                keyColumns: new[] { "MediaEntryId", "ProfileId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "ProfileMediaProgresses",
                keyColumns: new[] { "MediaEntryId", "ProfileId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "ProfileMediaProgresses",
                keyColumns: new[] { "MediaEntryId", "ProfileId" },
                keyValues: new object[] { 8, 1 });

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Profiles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "SearchTerms",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Libraries",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Libraries",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "AllowedRatings",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Genres",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Rated",
                table: "MediaEntries");

            migrationBuilder.RenameColumn(
                name: "MaxTVRatings",
                table: "Profiles",
                newName: "MaxTVRating");

            migrationBuilder.RenameColumn(
                name: "Western",
                table: "MediaEntries",
                newName: "Genre_Western");

            migrationBuilder.RenameColumn(
                name: "War",
                table: "MediaEntries",
                newName: "Genre_War");

            migrationBuilder.RenameColumn(
                name: "Travel",
                table: "MediaEntries",
                newName: "Genre_Travel");

            migrationBuilder.RenameColumn(
                name: "Thriller",
                table: "MediaEntries",
                newName: "Genre_Thriller");

            migrationBuilder.RenameColumn(
                name: "Talk_Show",
                table: "MediaEntries",
                newName: "Genre_Talk_Show");

            migrationBuilder.RenameColumn(
                name: "TV_Movie",
                table: "MediaEntries",
                newName: "Genre_TV_Movie");

            migrationBuilder.RenameColumn(
                name: "Suspense",
                table: "MediaEntries",
                newName: "Genre_Suspense");

            migrationBuilder.RenameColumn(
                name: "Sports",
                table: "MediaEntries",
                newName: "Genre_Sports");

            migrationBuilder.RenameColumn(
                name: "Soap",
                table: "MediaEntries",
                newName: "Genre_Soap");

            migrationBuilder.RenameColumn(
                name: "Science_Fiction",
                table: "MediaEntries",
                newName: "Genre_Science_Fiction");

            migrationBuilder.RenameColumn(
                name: "Romance",
                table: "MediaEntries",
                newName: "Genre_Romance");

            migrationBuilder.RenameColumn(
                name: "Reality",
                table: "MediaEntries",
                newName: "Genre_Reality");

            migrationBuilder.RenameColumn(
                name: "Political",
                table: "MediaEntries",
                newName: "Genre_Political");

            migrationBuilder.RenameColumn(
                name: "Podcast",
                table: "MediaEntries",
                newName: "Genre_Podcast");

            migrationBuilder.RenameColumn(
                name: "News",
                table: "MediaEntries",
                newName: "Genre_News");

            migrationBuilder.RenameColumn(
                name: "Mystery",
                table: "MediaEntries",
                newName: "Genre_Mystery");

            migrationBuilder.RenameColumn(
                name: "Musical",
                table: "MediaEntries",
                newName: "Genre_Musical");

            migrationBuilder.RenameColumn(
                name: "Music",
                table: "MediaEntries",
                newName: "Genre_Music");

            migrationBuilder.RenameColumn(
                name: "Mini_Series",
                table: "MediaEntries",
                newName: "Genre_Mini_Series");

            migrationBuilder.RenameColumn(
                name: "Martial_Arts",
                table: "MediaEntries",
                newName: "Genre_Martial_Arts");

            migrationBuilder.RenameColumn(
                name: "Indie",
                table: "MediaEntries",
                newName: "Genre_Indie");

            migrationBuilder.RenameColumn(
                name: "Horror",
                table: "MediaEntries",
                newName: "Genre_Horror");

            migrationBuilder.RenameColumn(
                name: "Home_and_Garden",
                table: "MediaEntries",
                newName: "Genre_Home_and_Garden");

            migrationBuilder.RenameColumn(
                name: "History",
                table: "MediaEntries",
                newName: "Genre_History");

            migrationBuilder.RenameColumn(
                name: "Game_Show",
                table: "MediaEntries",
                newName: "Genre_Game_Show");

            migrationBuilder.RenameColumn(
                name: "Food",
                table: "MediaEntries",
                newName: "Genre_Food");

            migrationBuilder.RenameColumn(
                name: "Fantasy",
                table: "MediaEntries",
                newName: "Genre_Fantasy");

            migrationBuilder.RenameColumn(
                name: "Family",
                table: "MediaEntries",
                newName: "Genre_Family");

            migrationBuilder.RenameColumn(
                name: "Drama",
                table: "MediaEntries",
                newName: "Genre_Drama");

            migrationBuilder.RenameColumn(
                name: "Documentary",
                table: "MediaEntries",
                newName: "Genre_Documentary");

            migrationBuilder.RenameColumn(
                name: "Crime",
                table: "MediaEntries",
                newName: "Genre_Crime");

            migrationBuilder.RenameColumn(
                name: "Comedy",
                table: "MediaEntries",
                newName: "Genre_Comedy");

            migrationBuilder.RenameColumn(
                name: "Children",
                table: "MediaEntries",
                newName: "Genre_Children");

            migrationBuilder.RenameColumn(
                name: "Awards_Show",
                table: "MediaEntries",
                newName: "Genre_Awards_Show");

            migrationBuilder.RenameColumn(
                name: "Anime",
                table: "MediaEntries",
                newName: "Genre_Anime");

            migrationBuilder.RenameColumn(
                name: "Animation",
                table: "MediaEntries",
                newName: "Genre_Animation");

            migrationBuilder.RenameColumn(
                name: "Adventure",
                table: "MediaEntries",
                newName: "Genre_Adventure");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "MediaEntries",
                newName: "Genre_Action");

            migrationBuilder.AddColumn<double>(
                name: "CurrentProgress",
                table: "Playlists",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "AvailableGenresResults",
                columns: table => new
                {
                    Genre_Action = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Adventure = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Animation = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Anime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Awards_Show = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Children = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Comedy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Crime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Documentary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Drama = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Family = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Fantasy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Food = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Game_Show = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_History = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Home_and_Garden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Horror = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Indie = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Martial_Arts = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Mini_Series = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Music = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Musical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Mystery = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_News = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Podcast = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Political = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Reality = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Romance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Science_Fiction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Soap = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Sports = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Suspense = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Talk_Show = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Thriller = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Travel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_TV_Movie = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_War = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Western = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SearchTerms_Term",
                table: "SearchTerms",
                column: "Term");

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
                name: "IX_MediaEntries_MovieRating",
                table: "MediaEntries",
                column: "MovieRating");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TVRating",
                table: "MediaEntries",
                column: "TVRating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailableGenresResults");

            migrationBuilder.DropIndex(
                name: "IX_SearchTerms_Term",
                table: "SearchTerms");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Action",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Adventure",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Animation",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Anime",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Awards_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Children",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Comedy",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Crime",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Documentary",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Drama",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Family",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Fantasy",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Food",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Game_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_History",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Home_and_Garden",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Horror",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Indie",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Martial_Arts",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Mini_Series",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Music",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Musical",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Mystery",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_News",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Podcast",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Political",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Reality",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Romance",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Science_Fiction",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Soap",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Sports",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Suspense",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Talk_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Thriller",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Travel",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_TV_Movie",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_War",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Western",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_MovieRating",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_TVRating",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "CurrentProgress",
                table: "Playlists");

            migrationBuilder.RenameColumn(
                name: "MaxTVRating",
                table: "Profiles",
                newName: "MaxTVRatings");

            migrationBuilder.RenameColumn(
                name: "Genre_Western",
                table: "MediaEntries",
                newName: "Western");

            migrationBuilder.RenameColumn(
                name: "Genre_War",
                table: "MediaEntries",
                newName: "War");

            migrationBuilder.RenameColumn(
                name: "Genre_Travel",
                table: "MediaEntries",
                newName: "Travel");

            migrationBuilder.RenameColumn(
                name: "Genre_Thriller",
                table: "MediaEntries",
                newName: "Thriller");

            migrationBuilder.RenameColumn(
                name: "Genre_Talk_Show",
                table: "MediaEntries",
                newName: "Talk_Show");

            migrationBuilder.RenameColumn(
                name: "Genre_TV_Movie",
                table: "MediaEntries",
                newName: "TV_Movie");

            migrationBuilder.RenameColumn(
                name: "Genre_Suspense",
                table: "MediaEntries",
                newName: "Suspense");

            migrationBuilder.RenameColumn(
                name: "Genre_Sports",
                table: "MediaEntries",
                newName: "Sports");

            migrationBuilder.RenameColumn(
                name: "Genre_Soap",
                table: "MediaEntries",
                newName: "Soap");

            migrationBuilder.RenameColumn(
                name: "Genre_Science_Fiction",
                table: "MediaEntries",
                newName: "Science_Fiction");

            migrationBuilder.RenameColumn(
                name: "Genre_Romance",
                table: "MediaEntries",
                newName: "Romance");

            migrationBuilder.RenameColumn(
                name: "Genre_Reality",
                table: "MediaEntries",
                newName: "Reality");

            migrationBuilder.RenameColumn(
                name: "Genre_Political",
                table: "MediaEntries",
                newName: "Political");

            migrationBuilder.RenameColumn(
                name: "Genre_Podcast",
                table: "MediaEntries",
                newName: "Podcast");

            migrationBuilder.RenameColumn(
                name: "Genre_News",
                table: "MediaEntries",
                newName: "News");

            migrationBuilder.RenameColumn(
                name: "Genre_Mystery",
                table: "MediaEntries",
                newName: "Mystery");

            migrationBuilder.RenameColumn(
                name: "Genre_Musical",
                table: "MediaEntries",
                newName: "Musical");

            migrationBuilder.RenameColumn(
                name: "Genre_Music",
                table: "MediaEntries",
                newName: "Music");

            migrationBuilder.RenameColumn(
                name: "Genre_Mini_Series",
                table: "MediaEntries",
                newName: "Mini_Series");

            migrationBuilder.RenameColumn(
                name: "Genre_Martial_Arts",
                table: "MediaEntries",
                newName: "Martial_Arts");

            migrationBuilder.RenameColumn(
                name: "Genre_Indie",
                table: "MediaEntries",
                newName: "Indie");

            migrationBuilder.RenameColumn(
                name: "Genre_Horror",
                table: "MediaEntries",
                newName: "Horror");

            migrationBuilder.RenameColumn(
                name: "Genre_Home_and_Garden",
                table: "MediaEntries",
                newName: "Home_and_Garden");

            migrationBuilder.RenameColumn(
                name: "Genre_History",
                table: "MediaEntries",
                newName: "History");

            migrationBuilder.RenameColumn(
                name: "Genre_Game_Show",
                table: "MediaEntries",
                newName: "Game_Show");

            migrationBuilder.RenameColumn(
                name: "Genre_Food",
                table: "MediaEntries",
                newName: "Food");

            migrationBuilder.RenameColumn(
                name: "Genre_Fantasy",
                table: "MediaEntries",
                newName: "Fantasy");

            migrationBuilder.RenameColumn(
                name: "Genre_Family",
                table: "MediaEntries",
                newName: "Family");

            migrationBuilder.RenameColumn(
                name: "Genre_Drama",
                table: "MediaEntries",
                newName: "Drama");

            migrationBuilder.RenameColumn(
                name: "Genre_Documentary",
                table: "MediaEntries",
                newName: "Documentary");

            migrationBuilder.RenameColumn(
                name: "Genre_Crime",
                table: "MediaEntries",
                newName: "Crime");

            migrationBuilder.RenameColumn(
                name: "Genre_Comedy",
                table: "MediaEntries",
                newName: "Comedy");

            migrationBuilder.RenameColumn(
                name: "Genre_Children",
                table: "MediaEntries",
                newName: "Children");

            migrationBuilder.RenameColumn(
                name: "Genre_Awards_Show",
                table: "MediaEntries",
                newName: "Awards_Show");

            migrationBuilder.RenameColumn(
                name: "Genre_Anime",
                table: "MediaEntries",
                newName: "Anime");

            migrationBuilder.RenameColumn(
                name: "Genre_Animation",
                table: "MediaEntries",
                newName: "Animation");

            migrationBuilder.RenameColumn(
                name: "Genre_Adventure",
                table: "MediaEntries",
                newName: "Adventure");

            migrationBuilder.RenameColumn(
                name: "Genre_Action",
                table: "MediaEntries",
                newName: "Action");

            migrationBuilder.AddColumn<int>(
                name: "AllowedRatings",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "Genres",
                table: "MediaEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rated",
                table: "MediaEntries",
                type: "int",
                nullable: true);

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
                values: new object[,]
                {
                    { 1, 1, false, "Movies" },
                    { 2, 1, true, "TV Shows" }
                });

            migrationBuilder.InsertData(
                table: "Profiles",
                columns: new[] { "Id", "AccountId", "AllowedRatings", "AvatarUrl", "IsMain", "Locked", "MaxMovieRating", "MaxTVRatings", "Name", "PinNumber", "TitleRequestPermission" },
                values: new object[] { 1, 1, 8191, "https://s3.dustypig.tv/demo-media/profile_grey.png", true, false, 0, 0, "Test User", null, (byte)1 });

            migrationBuilder.InsertData(
                table: "MediaEntries",
                columns: new[] { "Id", "Action", "Added", "Adventure", "Animation", "Anime", "ArtworkUrl", "Awards_Show", "BackdropUrl", "BifUrl", "Children", "Comedy", "CreditsStartTime", "Crime", "Date", "Description", "Documentary", "Drama", "EntryType", "Episode", "ExtraSortOrder", "Family", "Fantasy", "Food", "Game_Show", "Genres", "Hash", "History", "Home_and_Garden", "Horror", "Indie", "IntroEndTime", "IntroStartTime", "Length", "LibraryId", "LinkedToId", "Martial_Arts", "Mini_Series", "MovieRating", "Music", "Musical", "Mystery", "News", "Podcast", "Political", "Popularity", "PopularityUpdated", "Rated", "Reality", "Romance", "Science_Fiction", "Season", "Soap", "SortTitle", "Sports", "Suspense", "TMDB_Id", "TVRating", "TV_Movie", "Talk_Show", "Thriller", "Title", "Travel", "VideoUrl", "War", "Western", "Xid" },
                values: new object[,]
                {
                    { 1, false, new DateTime(2021, 9, 6, 5, 20, 38, 399, DateTimeKind.Unspecified).AddTicks(9293), false, false, false, "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.jpg", false, "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.backdrop.jpg", "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.bif", false, false, 205.875, false, new DateTime(2017, 5, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Agent 327 is investigating a clue that leads him to a shady barbershop in Amsterdam. Little does he know that he is being tailed by mercenary Boris Kloris.", false, false, 1, null, null, false, false, false, false, 4195332L, "4EA15C97603CE91602141FB1D5D04F5705311AEA6BB1FFD3B0AF4801BB7FE5A9B867B08AD8E2E90BBFCF70583E85EC20D9D0816E6B55FD34D16123A9F03624B1", false, false, false, false, null, null, 231.47999999999999, 1, null, false, false, null, false, false, false, false, false, false, null, null, 1, false, false, false, null, false, "agent 327: operation barbershop", false, false, 457784, null, false, false, false, "Agent 327: Operation Barbershop", false, "https://s3.dustypig.tv/demo-media/Movies/Agent%20327_%20Operation%20Barbershop%20%282017%29.mp4", false, false, null },
                    { 2, false, new DateTime(2021, 9, 6, 5, 20, 38, 454, DateTimeKind.Unspecified).AddTicks(1594), false, false, false, "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.jpg", false, "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.backdrop.jpg", "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.bif", false, false, 490.25, false, new DateTime(2008, 4, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Follow a day of the life of Big Buck Bunny when he meets three bullying rodents: Frank, Rinky, and Gamera. The rodents amuse themselves by harassing helpless creatures by throwing fruits, nuts and rocks at them. After the deaths of two of Bunny's favorite butterflies, and an offensive attack on Bunny himself, Bunny sets aside his gentle nature and orchestrates a complex plan for revenge.", false, false, 1, null, null, false, false, false, false, 1092L, "9646997A1E9CDA5FC57353A2F7A6CEE4B58BBCADDBE9D921E09F75396C1F07682CACE518417BB111644EE79D2C0ECBAFB52F21BB652D7FF2B8A231ADB40C8015", false, false, false, false, null, null, 596.47400000000005, 1, null, false, false, null, false, false, false, false, false, false, null, null, 1, false, false, false, null, false, "big buck bunny", false, false, 10378, null, false, false, false, "Big Buck Bunny", false, "https://s3.dustypig.tv/demo-media/Movies/Big%20Buck%20Bunny%20%282008%29.mp4", false, false, null },
                    { 3, false, new DateTime(2021, 9, 6, 5, 20, 38, 506, DateTimeKind.Unspecified).AddTicks(3620), false, false, false, "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.jpg", false, "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.backdrop.jpg", "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.bif", false, false, 164.083, false, new DateTime(2020, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Fueled by caffeine, a young woman runs through the bittersweet memories of her past relationship.", false, false, 1, null, null, false, false, false, false, 1029L, "6BEC6E139049C49C2FF1C531876D0D7F79D8FFAB42F8F29074388B50FE045A09003E4F87E677A842D6D35A6D3DF30B3274BF17E40A613A5155804566BF2E4DFD", false, false, false, false, 6.7919999999999998, 0.0, 184.59899999999999, 1, null, false, false, null, false, false, false, false, false, false, null, null, 1, false, false, false, null, false, "coffee run", false, false, 717986, null, false, false, false, "Coffee Run", false, "https://s3.dustypig.tv/demo-media/Movies/Coffee%20Run%20%282020%29.mp4", false, false, null },
                    { 4, false, new DateTime(2021, 9, 6, 5, 20, 38, 554, DateTimeKind.Unspecified).AddTicks(5793), false, false, false, "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.jpg", false, "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.backdrop.jpg", "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.bif", false, false, 147.31399999999999, false, new DateTime(2018, 4, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hero is a showcase for the updated Grease Pencil tools in Blender 2.80. Grease Pencil means 2D animation tools within a full 3D pipeline.", false, false, 1, null, null, false, false, false, false, 1030L, "B79323E58B71C58E7A256FE320B5791941DE97A3F748EF1AEBA6DA8D7D38E875557D3553F0D1AEAD359F5AA8DDE4DED456484F9F7D5483D05836B17F81C54581", false, false, false, false, 4.8380000000000001, 0.0, 236.65799999999999, 1, null, false, false, null, false, false, false, false, false, false, null, null, 1, false, false, false, null, false, "hero", false, false, 615324, null, false, false, false, "Hero", false, "https://s3.dustypig.tv/demo-media/Movies/Hero%20%282018%29.mp4", false, false, null },
                    { 5, false, new DateTime(2021, 9, 6, 5, 20, 38, 601, DateTimeKind.Unspecified).AddTicks(9560), false, false, false, "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.jpg", false, "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.backdrop.jpg", "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.bif", false, false, 427.79199999999997, false, new DateTime(2019, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "The story of a shepherd girl and her dog who face ancient spirits in order to continue the cycle of life.", false, false, 1, null, null, false, false, false, false, 3076L, "027B24E5D65C5FA431D54AD24F16DD801851CE3F95615CDD3951A5141D79AFD7B01688C0B2E83060948888B339160411DD1E116FFB018E440943BFD99B022291", false, false, false, false, null, null, 464.09800000000001, 1, null, false, false, null, false, false, false, false, false, false, null, null, 1, false, false, false, null, false, "spring", false, false, 593048, null, false, false, false, "Spring", false, "https://s3.dustypig.tv/demo-media/Movies/Spring%20%282019%29.mp4", false, false, null },
                    { 6, false, null, false, false, false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/show.jpg", false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/backdrop.jpg", null, false, false, null, false, null, "The Caminandes cartoon series follows our hero Koro the Llama as he explores Patagonia, attempts to overcome various obstacles, and becomes friends with Oti the pesky penguin.", false, false, 2, null, null, false, false, false, false, 1060L, "E47A2E93157FE103F49F71DE1D04CA6E31F47F6DFFADAA2CDA654B71C61BB0E4038235F5C97C09323590BB084EF5BD30C58B555689FE835668A2DC68232C548F", false, false, false, false, null, null, null, 2, null, false, false, null, false, false, false, false, false, false, null, null, 256, false, false, false, null, false, "caminandes", false, false, 276116, null, false, false, false, "Caminandes", false, null, false, false, null }
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
                columns: new[] { "Id", "Action", "Added", "Adventure", "Animation", "Anime", "ArtworkUrl", "Awards_Show", "BackdropUrl", "BifUrl", "Children", "Comedy", "CreditsStartTime", "Crime", "Date", "Description", "Documentary", "Drama", "EntryType", "Episode", "ExtraSortOrder", "Family", "Fantasy", "Food", "Game_Show", "Genres", "Hash", "History", "Home_and_Garden", "Horror", "Indie", "IntroEndTime", "IntroStartTime", "Length", "LibraryId", "LinkedToId", "Martial_Arts", "Mini_Series", "MovieRating", "Music", "Musical", "Mystery", "News", "Podcast", "Political", "Popularity", "PopularityUpdated", "Rated", "Reality", "Romance", "Science_Fiction", "Season", "Soap", "SortTitle", "Sports", "Suspense", "TMDB_Id", "TVRating", "TV_Movie", "Talk_Show", "Thriller", "Title", "Travel", "VideoUrl", "War", "Western", "Xid" },
                values: new object[,]
                {
                    { 7, false, new DateTime(2021, 9, 6, 5, 20, 39, 559, DateTimeKind.Unspecified).AddTicks(3737), false, false, false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.jpg", false, null, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.bif", false, false, 139.5, false, new DateTime(2013, 12, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro meets Oti, a pesky Magellanic penguin, in an epic battle over tasty red berries during the winter.", false, false, 3, 3, null, false, false, false, false, null, "41B2658FEE67627A1F641E585A1AABAE1267C2E2331FD4101A8EE2743A9E9B67A0EC7F20DE6A3CD9E22BD8BFAD3CA3CF2BA27F2236DE25C2413611D2A930DA6A", false, false, false, false, null, null, 150.048, 2, 6, false, false, null, false, false, false, false, false, false, null, null, null, false, false, false, 1, false, null, false, false, 0, null, false, false, false, "Llamigos", false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e03%20-%20Llamigos.mp4", false, false, 10003L },
                    { 8, false, new DateTime(2021, 9, 6, 5, 20, 38, 559, DateTimeKind.Unspecified).AddTicks(3102), false, false, false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.jpg", false, null, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.bif", false, false, 119.25, false, new DateTime(2013, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro hunts for food on the other side of a fence and is once again inspired by the Armadillo but this time to a shocking effect.", false, false, 3, 2, null, false, false, false, false, null, "94A2E1F9E973552DB73220BED6842AD4EED820B3F530A387177EAC70189A4F50A2B6D4A0E705DC728F613E43BF2C87E19E6B44ABE003A12B1F1384A68DFB8614", false, false, false, false, null, null, 146.00800000000001, 2, 6, false, false, null, false, false, false, false, false, false, null, null, null, false, false, false, 1, false, null, false, false, 0, null, false, false, false, "Gran Dillama", false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e02%20-%20Gran%20Dillama.mp4", false, false, 10002L },
                    { 9, false, new DateTime(2021, 9, 6, 5, 20, 37, 559, DateTimeKind.Unspecified).AddTicks(1031), false, false, false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.jpg", false, null, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.bif", false, false, 87.917000000000002, false, new DateTime(2013, 9, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), "Koro has trouble crossing an apparent desolate road, a problem that an unwitting Armadillo does not share.", false, false, 3, 1, null, false, false, false, false, null, "32B523CD69F065EB37CDE5C1BD8A18E6C1367A27608F450A3479A6AF5F98D0391A3D9ACB869614815F6D0A01954F89412390DC60A85161FC65398C3D91960DD8", false, false, false, false, null, null, 90.001000000000005, 2, 6, false, false, null, false, false, false, false, false, false, null, null, null, false, false, false, 1, false, null, false, false, 0, null, false, false, false, "Llama Drama", false, "https://s3.dustypig.tv/demo-media/TV%20Shows/Caminandes/Season%2001/Caminandes%20-%20s01e01%20-%20Llama%20Drama.mp4", false, false, 10001L }
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
                    { 2, 1, 180.0, new DateTime(2021, 9, 24, 11, 34, 0, 0, DateTimeKind.Unspecified), null },
                    { 8, 1, 30.0, new DateTime(2021, 9, 24, 13, 12, 14, 344, DateTimeKind.Unspecified).AddTicks(8000), null }
                });
        }
    }
}
