using System.Net;
using System.Text.Json;
using Leaderboard.Options;
using Leaderboard.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Leaderboard.Tests;

public class ClerkDirectoryServiceTests
{
	[Fact]
	public async Task GetDisplayNamesAsync_MapsUsernamesAndFallsBackToId()
	{
		var handler = new FakeHandler(request =>
		{
			string url = request.RequestUri!.ToString();
			if (url.EndsWith("/v1/users/user_123"))
				return JsonResponse(new { id = "user_123", username = "alice" });
			if (url.EndsWith("/v1/users/user_456"))
				return JsonResponse(new { id = "user_456", username = (string?)null, first_name = "Bob", last_name = "Builder" });
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var service = CreateService(handler, "sk_test_123");

		var result = await service.GetDisplayNamesAsync(["user_123", "user_456", "user_missing"]);

		Assert.Equal("alice", result["user_123"]);
		Assert.Equal("Bob Builder", result["user_456"]);
		Assert.Equal("user_missing", result["user_missing"]);
	}

	[Fact]
	public async Task GetDisplayNamesAsync_WithEmptyInput_MakesNoHttpCalls()
	{
		var handler = new FakeHandler(_ => JsonResponse(new { id = "x" }));
		var service = CreateService(handler, "sk_test_123");

		var result = await service.GetDisplayNamesAsync([]);

		Assert.Empty(result);
		Assert.Equal(0, handler.CallCount);
	}

	[Fact]
	public async Task GetDisplayNameAsync_CachesSingleUserFetch()
	{
		var handler = new FakeHandler(_ => JsonResponse(new { id = "user_123", username = "alice" }));
		var service = CreateService(handler, "sk_test_123");

		string first = await service.GetDisplayNameAsync("user_123");
		string second = await service.GetDisplayNameAsync("user_123");

		Assert.Equal("alice", first);
		Assert.Equal("alice", second);
		Assert.Equal(1, handler.CallCount);
	}

	[Fact]
	public async Task GetDisplayNamesAsync_WithoutSecretKey_FallsBackToIds()
	{
		var handler = new FakeHandler(_ => JsonResponse(new { id = "user_123", username = "alice" }));
		var service = CreateService(handler, "");

		var result = await service.GetDisplayNamesAsync(["user_123"]);

		Assert.Equal("user_123", result["user_123"]);
		Assert.Equal(0, handler.CallCount);
	}

	private static ClerkDirectoryService CreateService(FakeHandler handler, string secretKey) =>
		new(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(new ClerkOptions { SecretKey = secretKey }), new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

	private static HttpResponseMessage JsonResponse(object payload) =>
		new(HttpStatusCode.OK)
		{
			Content = new StringContent(JsonSerializer.Serialize(payload))
		};

	private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public int CallCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			return Task.FromResult(responder(request));
		}
	}
}
