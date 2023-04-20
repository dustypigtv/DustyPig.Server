using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class Rename_Hash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "FCMTokens",
                newName: "Hash");

            migrationBuilder.RenameIndex(
                name: "IX_FCMTokens_ProfileId_TokenHash",
                table: "FCMTokens",
                newName: "IX_FCMTokens_ProfileId_Hash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Hash",
                table: "FCMTokens",
                newName: "TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_FCMTokens_ProfileId_Hash",
                table: "FCMTokens",
                newName: "IX_FCMTokens_ProfileId_TokenHash");
        }
    }
}
