using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Migrations
{
    /// <inheritdoc />
    public partial class Simplify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GetRequests_Profiles_ProfileId",
                table: "GetRequests");

            migrationBuilder.DropIndex(
                name: "IX_WatchListItems_ProfileId",
                table: "WatchListItems");

            migrationBuilder.DropIndex(
                name: "IX_TMDB_EntryPeopleBridges_TMDB_EntryId",
                table: "TMDB_EntryPeopleBridges");

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
                name: "IX_GetRequests_ProfileId",
                table: "GetRequests");

            migrationBuilder.DropIndex(
                name: "IX_AutoPlaylistSeries_PlaylistId",
                table: "AutoPlaylistSeries");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "GetRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfileId",
                table: "GetRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchListItems_ProfileId",
                table: "WatchListItems",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_EntryPeopleBridges_TMDB_EntryId",
                table: "TMDB_EntryPeopleBridges",
                column: "TMDB_EntryId");

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
                name: "IX_GetRequests_ProfileId",
                table: "GetRequests",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPlaylistSeries_PlaylistId",
                table: "AutoPlaylistSeries",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_GetRequests_Profiles_ProfileId",
                table: "GetRequests",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id");
        }
    }
}
