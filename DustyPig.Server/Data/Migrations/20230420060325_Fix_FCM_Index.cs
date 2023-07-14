using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class Fix_FCM_Index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "FCMTokens",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FCMTokens_ProfileId_TokenHash",
                table: "FCMTokens",
                columns: new[] { "ProfileId", "TokenHash" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FCMTokens_ProfileId_TokenHash",
                table: "FCMTokens");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "FCMTokens");

        }
    }
}
