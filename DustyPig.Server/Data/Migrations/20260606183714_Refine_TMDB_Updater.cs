using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Refine_TMDB_Updater : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureCount",
                table: "TMDB_Entries");

            migrationBuilder.DropColumn(
                name: "TMDB_Updated",
                table: "MediaEntries");

            migrationBuilder.AddColumn<bool>(
                name: "PermanentlyFailed",
                table: "TMDB_Entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermanentlyFailed",
                table: "TMDB_Entries");

            migrationBuilder.AddColumn<int>(
                name: "FailureCount",
                table: "TMDB_Entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TMDB_Updated",
                table: "MediaEntries",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
