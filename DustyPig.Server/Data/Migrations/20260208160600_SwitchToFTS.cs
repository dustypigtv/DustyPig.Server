using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToFTS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_SearchTitle",
                table: "MediaEntries",
                column: "SearchTitle")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_SearchTitle",
                table: "MediaEntries");
        }
    }
}
