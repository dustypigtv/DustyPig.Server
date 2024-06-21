using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Subtitles");

            migrationBuilder.DropColumn(
                name: "ArtworkSize",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "ArtworkSize",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "BackdropSize",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "BifSize",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "VideoSize",
                table: "MediaEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "FileSize",
                table: "Subtitles",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "ArtworkSize",
                table: "Playlists",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "ArtworkSize",
                table: "MediaEntries",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "BackdropSize",
                table: "MediaEntries",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "BifSize",
                table: "MediaEntries",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "VideoSize",
                table: "MediaEntries",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);
        }
    }
}
