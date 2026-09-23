using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaderboard.Migrations
{
    /// <inheritdoc />
    public partial class AddPrefixAndUsedAtForKeyAndGameSubmitsAllowed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "ApiKeys");

            migrationBuilder.AddColumn<bool>(
                name: "SubmitsAllowed",
                table: "Games",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KeyPrefix",
                table: "ApiKeys",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "ApiKeys",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "ApiKeys",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmitsAllowed",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "KeyPrefix",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "ApiKeys");

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "ApiKeys",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
