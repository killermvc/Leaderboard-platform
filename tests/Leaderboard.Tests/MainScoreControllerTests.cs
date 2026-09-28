using System.Security.Claims;
using Leaderboard.Controllers;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class ScoreControllerTests
{
	[Fact]
	public async Task GetLeaderboard_ReturnsRepositoryResults()
	{
		var scores = new Mock<IScoreRepository>();
		var entries = new List<LeaderboardEntry>
		{
			new() { UserId = 4, UserName = "Ryu", Score = 9000 }
		};
		scores.Setup(repository => repository.GetLeaderboardAsync(7, 10)).ReturnsAsync(entries);
		var controller = CreateController(scores.Object);

		var result = await controller.GetLeaderboard(7);

		var response = Assert.IsType<OkObjectResult>(result);
		Assert.Same(entries, response.Value);
	}

	[Fact]
	public async Task SubmitScore_WithValidUserAndGame_SubmitsScore()
	{
		var scores = new Mock<IScoreRepository>();
		var games = new Mock<IGameRepository>();
		var game = new Game { Id = 7, Name = "Arcade", Description = "Test game" };
		games.Setup(repository => repository.GetGameByIdAsync(7)).ReturnsAsync(game);
		var controller = CreateController(scores.Object, games.Object, "4");

		var result = await controller.SubmitScore(new ScoreRequest
		{
			GameId = 7,
			Score = 9000,
			Title = "New record",
			Description = "First attempt"
		});

		var response = Assert.IsType<OkObjectResult>(result);
		Assert.Equal("Score submitted successfully.", response.Value);
		scores.Verify(repository => repository.SubmitScoreAsync(4, 7, 9000, "New record", "First attempt"), Times.Once);
	}

	[Fact]
	public async Task SubmitScore_WithInvalidUserClaim_ReturnsUnauthorized()
	{
		var scores = new Mock<IScoreRepository>();
		var controller = CreateController(scores.Object, userId: "not-a-user");

		var result = await controller.SubmitScore(new ScoreRequest { GameId = 7, Score = 100 });

		Assert.IsType<UnauthorizedObjectResult>(result);
		scores.Verify(repository => repository.SubmitScoreAsync(
			It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
	}

	[Fact]
	public async Task GetRank_WhenUserHasNoRank_ReturnsNotFound()
	{
		var scores = new Mock<IScoreRepository>();
		scores.Setup(repository => repository.GetRankAsync(7, 4)).ReturnsAsync((long?)null);
		var controller = CreateController(scores.Object);

		var result = await controller.GetRank(7, 4);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	private static ScoreController CreateController(
		IScoreRepository scores,
		IGameRepository? games = null,
		string userId = "4")
	{
		var controller = new ScoreController(
			scores,
			games ?? Mock.Of<IGameRepository>(),
			Mock.Of<IUserRepository>(),
			NullLogger<ScoreController>.Instance);
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, userId)], "test"))
			}
		};
		return controller;
	}
}