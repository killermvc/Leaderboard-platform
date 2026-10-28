using System.Reflection;
using Leaderboard.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Leaderboard.Tests;

public class LeaderboardMigrationTests
{
    [Fact]
    public void Up_creates_default_boards_and_backfills_existing_scores()
    {
        var operations = GetOperations("Up");
        var sql = string.Join("\n", operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("INSERT INTO `Leaderboards`", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UPDATE `Scores`", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LeaderboardId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Default'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            operations,
            operation => operation is AlterColumnOperation alterColumn &&
                alterColumn.Table == "Scores" &&
                alterColumn.Name == "LeaderboardId" &&
                !alterColumn.IsNullable);
    }

    [Fact]
    public void Up_uses_leaderboard_scoped_submission_idempotency()
    {
        var operations = GetOperations("Up");

        var oldIndex = operations
            .OfType<DropIndexOperation>()
            .Single(operation => operation.Name == "IX_Scores_GameId_SubmissionId");
        var newIndex = operations
            .OfType<CreateIndexOperation>()
            .Single(operation => operation.Name == "IX_Scores_LeaderboardId_SubmissionId");

        Assert.Equal(["LeaderboardId", "SubmissionId"], newIndex.Columns);
        Assert.True(newIndex.IsUnique);
    }

    [Fact]
    public void Down_does_not_delete_existing_games_or_scores()
    {
        var operations = GetOperations("Down");

        Assert.DoesNotContain(
            operations,
            operation => operation is DeleteDataOperation deleteData &&
                (deleteData.Table == "Games" || deleteData.Table == "Scores"));
    }

    private static IReadOnlyList<MigrationOperation> GetOperations(string methodName)
    {
        var migration = new AddLeaderboards();
        var migrationBuilder = new MigrationBuilder("MySql");
        var method = typeof(AddLeaderboards).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        method!.Invoke(migration, [migrationBuilder]);
        return migrationBuilder.Operations;
    }
}
