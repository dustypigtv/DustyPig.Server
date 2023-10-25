using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Playlists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Playlists_ProfileId_Name_CurrentIndex",
                table: "Playlists");

            migrationBuilder.RenameColumn(
                name: "CurrentIndex",
                table: "Playlists",
                newName: "CurrentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_ProfileId_Name",
                table: "Playlists",
                columns: new[] { "ProfileId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Playlists_ProfileId_Name",
                table: "Playlists");

            migrationBuilder.RenameColumn(
                name: "CurrentItemId",
                table: "Playlists",
                newName: "CurrentIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_ProfileId_Name_CurrentIndex",
                table: "Playlists",
                columns: new[] { "ProfileId", "Name", "CurrentIndex" },
                unique: true);
        }
    }
}
