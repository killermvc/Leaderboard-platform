using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionIdToScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET @score_game_index_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Scores'
      AND INDEX_NAME = 'IX_Scores_GameId'
);
SET @drop_score_game_index = IF(
    @score_game_index_exists > 0,
    'DROP INDEX `IX_Scores_GameId` ON `Scores`',
    'SELECT 1'
);
PREPARE drop_score_game_index_statement FROM @drop_score_game_index;
EXECUTE drop_score_game_index_statement;
DEALLOCATE PREPARE drop_score_game_index_statement;");

            migrationBuilder.AddColumn<string>(
                name: "SubmissionId",
                table: "Scores",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_GameId_SubmissionId",
                table: "Scores",
                columns: new[] { "GameId", "SubmissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET @score_submission_index_exists = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Scores'
      AND INDEX_NAME = 'IX_Scores_GameId_SubmissionId'
);
SET @drop_score_submission_index = IF(
    @score_submission_index_exists > 0,
    'DROP INDEX `IX_Scores_GameId_SubmissionId` ON `Scores`',
    'SELECT 1'
);
PREPARE drop_score_submission_index_statement FROM @drop_score_submission_index;
EXECUTE drop_score_submission_index_statement;
DEALLOCATE PREPARE drop_score_submission_index_statement;");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Scores");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_GameId",
                table: "Scores",
                column: "GameId");
        }
    }
}
