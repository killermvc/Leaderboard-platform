using System.Net.Http.Headers;
using System.Text.Json;
using Leaderboard.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Leaderboard.Services;

/// <summary>
/// Represents a service that interacts with the Clerk API to retrieve user information and display names. This service provides methods for searching users, retrieving display names, and caching user data for improved performance. It encapsulates the logic for making HTTP requests to the Clerk API and processing the responses, ensuring that user information is retrieved efficiently and securely.
/// </summary>
public sealed class ClerkDirectoryService(
	HttpClient httpClient,
	IOptions<ClerkOptions> options,
	IMemoryCache? cache = null) : IClerkDirectoryService
{
	private static readonly TimeSpan UserCacheLifetime = TimeSpan.FromMinutes(5);
	/// <summary>
	/// Searches for users in the Clerk directory based on the specified query and limit. This method sends an HTTP request to the Clerk API to retrieve user information matching the provided query. It returns a read-only list of <see cref="ClerkDirectoryUser"/> objects representing the matching users. The method ensures that the query is properly encoded and that the limit is clamped between 1 and 100. If the Clerk secret key is not configured, an <see cref="InvalidOperationException"/> is thrown.
	/// </summary>
	/// <param name="query">The query to search for.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A read-only list of <see cref="ClerkDirectoryUser"/> objects.</returns>
	/// <exception cref="InvalidOperationException"></exception>
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

	/// <summary>
	/// Retrieves the display names for a collection of Clerk user IDs. This method sends HTTP requests to the Clerk API to retrieve user information for each provided user ID. It returns a read-only dictionary mapping each user ID to its corresponding display name. If the Clerk secret key is not configured, the method returns a dictionary where each user ID maps to itself. The method ensures that only distinct and non-empty user IDs are processed, and it utilizes caching to improve performance for repeated requests.
	/// </summary>
	/// <param name="clerkUserIds">The collection of Clerk user IDs for which to retrieve display names.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A read-only dictionary mapping each user ID to its corresponding display name.</returns>
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

	/// <summary>
	/// Retrieves the display name for a single Clerk user ID. This method sends an HTTP request to the Clerk API to retrieve user information for the provided user ID. It returns the display name of the user, or the user ID itself if the user is not found or an error occurs.
	/// </summary>
	/// <param name="clerkUserId">The Clerk user ID for which to retrieve the display name.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>The display name of the user, or the user ID itself if the user is not found or an error occurs.</returns>
	public async Task<string> GetDisplayNameAsync(string clerkUserId, CancellationToken cancellationToken = default)
	{
		ClerkDirectoryUser? user = await GetUserAsync(clerkUserId, cancellationToken);
		return user?.DisplayName ?? clerkUserId;
	}

	/// <summary>
	/// Retrieves the user information for a single Clerk user ID. This method sends an HTTP request to the Clerk API to retrieve user information for the provided user ID. It returns a <see cref="ClerkDirectoryUser"/> object representing the user, or null if the user is not found or an error occurs. The method utilizes caching to improve performance for repeated requests.
	/// </summary>
	/// <param name="clerkUserId">The Clerk user ID for which to retrieve the user information.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ClerkDirectoryUser"/> object representing the user, or null if the user is not found or an error occurs.</returns>
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