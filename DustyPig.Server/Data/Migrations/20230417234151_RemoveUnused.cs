using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class RemoveUnused : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationMethods",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "WeeklySummary",
                table: "Profiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationMethods",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WeeklySummary",
                table: "Profiles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
