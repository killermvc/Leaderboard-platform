using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddClerkUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClerkUserId",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClerkUserId",
                table: "Users",
                column: "ClerkUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ClerkUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClerkUserId",
                table: "Users");
        }
    }
}
