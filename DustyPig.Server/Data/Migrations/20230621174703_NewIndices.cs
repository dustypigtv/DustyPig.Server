using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class NewIndices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WatchListItems_ProfileId",
                table: "WatchListItems",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleOverrides_ProfileId",
                table: "TitleOverrides",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitles_MediaEntryId",
                table: "Subtitles",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ProfileId",
                table: "Subscriptions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_AccountId",
                table: "Profiles",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMediaProgresses_ProfileId",
                table: "ProfileMediaProgresses",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileLibraryShares_ProfileId",
                table: "ProfileLibraryShares",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_ProfileId",
                table: "Playlists",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaSearchBridges_MediaEntryId",
                table: "MediaSearchBridges",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPersonBridges_MediaEntryId",
                table: "MediaPersonBridges",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_LibraryId",
                table: "MediaEntries",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TMDB_Id",
                table: "MediaEntries",
                column: "TMDB_Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WatchListItems_ProfileId",
                table: "WatchListItems");

            migrationBuilder.DropIndex(
                name: "IX_TitleOverrides_ProfileId",
                table: "TitleOverrides");

            migrationBuilder.DropIndex(
                name: "IX_Subtitles_MediaEntryId",
                table: "Subtitles");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_ProfileId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_AccountId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_ProfileMediaProgresses_ProfileId",
                table: "ProfileMediaProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ProfileLibraryShares_ProfileId",
                table: "ProfileLibraryShares");

            migrationBuilder.DropIndex(
                name: "IX_Playlists_ProfileId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_MediaSearchBridges_MediaEntryId",
                table: "MediaSearchBridges");

            migrationBuilder.DropIndex(
                name: "IX_MediaPersonBridges_MediaEntryId",
                table: "MediaPersonBridges");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_LibraryId",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_TMDB_Id",
                table: "MediaEntries");
        }
    }
}
