using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    public partial class ProgressXid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Xid",
                table: "ProfileMediaProgresses",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortTitle",
                value: "agent 327: operation barbershop");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortTitle",
                value: "big buck bunny");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortTitle",
                value: "coffee run");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 4,
                column: "SortTitle",
                value: "hero");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 5,
                column: "SortTitle",
                value: "spring");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 6,
                column: "SortTitle",
                value: "caminandes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Xid",
                table: "ProfileMediaProgresses");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 1,
                column: "SortTitle",
                value: "Agent 327: Operation Barbershop");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 2,
                column: "SortTitle",
                value: "Big Buck Bunny");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 3,
                column: "SortTitle",
                value: "Coffee Run");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 4,
                column: "SortTitle",
                value: "Hero");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 5,
                column: "SortTitle",
                value: "Spring");

            migrationBuilder.UpdateData(
                table: "MediaEntries",
                keyColumn: "Id",
                keyValue: 6,
                column: "SortTitle",
                value: "Caminandes");
        }
    }
}
