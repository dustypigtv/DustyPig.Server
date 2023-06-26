using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class NewGenresSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Action",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Adventure",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Animation",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Anime",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Awards_Show",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Children",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Comedy",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Crime",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Documentary",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Drama",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Family",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Fantasy",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Food",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Game_Show",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "History",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Home_and_Garden",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Horror",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Indie",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Martial_Arts",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Mini_Series",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Music",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Musical",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Mystery",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "News",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Podcast",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Political",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Reality",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Romance",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Science_Fiction",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Soap",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sports",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Suspense",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TV_Movie",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Talk_Show",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Thriller",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Travel",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "War",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Western",
                table: "MediaEntries",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Adventure",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Animation",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Anime",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Awards_Show",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Children",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Comedy",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Crime",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Documentary",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Drama",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Family",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Fantasy",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Food",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Game_Show",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "History",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Home_and_Garden",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Horror",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Indie",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Martial_Arts",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Mini_Series",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Music",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Musical",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Mystery",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "News",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Podcast",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Political",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Reality",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Romance",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Science_Fiction",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Soap",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Sports",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Suspense",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "TV_Movie",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Talk_Show",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Thriller",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Travel",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "War",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Western",
                table: "MediaEntries");
        }
    }
}
