using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Remove_PopularityUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_PopularityUpdated",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "PopularityUpdated",
                table: "MediaEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PopularityUpdated",
                table: "MediaEntries",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_PopularityUpdated",
                table: "MediaEntries",
                column: "PopularityUpdated");
        }
    }
}
