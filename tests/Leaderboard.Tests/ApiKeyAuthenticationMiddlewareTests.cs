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
	public async Task ValidKey_CallsNextWithApiKeyClaimsAndRecordsUsage()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("valid-key");
		var apiKey = CreateApiKey(gameId: 7);
		repository.Setup(item => item.GetByHashAsync("valid-hash")).ReturnsAsync(apiKey);
		var middleware = CreateMiddleware(context =>
		{
			Assert.Equal(12, context.User.GetApiKeyId());
			Assert.Equal(7, context.User.GetApiKeyGameId());
			Assert.True(context.User.HasApiKeyPermission(ApiKeyPermissions.SubmitScores));
			return Task.CompletedTask;
		});

		var context = CreateContext("valid-key");
		await middleware.InvokeAsync(context, service.Object, repository.Object);

		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.NotNull(apiKey.LastUsedAt);
		repository.Verify(item => item.UpdateAsync(apiKey), Times.Once);
	}

	[Fact]
	public async Task MissingKey_PassesThroughWithoutRepositoryLookup()
	{
		var repository = new Mock<IApiKeyRepository>();
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});

		await middleware.InvokeAsync(new DefaultHttpContext(), Mock.Of<IApiKeyService>(), repository.Object);

		Assert.Equal(1, nextCalls);
		repository.Verify(item => item.GetByHashAsync(It.IsAny<string>()), Times.Never);
	}

	[Theory]
	[InlineData("apiKey")]
	[InlineData("api_key")]
	public async Task QueryStringKey_IsAccepted(string queryName)
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("query-key");
		repository.Setup(item => item.GetByHashAsync("query-hash")).ReturnsAsync(CreateApiKey());
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});
		var context = new DefaultHttpContext();
		context.Request.QueryString = new QueryString($"?{queryName}=query-key");

		await middleware.InvokeAsync(context, service.Object, repository.Object);

		Assert.Equal(1, nextCalls);
	}

	[Fact]
	public async Task HeaderKey_TakesPrecedenceOverQueryStringKey()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = new Mock<IApiKeyService>();
		service.Setup(item => item.HashApiKey("header-key")).Returns("header-hash");
		service.Setup(item => item.HashApiKey("query-key")).Returns("query-hash");
		repository.Setup(item => item.GetByHashAsync("header-hash")).ReturnsAsync(CreateApiKey());
		var middleware = CreateMiddleware(_ => Task.CompletedTask);
		var context = CreateContext("header-key");
		context.Request.QueryString = new QueryString("?apiKey=query-key");

		await middleware.InvokeAsync(context, service.Object, repository.Object);

		repository.Verify(item => item.GetByHashAsync("header-hash"), Times.Once);
		repository.Verify(item => item.GetByHashAsync("query-hash"), Times.Never);
	}

	[Fact]
	public async Task RepositoryFailure_ReturnsServiceUnavailableAndDoesNotCallNext()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("failure-key");
		repository.Setup(item => item.GetByHashAsync("failure-hash"))
			.ThrowsAsync(new InvalidOperationException("database unavailable"));
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});

		var context = CreateContext("failure-key");
		await middleware.InvokeAsync(context, service.Object, repository.Object);

		Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
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
	public async Task KeyForDifferentRouteGame_ReturnsForbiddenAndDoesNotCallNext()
	{
		var repository = new Mock<IApiKeyRepository>();
		var service = CreateService("route-key");
		repository.Setup(item => item.GetByHashAsync("route-hash")).ReturnsAsync(CreateApiKey(gameId: 7));
		int nextCalls = 0;
		var middleware = CreateMiddleware(_ =>
		{
			nextCalls++;
			return Task.CompletedTask;
		});
		var context = CreateContext("route-key");
		context.Request.RouteValues["gameId"] = 8;

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