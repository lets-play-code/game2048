using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Game2048.Game.Migrations
{
    /// <inheritdoc />
    public partial class InitialGame2048Persistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerName = table.Column<string>(type: "TEXT", nullable: false),
                    BestScore = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SlotKey = table.Column<string>(type: "TEXT", nullable: false),
                    BoardJson = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Win = table.Column<bool>(type: "INTEGER", nullable: false),
                    Lose = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScoreRecorded = table.Column<bool>(type: "INTEGER", nullable: false),
                    LeakedShouldAddTile = table.Column<bool>(type: "INTEGER", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedGames", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_PlayerName",
                table: "LeaderboardEntries",
                column: "PlayerName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedGames_SlotKey",
                table: "SavedGames",
                column: "SlotKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "SavedGames");
        }
    }
}
