using System.Net;
using Leaderboard.Middleware;
using Leaderboard.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Leaderboard.Tests;

public class GameClientRateLimitingMiddlewareTests
{
	[Fact]
	public async Task ScoreSubmission_IsLimitedPerIp()
	{
		int nextCalls = 0;
		var middleware = CreateMiddleware(ipCapacity: 1, apiKeyCapacity: 10);

		await middleware.InvokeAsync(CreateContext("203.0.113.10", "first-key"));
		await middleware.InvokeAsync(CreateContext("203.0.113.10", "second-key"));

		Assert.Equal(1, nextCalls);

		GameClientRateLimitingMiddleware CreateMiddleware(int ipCapacity, int apiKeyCapacity)
		{
			return new GameClientRateLimitingMiddleware(
				context =>
				{
					nextCalls++;
					return Task.CompletedTask;
				},
				Microsoft.Extensions.Options.Options.Create(new RateLimitOptions
				{
					ScoreSubmission = new ScoreSubmissionRateLimitOptions
					{
						IpCapacity = ipCapacity,
						IpRefillPerSecond = 0.001,
						ApiKeyCapacity = apiKeyCapacity,
						ApiKeyRefillPerSecond = 0.001
					}
				}),
				NullLogger<GameClientRateLimitingMiddleware>.Instance);
		}
	}

	[Fact]
	public async Task ScoreSubmission_IsLimitedPerApiKeyAcrossIps()
	{
		var middleware = new GameClientRateLimitingMiddleware(
			_ => Task.CompletedTask,
			Microsoft.Extensions.Options.Options.Create(new RateLimitOptions
			{
				ScoreSubmission = new ScoreSubmissionRateLimitOptions
				{
					IpCapacity = 10,
					IpRefillPerSecond = 1,
					ApiKeyCapacity = 1,
					ApiKeyRefillPerSecond = 1
				}
			}),
			NullLogger<GameClientRateLimitingMiddleware>.Instance);

		HttpContext first = CreateContext("203.0.113.10", "shared-key");
		HttpContext second = CreateContext("203.0.113.11", "shared-key");

		await middleware.InvokeAsync(first);
		await middleware.InvokeAsync(second);

		Assert.Equal(StatusCodes.Status200OK, first.Response.StatusCode);
		Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
		Assert.Equal("1", second.Response.Headers.RetryAfter.ToString());
	}

	private static DefaultHttpContext CreateContext(string ipAddress, string apiKey)
	{
		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
		context.Request.Method = HttpMethods.Post;
		context.Request.Path = "/api/v1/scores/submit";
		context.Request.Headers[ApiKeyAuthenticationMiddleware.HeaderName] = apiKey;
		return context;
	}
}