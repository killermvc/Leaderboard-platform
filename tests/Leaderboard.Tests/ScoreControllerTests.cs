using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Leaderboard.Controllers.V1;
using Leaderboard.Dtos;
using Leaderboard.Middleware;
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
	public async Task SubmitScore_WithApiKeyAndPermission_CreatesApprovedAnonymousScore()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var game = new Game { Id = 7, Name = "Arcade", Description = "Test game" };
		var score = new Score
		{
			Id = 42,
			Game = game,
			PlayerName = "Ryu",
			Value = 9000,
			Status = ScoreStatus.Approved,
			Title = "New record"
		};

		gameRepository.Setup(repository => repository.GetGameByIdAsync(7)).ReturnsAsync(game);
		scoreRepository
			.Setup(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 9000, "New record", null))
			.ReturnsAsync(score);

		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);
		var result = await controller.SubmitScore(new SubmitScoreRequest
		{
			Name = "Ryu",
			Score = 9000,
			Title = "New record"
		});

		var created = Assert.IsType<ObjectResult>(result);
		Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
		var response = Assert.IsType<ScoreDto>(created.Value);
		Assert.Equal(ScoreStatus.Approved, response.Status);
		Assert.Null(response.User);
		Assert.Equal("Ryu", response.PlayerName);
		scoreRepository.Verify(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 9000, "New record", null), Times.Once);
	}

	[Fact]
	public async Task SubmitScore_WithoutApiKey_ReturnsUnauthorized()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7, authenticated: false);

		var result = await controller.SubmitScore(new SubmitScoreRequest { Name = "Ryu", Score = 100 });

		Assert.IsType<UnauthorizedObjectResult>(result);
		scoreRepository.Verify(repository => repository.SubmitNamedScoreAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
	}

	[Fact]
	public async Task SubmitScore_WithoutSubmitPermission_ReturnsForbidden()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.ReadScores, 7);

		var result = await controller.SubmitScore(new SubmitScoreRequest { Name = "Ryu", Score = 100 });

		Assert.IsType<ForbidResult>(result);
		gameRepository.Verify(repository => repository.GetGameByIdAsync(It.IsAny<int>()), Times.Never);
	}

	[Fact]
	public async Task SubmitScore_WithDifferentGameId_ReturnsForbidden()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(new SubmitScoreRequest { GameId = 8, Name = "Ryu", Score = 100 });

		Assert.IsType<ForbidResult>(result);
		gameRepository.Verify(repository => repository.GetGameByIdAsync(It.IsAny<int>()), Times.Never);
	}

	[Theory]
	[InlineData(" ")]
	[InlineData("\t\n")]
	public void SubmitScoreRequest_RejectsWhitespaceOnlyName(string name)
	{
		var request = new SubmitScoreRequest { Name = name, Score = 100 };
		var validationContext = new ValidationContext(request);
		var errors = new List<ValidationResult>();

		Assert.False(Validator.TryValidateObject(request, validationContext, errors, validateAllProperties: true));

		Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SubmitScoreRequest.Name)));
	}

	private static ScoreController CreateController(
		Mock<IScoreRepository> scoreRepository,
		Mock<IGameRepository> gameRepository,
		ApiKeyPermissions permissions,
		int gameId,
		bool authenticated = true)
	{
		var controller = new ScoreController(
			scoreRepository.Object,
			gameRepository.Object,
			NullLogger<ScoreController>.Instance);

		var claims = authenticated
			? new[]
			{
				new Claim(ApiKeyClaims.KeyId, "12"),
				new Claim(ApiKeyClaims.GameId, gameId.ToString()),
				new Claim(ApiKeyClaims.Permissions, ((int)permissions).ToString())
			}
			: [];

		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? ApiKeyClaims.AuthenticationType : null))
			}
		};

		return controller;
	}
}
