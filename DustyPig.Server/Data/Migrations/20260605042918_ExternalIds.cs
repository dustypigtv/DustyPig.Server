using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<long>(
                name: "AvatarVersion",
                table: "Profiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ArtworkVersion",
                table: "Playlists",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BackdropVersion",
                table: "Playlists",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ArtworkVersion",
                table: "MediaEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BackdropVersion",
                table: "MediaEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "IMDB_Id",
                table: "MediaEntries",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TVDB_Id",
                table: "MediaEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_IMDB_Id",
                table: "MediaEntries",
                column: "IMDB_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TVDB_Id",
                table: "MediaEntries",
                column: "TVDB_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_IMDB_Id",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_TVDB_Id",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "AvatarVersion",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ArtworkVersion",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "BackdropVersion",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "ArtworkVersion",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "BackdropVersion",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "IMDB_Id",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "TVDB_Id",
                table: "MediaEntries");

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
        }
    }
}
