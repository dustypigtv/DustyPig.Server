using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AvatarInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<double>(
                name: "AvatarScale",
                table: "Profiles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "OriginalAvatarUrl",
                table: "Profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarCenterX",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "AvatarCenterY",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "AvatarScale",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "OriginalAvatarUrl",
                table: "Profiles");
        }
    }
}
