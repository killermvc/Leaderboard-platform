using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Xunit;
using LeaderboardEntity = Leaderboard.Models.Leaderboard;

namespace Leaderboard.Tests;

public class ScoreRepositoryBoardTests
{
	[Fact]
	public async Task SubmitScore_RejectsBoardFromAnotherGame()
	{
		await using var context = CreateContext();
		context.Games.AddRange(
			new Game { Id = 1, Name = "One", Description = "One" },
			new Game { Id = 2, Name = "Two", Description = "Two" });
		context.Users.Add(new User { Id = 4, ClerkUserId = "user-4" });
		context.Leaderboards.Add(new LeaderboardEntity { Id = 10, GameId = 2, Name = "Other" });
		await context.SaveChangesAsync();
		var repository = CreateRepository(context);

		await Assert.ThrowsAsync<KeyNotFoundException>(() =>
			repository.SubmitScoreAsync(4, 1, 10, 100));
	}

	[Fact]
	public async Task SubmitScore_RejectsInactiveBoard()
	{
		await using var context = CreateContext();
		context.Games.Add(new Game { Id = 1, Name = "One", Description = "One" });
		context.Users.Add(new User { Id = 4, ClerkUserId = "user-4" });
		context.Leaderboards.Add(new LeaderboardEntity { Id = 10, GameId = 1, Name = "Closed", IsActive = false });
		await context.SaveChangesAsync();
		var repository = CreateRepository(context);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			repository.SubmitScoreAsync(4, 1, 10, 100));
	}

	[Fact]
	public async Task SubmitScore_ComparesHighScoresWithinSelectedBoard()
	{
		await using var context = CreateContext();
		var game = new Game { Id = 1, Name = "One", Description = "One" };
		var first = new LeaderboardEntity { Id = 10, GameId = 1, Name = "First" };
		var second = new LeaderboardEntity { Id = 11, GameId = 1, Name = "Second" };
		var user = new User { Id = 4, ClerkUserId = "user-4" };
		context.AddRange(game, first, second, user);
		context.Scores.Add(new Score
		{
			Game = game, Leaderboard = first, LeaderboardId = first.Id, UserId = user.Id,
			Value = 100, Status = ScoreStatus.Approved, Title = "old"
		});
		await context.SaveChangesAsync();
		var repository = CreateRepository(context);

		await repository.SubmitScoreAsync(user.Id, game.Id, second.Id, 50);

		var saved = await context.Scores.SingleAsync(score => score.LeaderboardId == second.Id);
		Assert.Equal(50, saved.Value);
		Assert.Equal(ScoreStatus.Pending, saved.Status);
	}

	[Fact]
	public async Task Score_history_queries_filter_to_selected_board()
	{
		await using var context = CreateContext();
		var game = new Game { Id = 1, Name = "One", Description = "One" };
		var first = new LeaderboardEntity { Id = 10, GameId = 1, Name = "First" };
		var second = new LeaderboardEntity { Id = 11, GameId = 1, Name = "Second" };
		var user = new User { Id = 4, ClerkUserId = "user-4" };
		context.AddRange(game, first, second, user);
		context.Scores.AddRange(
			new Score
			{
				Game = game, Leaderboard = first, LeaderboardId = first.Id, User = user,
				Value = 100, Status = ScoreStatus.Approved, Title = "first"
			},
			new Score
			{
				Game = game, Leaderboard = second, LeaderboardId = second.Id, User = user,
				Value = 200, Status = ScoreStatus.Approved, Title = "second"
			});
		await context.SaveChangesAsync();
		var repository = CreateRepository(context);

		var scores = await repository.GetScoresByUserAsync(user.Id, 10, 0, first.Id);
		var recent = await repository.GetRecentScoresAsync(10, 0, second.Id);
		var submissions = await repository.GetAllSubmissionsAsync(10, 0, first.Id);
		var ownScores = await repository.GetAllScoresByUserAsync(user.Id, 10, 0, second.Id);

		Assert.Single(scores);
		Assert.Equal(first.Id, scores[0].LeaderboardId);
		Assert.Single(recent);
		Assert.Equal(second.Id, recent[0].LeaderboardId);
		Assert.Single(submissions);
		Assert.Equal(first.Id, submissions[0].LeaderboardId);
		Assert.Single(ownScores);
		Assert.Equal(second.Id, ownScores[0].LeaderboardId);
	}

	private static AppDbContext CreateContext() =>
		new(new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options);

	private static ScoreRepository CreateRepository(AppDbContext context) =>
		new(context, ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false"));
}
