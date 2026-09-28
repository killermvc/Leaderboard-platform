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

public class V1ScoreControllerTests
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
			.Setup(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 9000, "submission-1", "New record", null))
			.ReturnsAsync(score);

		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);
		var result = await controller.SubmitScore(new SubmitScoreRequest
		{
			Name = "Ryu",
			Score = 9000,
			SubmissionId = "submission-1",
			Title = "New record"
		});

		var created = Assert.IsType<ObjectResult>(result);
		Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
		var response = Assert.IsType<ScoreDto>(created.Value);
		Assert.Equal(ScoreStatus.Approved, response.Status);
		Assert.Null(response.User);
		Assert.Equal("Ryu", response.PlayerName);
		scoreRepository.Verify(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 9000, "submission-1", "New record", null), Times.Once);
	}

	[Fact]
	public async Task SubmitScore_WithoutApiKey_ReturnsUnauthorized()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7, authenticated: false);

		var result = await controller.SubmitScore(new SubmitScoreRequest { Name = "Ryu", Score = 100 });

		Assert.IsType<UnauthorizedObjectResult>(result);
		scoreRepository.Verify(repository => repository.SubmitNamedScoreAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
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

	[Fact]
	public async Task SubmitScore_WithMatchingGameId_Succeeds()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var game = new Game { Id = 7, Name = "Arcade", Description = "Test game" };
		var score = new Score { Id = 42, Game = game, PlayerName = "Ryu", Value = 100, Status = ScoreStatus.Approved };
		gameRepository.Setup(repository => repository.GetGameByIdAsync(7)).ReturnsAsync(game);
		scoreRepository.Setup(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 100, "submission-1", null, null))
			.ReturnsAsync(score);
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(new SubmitScoreRequest
		{
			GameId = 7,
			Name = "Ryu",
			Score = 100,
			SubmissionId = "submission-1"
		});

		Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
	}

	[Fact]
	public async Task SubmitScore_WhenGameMissing_ReturnsNotFound()
	{
		var gameRepository = new Mock<IGameRepository>();
		gameRepository.Setup(repository => repository.GetGameByIdAsync(7)).ReturnsAsync((Game?)null);
		var controller = CreateController(new Mock<IScoreRepository>(), gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(ValidRequest());

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task SubmitScore_WhenGameDisallowsSubmissions_ReturnsBadRequest()
	{
		var gameRepository = new Mock<IGameRepository>();
		gameRepository.Setup(repository => repository.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Test game", SubmitsAllowed = false });
		var scoreRepository = new Mock<IScoreRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(ValidRequest());

		Assert.IsType<BadRequestObjectResult>(result);
		scoreRepository.Verify(repository => repository.SubmitNamedScoreAsync(
			It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
	}

	[Fact]
	public async Task SubmitScore_WhenRepositoryCannotFindGame_ReturnsNotFound()
	{
		var gameRepository = new Mock<IGameRepository>();
		gameRepository.Setup(repository => repository.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Test game" });
		var scoreRepository = new Mock<IScoreRepository>();
		scoreRepository.Setup(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 100, "submission-1", null, null))
			.ThrowsAsync(new KeyNotFoundException("Game not found"));
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(ValidRequest());

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task SubmitScore_WhenRepositoryRejectsSubmission_ReturnsBadRequest()
	{
		var gameRepository = new Mock<IGameRepository>();
		gameRepository.Setup(repository => repository.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Test game" });
		var scoreRepository = new Mock<IScoreRepository>();
		scoreRepository.Setup(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 100, "submission-1", null, null))
			.ThrowsAsync(new InvalidOperationException("Duplicate submission"));
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(ValidRequest());

		Assert.IsType<BadRequestObjectResult>(result);
	}

	[Fact]
	public async Task SubmitScore_WhenRepositoryFailsUnexpectedly_ReturnsInternalServerError()
	{
		var gameRepository = new Mock<IGameRepository>();
		gameRepository.Setup(repository => repository.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Test game" });
		var scoreRepository = new Mock<IScoreRepository>();
		scoreRepository.Setup(repository => repository.SubmitNamedScoreAsync(7, "Ryu", 100, "submission-1", null, null))
			.ThrowsAsync(new Exception("database failure"));
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.SubmitScores, 7);

		var result = await controller.SubmitScore(ValidRequest());

		Assert.Equal(StatusCodes.Status500InternalServerError, Assert.IsType<ObjectResult>(result).StatusCode);
	}

	[Fact]
	public async Task SubmitScore_WithJwtOnlyClaims_ReturnsUnauthorized()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.None, 7, authenticated: false);
		controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, "8")], "Bearer"));

		var result = await controller.SubmitScore(ValidRequest());

		Assert.IsType<UnauthorizedObjectResult>(result);
	}

	[Fact]
	public async Task GetScoreByPlayerName_WithReadPermission_ReturnsBestScore()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var score = new Score
		{
			Id = 42,
			Game = new Game { Id = 7, Name = "Arcade", Description = "Test game" },
			PlayerName = "Ryu",
			Value = 9000,
			Status = ScoreStatus.Approved
		};
		scoreRepository
			.Setup(repository => repository.GetBestNamedScoreByGameAsync(7, "Ryu"))
			.ReturnsAsync(score);

		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.ReadScores, 7);
		var result = await controller.GetScoreByPlayerName(7, "Ryu");

		var response = Assert.IsType<OkObjectResult>(result);
		var scoreDto = Assert.IsType<ScoreDto>(response.Value);
		Assert.Equal("Ryu", scoreDto.PlayerName);
		Assert.Equal(9000, scoreDto.Value);
	}

	[Fact]
	public async Task GetLeaderboard_WithReadPermission_ReturnsEntireGameLeaderboard()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var leaderboard = new List<LeaderboardEntry>
		{
			new() { UserName = "Ryu", Score = 9000 }
		};
		scoreRepository
			.Setup(repository => repository.GetLeaderboardAsync(7, int.MaxValue))
			.ReturnsAsync(leaderboard);

		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.ReadLeaderboard, 7);
		var result = await controller.GetLeaderboard(7);

		var response = Assert.IsType<OkObjectResult>(result);
		Assert.Same(leaderboard, response.Value);
		scoreRepository.Verify(repository => repository.GetLeaderboardAsync(7, int.MaxValue), Times.Once);
	}

	[Fact]
	public async Task GetScoreByPlayerName_WithoutReadPermission_ReturnsForbidden()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.ReadLeaderboard, 7);

		var result = await controller.GetScoreByPlayerName(7, "Ryu");

		Assert.IsType<ForbidResult>(result);
		scoreRepository.Verify(repository => repository.GetBestNamedScoreByGameAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task GetLeaderboard_WithDifferentGameId_ReturnsForbidden()
	{
		var scoreRepository = new Mock<IScoreRepository>();
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(scoreRepository, gameRepository, ApiKeyPermissions.ReadLeaderboard, 7);

		var result = await controller.GetLeaderboard(8);

		Assert.IsType<ForbidResult>(result);
		scoreRepository.Verify(repository => repository.GetLeaderboardAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
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

	[Fact]
	public void SubmitScoreRequest_RequiresSubmissionId()
	{
		var request = new SubmitScoreRequest { Name = "Ryu", Score = 100 };
		var validationContext = new ValidationContext(request);
		var errors = new List<ValidationResult>();

		Assert.False(Validator.TryValidateObject(request, validationContext, errors, validateAllProperties: true));

		Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SubmitScoreRequest.SubmissionId)));
	}

	[Fact]
	public void SubmitScoreRequest_RejectsOversizedNameAndSubmissionId()
	{
		var request = new SubmitScoreRequest
		{
			Name = new string('n', 65),
			Score = 100,
			SubmissionId = new string('s', 129)
		};
		var validationContext = new ValidationContext(request);
		var errors = new List<ValidationResult>();

		Assert.False(Validator.TryValidateObject(request, validationContext, errors, validateAllProperties: true));
		Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SubmitScoreRequest.Name)));
		Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SubmitScoreRequest.SubmissionId)));
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

	private static SubmitScoreRequest ValidRequest() => new()
	{
		Name = "Ryu",
		Score = 100,
		SubmissionId = "submission-1"
	};
}
