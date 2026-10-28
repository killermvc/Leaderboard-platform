using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Leaderboard.Tests;

public class LeaderboardModelTests
{
    private static IModel Model
    {
        get
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);
            return context.Model;
        }
    }

    [Fact]
    public void Configures_unique_board_names_per_game()
    {
        var entity = Model.FindEntityType(typeof(Leaderboard.Models.Leaderboard));

        Assert.NotNull(entity);
        Assert.Contains(
            entity!.GetIndexes(),
            index => index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(["GameId", "Name"]));
    }

    [Fact]
    public void Configures_game_cascade_and_score_restrict_relationships()
    {
        var model = Model;
        var gameEntity = model.FindEntityType(typeof(Game));
        var leaderboardEntity = model.FindEntityType(typeof(Leaderboard.Models.Leaderboard));
        var scoreEntity = model.FindEntityType(typeof(Score));

        Assert.NotNull(gameEntity);
        Assert.NotNull(leaderboardEntity);
        Assert.NotNull(scoreEntity);
        Assert.Equal(
            DeleteBehavior.Cascade,
            leaderboardEntity!.GetForeignKeys()
                .Single(foreignKey => foreignKey.PrincipalEntityType == gameEntity)
                .DeleteBehavior);
        Assert.Equal(
            DeleteBehavior.Restrict,
            scoreEntity!.GetForeignKeys()
                .Single(foreignKey => foreignKey.PrincipalEntityType == leaderboardEntity)
                .DeleteBehavior);
    }

    [Fact]
    public void Configures_board_scoped_score_indexes()
    {
        var entity = Model.FindEntityType(typeof(Score));

        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(["LeaderboardId", "Status"]));
        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(["LeaderboardId", "UserId", "Status"]));
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(["LeaderboardId", "SubmissionId"]));
    }
}
