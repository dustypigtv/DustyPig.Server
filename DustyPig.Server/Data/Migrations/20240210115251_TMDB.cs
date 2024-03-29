﻿using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class TMDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaPersonBridges");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.AddColumn<int>(
                name: "TMDB_EntryId",
                table: "MediaEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TMDB_Entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TMDB_Id = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(10000)", maxLength: 10000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovieRating = table.Column<int>(type: "int", nullable: true),
                    TVRating = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BackdropUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BackdropSize = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Popularity = table.Column<double>(type: "double", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMDB_Entries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMDB_People",
                columns: table => new
                {
                    TMDB_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMDB_People", x => x.TMDB_Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMDB_EntryPeopleBridges",
                columns: table => new
                {
                    TMDB_EntryId = table.Column<int>(type: "int", nullable: false),
                    TMDB_PersonId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMDB_EntryPeopleBridges", x => new { x.TMDB_EntryId, x.TMDB_PersonId, x.Role });
                    table.ForeignKey(
                        name: "FK_TMDB_EntryPeopleBridges_TMDB_Entries_TMDB_EntryId",
                        column: x => x.TMDB_EntryId,
                        principalTable: "TMDB_Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TMDB_EntryPeopleBridges_TMDB_People_TMDB_PersonId",
                        column: x => x.TMDB_PersonId,
                        principalTable: "TMDB_People",
                        principalColumn: "TMDB_Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Added",
                table: "MediaEntries",
                column: "Added");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_EntryType",
                table: "MediaEntries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries",
                column: "Popularity");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_PopularityUpdated",
                table: "MediaEntries",
                column: "PopularityUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_TMDB_EntryId",
                table: "MediaEntries",
                column: "TMDB_EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_Entries_MediaType",
                table: "TMDB_Entries",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_Entries_TMDB_Id",
                table: "TMDB_Entries",
                column: "TMDB_Id");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_Entries_TMDB_Id_MediaType",
                table: "TMDB_Entries",
                columns: new[] { "TMDB_Id", "MediaType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_EntryPeopleBridges_TMDB_EntryId",
                table: "TMDB_EntryPeopleBridges",
                column: "TMDB_EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TMDB_EntryPeopleBridges_TMDB_PersonId",
                table: "TMDB_EntryPeopleBridges",
                column: "TMDB_PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaEntries_TMDB_Entries_TMDB_EntryId",
                table: "MediaEntries",
                column: "TMDB_EntryId",
                principalTable: "TMDB_Entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaEntries_TMDB_Entries_TMDB_EntryId",
                table: "MediaEntries");

            migrationBuilder.DropTable(
                name: "TMDB_EntryPeopleBridges");

            migrationBuilder.DropTable(
                name: "TMDB_Entries");

            migrationBuilder.DropTable(
                name: "TMDB_People");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Added",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_EntryType",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_Popularity",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_PopularityUpdated",
                table: "MediaEntries");

            migrationBuilder.DropIndex(
                name: "IX_MediaEntries_TMDB_EntryId",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "TMDB_EntryId",
                table: "MediaEntries");

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaPersonBridges",
                columns: table => new
                {
                    MediaEntryId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaPersonBridges", x => new { x.MediaEntryId, x.PersonId, x.Role });
                    table.ForeignKey(
                        name: "FK_MediaPersonBridges_MediaEntries_MediaEntryId",
                        column: x => x.MediaEntryId,
                        principalTable: "MediaEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaPersonBridges_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPersonBridges_MediaEntryId",
                table: "MediaPersonBridges",
                column: "MediaEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaPersonBridges_PersonId",
                table: "MediaPersonBridges",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_People_Hash",
                table: "People",
                column: "Hash",
                unique: true);
        }
    }
}
