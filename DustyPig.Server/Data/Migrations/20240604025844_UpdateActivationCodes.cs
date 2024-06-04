using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActivationCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivationCodes_Accounts_AccountId",
                table: "ActivationCodes");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "ActivationCodes",
                newName: "ProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivationCodes_AccountId",
                table: "ActivationCodes",
                newName: "IX_ActivationCodes_ProfileId");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "ActivationCodes",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationCodes_Profiles_ProfileId",
                table: "ActivationCodes",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivationCodes_Profiles_ProfileId",
                table: "ActivationCodes");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "ActivationCodes");

            migrationBuilder.RenameColumn(
                name: "ProfileId",
                table: "ActivationCodes",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_ActivationCodes_ProfileId",
                table: "ActivationCodes",
                newName: "IX_ActivationCodes_AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivationCodes_Accounts_AccountId",
                table: "ActivationCodes",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
