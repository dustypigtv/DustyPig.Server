using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DustyPig.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAutoTooledTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailableGenresResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailableGenresResults",
                columns: table => new
                {
                    Genre_Action = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Adventure = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Animation = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Anime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Awards_Show = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Children = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Comedy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Crime = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Documentary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Drama = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Family = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Fantasy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Food = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Game_Show = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_History = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Home_and_Garden = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Horror = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Indie = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Martial_Arts = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Mini_Series = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Music = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Musical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Mystery = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_News = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Podcast = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Political = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Reality = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Romance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Science_Fiction = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Soap = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Sports = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Suspense = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_TV_Movie = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Talk_Show = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Thriller = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Travel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_War = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Genre_Western = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
