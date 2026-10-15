using System.Net.Http.Headers;
using System.Text.Json;
using Leaderboard.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

#pragma warning disable CS1591

namespace Leaderboard.Services;

public sealed class ClerkDirectoryService(
	HttpClient httpClient,
	IOptions<ClerkOptions> options,
	IMemoryCache? cache = null) : IClerkDirectoryService
{
	private static readonly TimeSpan UserCacheLifetime = TimeSpan.FromMinutes(5);
	public async Task<IReadOnlyList<ClerkDirectoryUser>> SearchUsersAsync(
		string query,
		int limit,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
		{
			throw new InvalidOperationException("Clerk:SecretKey must be configured for user search.");
		}

		using var request = new HttpRequestMessage(
			HttpMethod.Get,
			$"https://api.clerk.com/v1/users?query={Uri.EscapeDataString(query)}&limit={Math.Clamp(limit, 1, 100)}");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.SecretKey);

		using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
		using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

		if (document.RootElement.ValueKind != JsonValueKind.Array)
		{
			return [];
		}

		return document.RootElement.EnumerateArray()
			.Select(MapUser)
			.Where(user => user is not null)
			.Select(user => user!)
			.ToArray();
	}

	public async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
		IReadOnlyCollection<string> clerkUserIds,
		CancellationToken cancellationToken = default)
	{
		var distinctIds = clerkUserIds
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		if (distinctIds.Length == 0)
		{
			return new Dictionary<string, string>(StringComparer.Ordinal);
		}

		if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
		{
			return distinctIds.ToDictionary(id => id, id => id, StringComparer.Ordinal);
		}

		var tasks = distinctIds.Select(id => GetDisplayNameAsync(id, cancellationToken)).ToArray();
		string[] displayNames = await Task.WhenAll(tasks);
		var result = new Dictionary<string, string>(StringComparer.Ordinal);
		for (int i = 0; i < distinctIds.Length; i++)
		{
			result[distinctIds[i]] = displayNames[i];
		}
		return result;
	}

	public async Task<string> GetDisplayNameAsync(string clerkUserId, CancellationToken cancellationToken = default)
	{
		ClerkDirectoryUser? user = await GetUserAsync(clerkUserId, cancellationToken);
		return user?.DisplayName ?? clerkUserId;
	}

	public async Task<ClerkDirectoryUser?> GetUserAsync(string clerkUserId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(clerkUserId))
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
		{
			return null;
		}

		string cacheKey = $"clerk:user:{clerkUserId}";
		if (cache is not null && cache.TryGetValue<ClerkDirectoryUser>(cacheKey, out ClerkDirectoryUser? cached) && cached is not null)
		{
			return cached;
		}

		try
		{
			using var request = new HttpRequestMessage(
				HttpMethod.Get,
				$"https://api.clerk.com/v1/users/{Uri.EscapeDataString(clerkUserId)}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.SecretKey);

			using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
			ClerkDirectoryUser? user = MapUser(document.RootElement);
			if (user is not null && cache is not null)
			{
				cache.Set(cacheKey, user, UserCacheLifetime);
			}
			return user;
		}
		catch (HttpRequestException)
		{
			return null;
		}
		catch (TaskCanceledException)
		{
			return null;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static ClerkDirectoryUser? MapUser(JsonElement user)
	{
		if (!user.TryGetProperty("id", out JsonElement idElement))
		{
			return null;
		}

		string clerkUserId = idElement.GetString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(clerkUserId))
		{
			return null;
		}

		string? displayName = GetString(user, "username");
		if (string.IsNullOrWhiteSpace(displayName))
		{
			displayName = string.Join(" ", new[] { GetString(user, "first_name"), GetString(user, "last_name") }
				.Where(value => !string.IsNullOrWhiteSpace(value)));
		}
		if (string.IsNullOrWhiteSpace(displayName))
		{
			displayName = clerkUserId;
		}

		string? imageUrl = GetString(user, "image_url") ?? GetString(user, "profile_image_url");
		if (string.IsNullOrWhiteSpace(imageUrl))
		{
			imageUrl = null;
		}

		return new ClerkDirectoryUser(clerkUserId, displayName, imageUrl);
	}

	private static string? GetString(JsonElement element, string propertyName) =>
		element.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() : null;
}

#pragma warning restore CS1591
