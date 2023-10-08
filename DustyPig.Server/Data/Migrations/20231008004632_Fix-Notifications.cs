using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Friendships_FriendshipId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_GetRequests_GetRequestId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_MediaEntries_MediaEntryId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TitleOverrides_TitleOverrideId",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Friendships_FriendshipId",
                table: "Notifications",
                column: "FriendshipId",
                principalTable: "Friendships",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_GetRequests_GetRequestId",
                table: "Notifications",
                column: "GetRequestId",
                principalTable: "GetRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_MediaEntries_MediaEntryId",
                table: "Notifications",
                column: "MediaEntryId",
                principalTable: "MediaEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TitleOverrides_TitleOverrideId",
                table: "Notifications",
                column: "TitleOverrideId",
                principalTable: "TitleOverrides",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Friendships_FriendshipId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_GetRequests_GetRequestId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_MediaEntries_MediaEntryId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_TitleOverrides_TitleOverrideId",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Friendships_FriendshipId",
                table: "Notifications",
                column: "FriendshipId",
                principalTable: "Friendships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_GetRequests_GetRequestId",
                table: "Notifications",
                column: "GetRequestId",
                principalTable: "GetRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_MediaEntries_MediaEntryId",
                table: "Notifications",
                column: "MediaEntryId",
                principalTable: "MediaEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_TitleOverrides_TitleOverrideId",
                table: "Notifications",
                column: "TitleOverrideId",
                principalTable: "TitleOverrides",
                principalColumn: "Id");
        }
    }
}
