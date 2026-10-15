using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnused : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarCenterX",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "AvatarCenterY",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "AvatarNeedsCrop",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "AvatarScale",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvatarCenterX",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AvatarCenterY",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AvatarNeedsCrop",
                table: "Profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "AvatarScale",
                table: "Profiles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
