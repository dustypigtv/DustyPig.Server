using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class GenrePartialInices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Action",
                table: "MediaEntries",
                column: "Genre_Action",
                filter: "\"Genre_Action\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Adventure",
                table: "MediaEntries",
                column: "Genre_Adventure",
                filter: "\"Genre_Adventure\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Animation",
                table: "MediaEntries",
                column: "Genre_Animation",
                filter: "\"Genre_Animation\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Anime",
                table: "MediaEntries",
                column: "Genre_Anime",
                filter: "\"Genre_Anime\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Awards_Show",
                table: "MediaEntries",
                column: "Genre_Awards_Show",
                filter: "\"Genre_Awards_Show\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Children",
                table: "MediaEntries",
                column: "Genre_Children",
                filter: "\"Genre_Children\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Comedy",
                table: "MediaEntries",
                column: "Genre_Comedy",
                filter: "\"Genre_Comedy\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Crime",
                table: "MediaEntries",
                column: "Genre_Crime",
                filter: "\"Genre_Crime\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Documentary",
                table: "MediaEntries",
                column: "Genre_Documentary",
                filter: "\"Genre_Documentary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Drama",
                table: "MediaEntries",
                column: "Genre_Drama",
                filter: "\"Genre_Drama\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Family",
                table: "MediaEntries",
                column: "Genre_Family",
                filter: "\"Genre_Family\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Fantasy",
                table: "MediaEntries",
                column: "Genre_Fantasy",
                filter: "\"Genre_Fantasy\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Food",
                table: "MediaEntries",
                column: "Genre_Food",
                filter: "\"Genre_Food\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Game_Show",
                table: "MediaEntries",
                column: "Genre_Game_Show",
                filter: "\"Genre_Game_Show\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_History",
                table: "MediaEntries",
                column: "Genre_History",
                filter: "\"Genre_History\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Home_and_Garden",
                table: "MediaEntries",
                column: "Genre_Home_and_Garden",
                filter: "\"Genre_Home_and_Garden\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Horror",
                table: "MediaEntries",
                column: "Genre_Horror",
                filter: "\"Genre_Horror\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Indie",
                table: "MediaEntries",
                column: "Genre_Indie",
                filter: "\"Genre_Indie\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Martial_Arts",
                table: "MediaEntries",
                column: "Genre_Martial_Arts",
                filter: "\"Genre_Martial_Arts\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Mini_Series",
                table: "MediaEntries",
                column: "Genre_Mini_Series",
                filter: "\"Genre_Mini_Series\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Music",
                table: "MediaEntries",
                column: "Genre_Music",
                filter: "\"Genre_Action\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Musical",
                table: "MediaEntries",
                column: "Genre_Musical",
                filter: "\"Genre_Musical\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Mystery",
                table: "MediaEntries",
                column: "Genre_Mystery",
                filter: "\"Genre_Mystery\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_News",
                table: "MediaEntries",
                column: "Genre_News",
                filter: "\"Genre_News\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Podcast",
                table: "MediaEntries",
                column: "Genre_Podcast",
                filter: "\"Genre_Podcast\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Political",
                table: "MediaEntries",
                column: "Genre_Political",
                filter: "\"Genre_Political\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Reality",
                table: "MediaEntries",
                column: "Genre_Reality",
                filter: "\"Genre_Reality\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Romance",
                table: "MediaEntries",
                column: "Genre_Romance",
                filter: "\"Genre_Romance\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Science_Fiction",
                table: "MediaEntries",
                column: "Genre_Science_Fiction",
                filter: "\"Genre_Science_Fiction\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Soap",
                table: "MediaEntries",
                column: "Genre_Soap",
                filter: "\"Genre_Soap\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Sports",
                table: "MediaEntries",
                column: "Genre_Sports",
                filter: "\"Genre_Sports\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Suspense",
                table: "MediaEntries",
                column: "Genre_Suspense",
                filter: "\"Genre_Suspense\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Talk_Show",
                table: "MediaEntries",
                column: "Genre_Talk_Show",
                filter: "\"Genre_Talk_Show\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Thriller",
                table: "MediaEntries",
                column: "Genre_Thriller",
                filter: "\"Genre_Thriller\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Travel",
                table: "MediaEntries",
                column: "Genre_Travel",
                filter: "\"Genre_Travel\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_TV_Movie",
                table: "MediaEntries",
                column: "Genre_TV_Movie",
                filter: "\"Genre_TV_Movie\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_War",
                table: "MediaEntries",
                column: "Genre_War",
                filter: "\"Genre_War\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Western",
                table: "MediaEntries",
                column: "Genre_Western",
                filter: "\"Genre_Western\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
