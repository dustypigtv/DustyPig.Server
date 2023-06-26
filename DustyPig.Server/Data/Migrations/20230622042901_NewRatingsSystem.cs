using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class NewRatingsSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxMovieRating",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxTVRatings",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MovieRating",
                table: "MediaEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TVRating",
                table: "MediaEntries",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxMovieRating",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "MaxTVRatings",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "MovieRating",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "TVRating",
                table: "MediaEntries");
        }
    }
}
