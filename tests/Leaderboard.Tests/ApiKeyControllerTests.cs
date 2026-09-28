using System.Security.Claims;
using Leaderboard.Controllers;
using Leaderboard.Dtos;
using Leaderboard.Middleware;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class ApiKeyControllerTests
{
	[Fact]
	public async Task GenerateKey_WithMissingName_ReturnsBadRequest()
	{
		var gameRepository = new Mock<IGameRepository>();
		var controller = CreateController(gameRepository.Object, Mock.Of<IApiKeyRepository>(), Mock.Of<IApiKeyService>(), Mock.Of<IApiKeyAuthorizationService>(), "8");

		var result = await controller.GenerateKey(new CreateApiKeyRequest { Name = " ", GameId = 7 });

		Assert.IsType<BadRequestObjectResult>(result);
		gameRepository.Verify(item => item.GetGameByIdAsync(It.IsAny<int>()), Times.Never);
	}

	[Fact]
	public async Task GenerateKey_WithManagePermission_CreatesKey()
	{
		var gameRepository = new Mock<IGameRepository>();
		var keyRepository = new Mock<IApiKeyRepository>();
		var keyService = new Mock<IApiKeyService>();
		var authorization = new Mock<IApiKeyAuthorizationService>();
		gameRepository.Setup(item => item.GetGameByIdAsync(7)).ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Classic game" });
		keyService.Setup(item => item.GenerateApiKey(It.IsAny<int>())).Returns("plain-key");
		keyService.Setup(item => item.HashApiKey("plain-key")).Returns("hashed-key");
		authorization.Setup(item => item.CanManageKeysAsync(It.IsAny<ClaimsPrincipal>(), 7)).ReturnsAsync(true);
		keyRepository.Setup(item => item.AddAsync(It.IsAny<ApiKey>()))
			.Callback<ApiKey>(key => key.Id = 20)
			.Returns(Task.CompletedTask);
		var controller = CreateController(gameRepository.Object, keyRepository.Object, keyService.Object, authorization.Object, "8");

		var result = await controller.GenerateKey(new CreateApiKeyRequest { Name = "Production", GameId = 7, Permissions = ApiKeyPermissions.SubmitScores });

		var created = Assert.IsType<CreatedAtActionResult>(result);
		var response = Assert.IsType<CreatedApiKeyDto>(created.Value);
		Assert.Equal("plain-key", response.ApiKey);
		Assert.Equal(20, response.Id);
		keyRepository.Verify(item => item.AddAsync(It.Is<ApiKey>(key => key.KeyHash == "hashed-key" && key.UserId == 8)), Times.Once);
	}

	private static ApiKeyController CreateController(
		IGameRepository gameRepository,
		IApiKeyRepository keyRepository,
		IApiKeyService keyService,
		IApiKeyAuthorizationService authorization,
		string userId)
	{
		var controller = new ApiKeyController(keyRepository, keyService, gameRepository, authorization);
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