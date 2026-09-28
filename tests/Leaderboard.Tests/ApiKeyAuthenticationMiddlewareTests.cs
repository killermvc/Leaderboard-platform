using Leaderboard.Middleware;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class ApiKeyAuthenticationMiddlewareTests
{
	[Fact]
	public async Task InvalidKey_ReturnsUnauthorizedAndDoesNotCallNext()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("invalid-key");
		repository.Setup(item => item.GetByHashAsync("invalid-hash")).ReturnsAsync((ApiKey?)null);
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});

		var context = CreateContext("invalid-key");
		await middleware.InvokeAsync(context, service.Object, repository.Object);
		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
		Assert.Equal(0, nextCalls);
	}

	[Fact]
	public async Task RevokedKey_ReturnsUnauthorizedAndDoesNotCallNext()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("revoked-key");
		repository.Setup(item => item.GetByHashAsync("revoked-hash"))
			.ReturnsAsync(CreateApiKey(revokedAt: DateTime.UtcNow));
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});

		var context = CreateContext("revoked-key");
		await middleware.InvokeAsync(context, service.Object, repository.Object);

		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
		Assert.Equal(0, nextCalls);
	}

	[Fact]
	public async Task KeyForDifferentRequestedGame_ReturnsForbiddenAndDoesNotCallNext()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("game-key");
		repository.Setup(item => item.GetByHashAsync("game-hash")).ReturnsAsync(CreateApiKey(gameId: 7));
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});
		var context = CreateContext("game-key");
		context.Request.QueryString = new QueryString("?gameId=8");

		await middleware.InvokeAsync(context, service.Object, repository.Object);

		Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
		Assert.Equal(0, nextCalls);
	}

	[Fact]
	public async Task ExpiredKey_ReturnsUnauthorizedAndDoesNotCallNext()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("expired-key");
		repository.Setup(item => item.GetByHashAsync("expired-hash"))
			.ReturnsAsync(CreateApiKey(expiresAt: DateTime.UtcNow.AddMinutes(-1)));
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});

		var context = CreateContext("expired-key");
		await middleware.InvokeAsync(context, service.Object, repository.Object);

		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
		Assert.Equal(0, nextCalls);
	}

	private static Mock<IApiKeyService> CreateService(string key)
	{
		var service = new Mock<IApiKeyService>();
		service.Setup(item => item.HashApiKey(key)).Returns(key.Replace("key", "hash"));
		return service;
	}

	private static ApiKeyAuthenticationMiddleware CreateMiddleware(RequestDelegate next) =>
		new(next, NullLogger<ApiKeyAuthenticationMiddleware>.Instance);

	private static DefaultHttpContext CreateContext(string apiKey)
	{
		var context = new DefaultHttpContext();
		context.Request.Headers[ApiKeyAuthenticationMiddleware.HeaderName] = apiKey;
		return context;
	}

	private static ApiKey CreateApiKey(
		int gameId = 7,
		DateTime? revokedAt = null,
		DateTime? expiresAt = null) => new()
	{
		Id = 12,
		Name = "Test key",
		KeyHash = "hash",
		GameId = gameId,
		UserId = 8,
		Permissions = ApiKeyPermissions.SubmitScores,
		CreatedAt = DateTime.UtcNow.AddDays(-1),
		RevokedAt = revokedAt,
		ExpiresAt = expiresAt
	};
}