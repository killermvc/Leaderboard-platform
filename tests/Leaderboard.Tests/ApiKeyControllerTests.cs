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

	[Fact]
	public async Task GenerateKey_PersistsPermissionsAndExpiry_WhileReturningRawKeyOnlyInCreationResponse()
	{
		var gameRepository = new Mock<IGameRepository>();
		var keyRepository = new Mock<IApiKeyRepository>();
		var keyService = new Mock<IApiKeyService>();
		var authorization = new Mock<IApiKeyAuthorizationService>();
		var expiresAt = DateTime.UtcNow.AddDays(30);
		ApiKey? persistedKey = null;
		gameRepository.Setup(item => item.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Classic game" });
		keyService.Setup(item => item.GenerateApiKey(It.IsAny<int>())).Returns("plain-key");
		keyService.Setup(item => item.HashApiKey("plain-key")).Returns("hashed-key");
		authorization.Setup(item => item.CanManageKeysAsync(It.IsAny<ClaimsPrincipal>(), 7)).ReturnsAsync(true);
		keyRepository.Setup(item => item.AddAsync(It.IsAny<ApiKey>()))
			.Callback<ApiKey>(key =>
			{
				persistedKey = key;
				key.Id = 20;
			})
			.Returns(Task.CompletedTask);
		var controller = CreateController(gameRepository.Object, keyRepository.Object, keyService.Object, authorization.Object, "8");

		var result = await controller.GenerateKey(new CreateApiKeyRequest
		{
			Name = "Production",
			GameId = 7,
			Permissions = ApiKeyPermissions.SubmitScores | ApiKeyPermissions.ReadLeaderboard,
			ExpiresAt = expiresAt
		});

		var created = Assert.IsType<CreatedAtActionResult>(result);
		var response = Assert.IsType<CreatedApiKeyDto>(created.Value);
		Assert.Equal("plain-key", response.ApiKey);
		Assert.Equal(expiresAt, response.ExpiresAt);
		Assert.NotNull(persistedKey);
		Assert.Equal("hashed-key", persistedKey!.KeyHash);
		Assert.Equal(ApiKeyPermissions.SubmitScores | ApiKeyPermissions.ReadLeaderboard, persistedKey.Permissions);
		Assert.Equal(expiresAt, persistedKey.ExpiresAt);
		Assert.DoesNotContain("KeyHash", response.GetType().GetProperties().Select(property => property.Name));
	}

	[Fact]
	public async Task GetKeysForGame_ReturnsMetadataWithoutRawKeyOrHash()
	{
		var gameRepository = new Mock<IGameRepository>();
		var keyRepository = new Mock<IApiKeyRepository>();
		var authorization = new Mock<IApiKeyAuthorizationService>();
		gameRepository.Setup(item => item.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Classic game" });
		authorization.Setup(item => item.CanManageKeysAsync(It.IsAny<ClaimsPrincipal>(), 7)).ReturnsAsync(true);
		keyRepository.Setup(item => item.GetKeysForGameAsync(7)).ReturnsAsync([
			new ApiKey
			{
				Id = 20,
				Name = "Production",
				KeyHash = "hashed-key",
				GameId = 7,
				UserId = 8,
				Permissions = ApiKeyPermissions.SubmitScores,
				CreatedAt = DateTime.UtcNow
			}
		]);
		var controller = CreateController(gameRepository.Object, keyRepository.Object, Mock.Of<IApiKeyService>(), authorization.Object, "8");

		var result = await controller.GetKeysForGame(7);

		var response = Assert.IsAssignableFrom<IEnumerable<ApiKeyDto>>(Assert.IsType<OkObjectResult>(result).Value);
		var listedKey = Assert.Single(response);
		Assert.Equal(20, listedKey.Id);
		var serialized = System.Text.Json.JsonSerializer.Serialize(listedKey);
		Assert.DoesNotContain("plain-key", serialized);
		Assert.DoesNotContain("hashed-key", serialized);
	}

	[Fact]
	public async Task RegenerateKey_RevokesOldKeyAndCreatesReplacementWithSamePermissionsAndExpiry()
	{
		var gameRepository = new Mock<IGameRepository>();
		var keyRepository = new Mock<IApiKeyRepository>();
		var keyService = new Mock<IApiKeyService>();
		var authorization = new Mock<IApiKeyAuthorizationService>();
		var expiresAt = DateTime.UtcNow.AddDays(30);
		var existingKey = new ApiKey
		{
			Id = 20,
			Name = "Production",
			KeyHash = "old-hash",
			GameId = 7,
			UserId = 8,
			Permissions = ApiKeyPermissions.SubmitScores,
			ExpiresAt = expiresAt,
			CreatedAt = DateTime.UtcNow.AddDays(-1)
		};
		ApiKey? replacementKey = null;
		keyRepository.Setup(item => item.GetByIdAsync(20)).ReturnsAsync(existingKey);
		keyRepository.Setup(item => item.HasKeyWithNameAsync(7, "Production", 20)).ReturnsAsync(false);
		keyRepository.Setup(item => item.RegenerateAsync(existingKey, It.IsAny<ApiKey>()))
			.Callback<ApiKey, ApiKey>((_, replacement) =>
			{
				replacementKey = replacement;
				replacement.Id = 21;
			})
			.Returns(Task.CompletedTask);
		gameRepository.Setup(item => item.GetGameByIdAsync(7))
			.ReturnsAsync(new Game { Id = 7, Name = "Arcade", Description = "Classic game" });
		keyService.Setup(item => item.GenerateApiKey(It.IsAny<int>())).Returns("replacement-key");
		keyService.Setup(item => item.HashApiKey("replacement-key")).Returns("replacement-hash");
		authorization.Setup(item => item.CanManageKeysAsync(It.IsAny<ClaimsPrincipal>(), 7)).ReturnsAsync(true);
		var controller = CreateController(gameRepository.Object, keyRepository.Object, keyService.Object, authorization.Object, "8");

		var result = await controller.RegenerateKey(20);

		var response = Assert.IsType<CreatedApiKeyDto>(Assert.IsType<CreatedAtActionResult>(result).Value);
		Assert.Equal("replacement-key", response.ApiKey);
		Assert.NotNull(replacementKey);
		Assert.Equal("replacement-hash", replacementKey!.KeyHash);
		Assert.Equal(existingKey.Permissions, replacementKey.Permissions);
		Assert.Equal(existingKey.ExpiresAt, replacementKey.ExpiresAt);
		Assert.Equal("Production", replacementKey.Name);
		keyRepository.Verify(item => item.RegenerateAsync(existingKey, replacementKey), Times.Once);
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