using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Genres_Sort_Nulls_Last : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Genre_Adventure",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Action",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Animation",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Anime",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Awards_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Children",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Comedy",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Crime",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Documentary",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Drama",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Family",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Fantasy",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Food",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Game_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_History",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Home_and_Garden",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Horror",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Indie",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Martial_Arts",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mini_Series",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Musical",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mystery",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_News",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Podcast",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Political",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Reality",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Romance",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Science_Fiction",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Soap",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Sports",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Suspense",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Talk_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Thriller",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Travel",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_TV_Movie",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_War",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Western",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_SearchTitle",
                table: "MediaEntries");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Action",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Action\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Adventure",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Adventure\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Animation",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Animation\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Anime",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Anime\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Awards_Show",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Awards_Show\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Children",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Children\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Comedy",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Comedy\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Crime",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Crime\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Documentary",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Documentary\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Drama",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Drama\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Family",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Family\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Fantasy",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Fantasy\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Food",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Food\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Game_Show",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Game_Show\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_History",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_History\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Home_and_Garden",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Home_and_Garden\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Horror",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Horror\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Indie",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Indie\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Martial_Arts",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Martial_Arts\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mini_Series",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Mini_Series\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Music",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Music\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Musical",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Musical\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mystery",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Mystery\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_News",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_News\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Podcast",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Podcast\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Political",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Political\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Reality",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Reality\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Romance",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Romance\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Science_Fiction",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Science_Fiction\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Soap",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Soap\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Sports",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Sports\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Suspense",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Suspense\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Talk_Show",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Talk_Show\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Thriller",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Thriller\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Travel",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Travel\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_TV_Movie",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_TV_Movie\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_War",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_War\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Western",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Western\" = TRUE")
                .Annotation("Npgsql:IndexNullSortOrder", new[] { NullSortOrder.NullsLast });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Action",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Adventure",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Animation",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Anime",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Awards_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Children",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Comedy",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Crime",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Documentary",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Drama",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Family",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Fantasy",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Food",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Game_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_History",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Home_and_Garden",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Horror",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Indie",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Martial_Arts",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mini_Series",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Music",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Musical",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mystery",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_News",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Podcast",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Political",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Reality",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Romance",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Science_Fiction",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Soap",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Sports",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Suspense",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Talk_Show",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Thriller",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Travel",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_TV_Movie",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_War",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity_Genre_Western",
                table: "MediaEntries");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Genre_Adventure",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Adventure\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Action",
                table: "MediaEntries",
                column: "Popularity",
                descending: new bool[0],
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Action\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Animation",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Animation\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Anime",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Anime\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Awards_Show",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Awards_Show\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Children",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Children\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Comedy",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Comedy\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Crime",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Crime\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Documentary",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Documentary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Drama",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Drama\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Family",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Family\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Fantasy",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Fantasy\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Food",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Food\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Game_Show",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Game_Show\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_History",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_History\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Home_and_Garden",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Home_and_Garden\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Horror",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Horror\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Indie",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Indie\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Martial_Arts",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Martial_Arts\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mini_Series",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Mini_Series\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Musical",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Musical\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Mystery",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Mystery\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_News",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_News\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Podcast",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Podcast\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Political",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Political\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Reality",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Reality\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Romance",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Romance\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Science_Fiction",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Science_Fiction\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Soap",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Soap\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Sports",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Sports\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Suspense",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Suspense\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Talk_Show",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Talk_Show\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Thriller",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Thriller\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Travel",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Travel\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_TV_Movie",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_TV_Movie\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_War",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_War\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity_Genre_Western",
                table: "MediaEntries",
                column: "Popularity",
                filter: "\"Popularity\" IS NOT NULL AND \"Genre_Western\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_SearchTitle",
                table: "MediaEntries",
                column: "SearchTitle")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }
    }
}
