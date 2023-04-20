using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class Fix_Index_For_FCM : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FCMTokens_ProfileId_Hash",
                table: "FCMTokens");

            migrationBuilder.CreateIndex(
                name: "IX_FCMTokens_Hash",
                table: "FCMTokens",
                column: "Hash",
                unique: true);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FCMTokens_Hash",
                table: "FCMTokens");

            migrationBuilder.CreateIndex(
                name: "IX_FCMTokens_ProfileId_Hash",
                table: "FCMTokens",
                columns: new[] { "ProfileId", "Hash" },
                unique: true);
        }
    }
}
