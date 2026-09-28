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
            migrationBuilder.DropIndex(
                name: "IX_Scores_GameId",
                table: "Scores");

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
            migrationBuilder.DropIndex(
                name: "IX_Scores_GameId_SubmissionId",
                table: "Scores");

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
