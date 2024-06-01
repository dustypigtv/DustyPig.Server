using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeviceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                    name: "DeviceId",
                    table: "AccountTokens",
                    type: "varchar(128)",
                    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                    name: "DeviceId",
                    table: "AccountTokens");
        }
    }
}