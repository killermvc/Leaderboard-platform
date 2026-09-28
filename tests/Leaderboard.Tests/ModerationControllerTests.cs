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

public class ModerationControllerTests
{
	[Fact]
	public async Task ApproveScore_WhenUserCanModerate_ApprovesScore()
	{
		var scores = new Mock<IScoreRepository>();
		var moderators = new Mock<IGameModeratorRepository>();
		scores.Setup(item => item.GetByIdAsync(30)).ReturnsAsync(new Score { Id = 30, UserId = 2, Game = new Game { Id = 7, Name = "Arcade", Description = "Classic game" } });
		moderators.Setup(item => item.CanModerateGameAsync(7, 8)).ReturnsAsync(true);
		var controller = CreateController(scores.Object, moderators.Object, "8");

		var result = await controller.ApproveScore(30);

		Assert.IsType<OkObjectResult>(result);
		scores.Verify(item => item.ApproveScoreAsync(30, 8), Times.Once);
	}

	[Fact]
	public async Task RejectScore_WhenUserTriesToRejectOwnScore_ReturnsBadRequest()
	{
		var scores = new Mock<IScoreRepository>();
		var moderators = new Mock<IGameModeratorRepository>();
		scores.Setup(item => item.GetByIdAsync(30)).ReturnsAsync(new Score { Id = 30, UserId = 8, Game = new Game { Id = 7, Name = "Arcade", Description = "Classic game" } });
		moderators.Setup(item => item.CanModerateGameAsync(7, 8)).ReturnsAsync(true);
		var controller = CreateController(scores.Object, moderators.Object, "8");

		var result = await controller.RejectScore(30, new RejectScoreRequest { Reason = "duplicate" });

		Assert.IsType<BadRequestObjectResult>(result);
		scores.Verify(item => item.RejectScoreAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
	}

	[Fact]
	public async Task CanModerateGame_WithInvalidIdentity_ReturnsUnauthorized()
	{
		var controller = CreateController(Mock.Of<IScoreRepository>(), Mock.Of<IGameModeratorRepository>(), "invalid");

		var result = await controller.CanModerateGame(7);

		Assert.IsType<UnauthorizedObjectResult>(result);
	}

	private static ModerationController CreateController(IScoreRepository scores, IGameModeratorRepository moderators, string userId)
	{
		var controller = new ModerationController(
			scores,
			moderators,
			Mock.Of<IGameRepository>(),
			Mock.Of<IUserRepository>(),
			NullLogger<ModerationController>.Instance);
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