using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AutoPlaylistSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoPlaylistSeries",
                columns: table => new
                {
                    PlaylistId = table.Column<int>(type: "int", nullable: false),
                    MediaEntryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoPlaylistSeries", x => new { x.PlaylistId, x.MediaEntryId });
                    table.ForeignKey(
                        name: "FK_AutoPlaylistSeries_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoPlaylistSeries_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPlaylistSeries_MediaEntryId",
                table: "AutoPlaylistSeries",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPlaylistSeries_PlaylistId",
                table: "AutoPlaylistSeries",
                column: "PlaylistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoPlaylistSeries");
        }
    }
}
