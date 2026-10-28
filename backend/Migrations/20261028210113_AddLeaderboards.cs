using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scores_GameId_SubmissionId",
                table: "Scores");

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leaderboards_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "LeaderboardId",
                table: "Scores",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO `Leaderboards` (`GameId`, `Name`, `Description`, `IsActive`, `CreatedAt`, `UpdatedAt`)
                SELECT `Id`, 'Default', NULL, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                FROM `Games`;
                """);

            migrationBuilder.Sql("""
                UPDATE `Scores` AS s
                INNER JOIN `Leaderboards` AS l ON l.`GameId` = s.`GameId` AND l.`Name` = 'Default'
                SET s.`LeaderboardId` = l.`Id`;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "LeaderboardId",
                table: "Scores",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_GameId",
                table: "Scores",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_LeaderboardId",
                table: "Scores",
                column: "LeaderboardId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_LeaderboardId_Status",
                table: "Scores",
                columns: new[] { "LeaderboardId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Scores_LeaderboardId_SubmissionId",
                table: "Scores",
                columns: new[] { "LeaderboardId", "SubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_LeaderboardId_UserId_Status",
                table: "Scores",
                columns: new[] { "LeaderboardId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_GameId",
                table: "Leaderboards",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_GameId_Name",
                table: "Leaderboards",
                columns: new[] { "GameId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Leaderboards_LeaderboardId",
                table: "Scores",
                column: "LeaderboardId",
                principalTable: "Leaderboards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Leaderboards_LeaderboardId",
                table: "Scores");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropIndex(
                name: "IX_Scores_GameId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_LeaderboardId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_LeaderboardId_Status",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_LeaderboardId_SubmissionId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_LeaderboardId_UserId_Status",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "LeaderboardId",
                table: "Scores");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_GameId_SubmissionId",
                table: "Scores",
                columns: new[] { "GameId", "SubmissionId" },
                unique: true);
        }
    }
}
