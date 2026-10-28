using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class LeaderboardRepositoryTests
{
    [Fact]
    public async Task Create_trims_name_and_rejects_whitespace()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);

        var created = await repository.CreateAsync(7, "  Arcade  ", "desc");

        Assert.Equal("Arcade", created.Name);
        await Assert.ThrowsAsync<ArgumentException>(() => repository.CreateAsync(7, "  ", null));
    }

    [Fact]
    public async Task Duplicate_name_is_rejected_within_game()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        await repository.CreateAsync(7, "Arcade", null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateAsync(7, " arcade ", null));
    }

    [Fact]
    public async Task Get_by_game_rejects_cross_game_board_and_updates_fields()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var board = await repository.CreateAsync(7, "Arcade", null);
        var other = await repository.CreateAsync(8, "Arcade", null);

        Assert.Single(await repository.GetByGameIdAsync(7));
        Assert.Null(await repository.GetByIdAsync(8, board.Id));

        var updated = await repository.UpdateAsync(7, board.Id, " Ranked ", "new", false);
        Assert.Equal(("Ranked", "new", false), (updated!.Name, updated.Description, updated.IsActive));
        Assert.Null(await repository.UpdateAsync(7, other.Id, "x", null, true));
    }

    [Fact]
    public async Task Delete_blocks_scored_board_and_deletes_empty_board()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var scored = await repository.CreateAsync(7, "Scored", null);
        var empty = await repository.CreateAsync(7, "Empty", null);
        context.Scores.Add(new Score
        {
            GameId = 7,
            Game = await context.Games.SingleAsync(game => game.Id == 7),
            LeaderboardId = scored.Id,
            Leaderboard = scored,
            Value = 10,
            Title = "score"
        });
        await context.SaveChangesAsync();

        Assert.Equal(LeaderboardDeleteResult.HasScores, await repository.DeleteAsync(7, scored.Id));
        Assert.Equal(LeaderboardDeleteResult.Deleted, await repository.DeleteAsync(7, empty.Id));
    }

    [Fact]
    public async Task Can_manage_allows_admin_and_assigned_moderator_only()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { Id = 1, Name = "Admin" });
        context.UserRoles.Add(new UserRole { UserId = 10, RoleId = 1 });
        await context.SaveChangesAsync();
        var moderators = new Mock<IGameModeratorRepository>();
        moderators.Setup(item => item.IsModeratorAsync(7, 11)).ReturnsAsync(true);
        var repository = new LeaderboardRepository(context, moderators.Object);

        Assert.True(await repository.CanManageAsync(7, 10));
        Assert.True(await repository.CanManageAsync(7, 11));
        Assert.False(await repository.CanManageAsync(7, 12));
    }

    private static LeaderboardRepository CreateRepository(AppDbContext context) =>
        new(context, Mock.Of<IGameModeratorRepository>());

    private static AppDbContext CreateContext()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        context.Games.AddRange(
            new Game { Id = 7, Name = "Game 7", Description = "Description" },
            new Game { Id = 8, Name = "Game 8", Description = "Description" });
        context.SaveChanges();
        return context;
    }
}
