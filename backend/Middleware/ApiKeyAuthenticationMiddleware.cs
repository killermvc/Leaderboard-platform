using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;

using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;

namespace Leaderboard.Middleware;

/// <summary>
/// Authenticates requests that carry an api key, either in the <c>X-API-Key</c> header
/// or, for clients that cannot set headers, as a query string value (<c>?apiKey=</c> / <c>?api_key=</c>).
///
/// The presented key is hashed with sha256 and matched against the stored <see cref="ApiKey.KeyHash"/>;
/// the raw key is never persisted or logged. A request without any api key is left untouched so it can
/// still be served anonymously or authenticated by jwt, while a request with an api key that cannot be
/// validated is rejected.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
{
	/// <summary>
	/// The name of the header that carries the api key.
	/// </summary>
	public const string HeaderName = "X-API-Key";

	/// <summary>
	/// Query string names accepted as a fallback for clients that cannot send the header.
	/// </summary>
	public static readonly string[] QueryStringNames = ["apiKey", "api_key"];

	/// <summary>
	/// How often a key's LastUsedAt is written back, to avoid a database write on every request.
	/// </summary>
	private static readonly TimeSpan LastUsedWriteInterval = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Processes a request, authenticating it with an api key if one is presented.
	/// </summary>
	public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService, IApiKeyRepository apiKeyRepository)
	{
		// A validated jwt always wins, api key authentication only fills in anonymous requests.
		if (context.User.Identity?.IsAuthenticated == true)
		{
			await next(context);
			return;
		}

		string? presentedKey = ExtractKey(context);
		if (string.IsNullOrWhiteSpace(presentedKey))
		{
			await next(context);
			return;
		}

		string keyHash = apiKeyService.HashApiKey(presentedKey);
		ApiKey? apiKey;

		try
		{
			apiKey = await apiKeyRepository.GetByHashAsync(keyHash);
		}
		catch (Exception ex)
		{
			// A presented key must never fall through to anonymous access because of a lookup failure.
			logger.LogError(ex, "Api key validation failed while looking up key hash {KeyHashPrefix}.", keyHash[..8]);
			await RejectAsync(context, StatusCodes.Status503ServiceUnavailable, "Api key validation is temporarily unavailable.");
			return;
		}

		if (apiKey is null)
		{
			logger.LogWarning("Rejected request to {Path}: unknown api key.", context.Request.Path);
			await RejectAsync(context, StatusCodes.Status401Unauthorized, "Invalid api key.");
			return;
		}

		if (apiKey.RevokedAt.HasValue)
		{
			logger.LogWarning("Rejected request to {Path}: api key {ApiKeyId} is revoked.", context.Request.Path, apiKey.Id);
			await RejectAsync(context, StatusCodes.Status401Unauthorized, "Api key has been revoked.");
			return;
		}

		if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value <= DateTime.UtcNow)
		{
			logger.LogWarning("Rejected request to {Path}: api key {ApiKeyId} expired at {ExpiresAt}.", context.Request.Path, apiKey.Id, apiKey.ExpiresAt);
			await RejectAsync(context, StatusCodes.Status401Unauthorized, "Api key has expired.");
			return;
		}

		int? requestedGameId = GetRequestedGameId(context);
		if (requestedGameId.HasValue && requestedGameId.Value != apiKey.GameId)
		{
			logger.LogWarning(
				"Rejected request to {Path}: api key {ApiKeyId} is scoped to game {KeyGameId} but game {RequestedGameId} was requested.",
				context.Request.Path, apiKey.Id, apiKey.GameId, requestedGameId.Value);
			await RejectAsync(context, StatusCodes.Status403Forbidden, "Api key is not valid for this game.");
			return;
		}

		context.User = CreatePrincipal(apiKey);

		await RecordUsageAsync(context, apiKeyRepository, apiKey);
		await next(context);
	}

	/// <summary>
	/// Reads the api key from the header, falling back to the query string for header-less clients.
	/// </summary>
	public static string? ExtractPresentedKey(HttpContext context)
	{
		if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue) && !StringValues.IsNullOrEmpty(headerValue))
		{
			return headerValue.ToString();
		}

		foreach (string name in QueryStringNames)
		{
			if (context.Request.Query.TryGetValue(name, out var queryValue) && !StringValues.IsNullOrEmpty(queryValue))
			{
				return queryValue.ToString();
			}
		}

		return null;
	}

	private static string? ExtractKey(HttpContext context) => ExtractPresentedKey(context);

	/// <summary>
	/// Resolves the game the caller is asking about, from the route or the query string.
	/// </summary>
	private static int? GetRequestedGameId(HttpContext context)
	{
		if (context.Request.RouteValues.TryGetValue("gameId", out object? routeValue) &&
			int.TryParse(routeValue?.ToString(), out int routeGameId))
		{
			return routeGameId;
		}

		if (context.Request.Query.TryGetValue("gameId", out var queryValue) &&
			int.TryParse(queryValue.ToString(), out int queryGameId))
		{
			return queryGameId;
		}

		return null;
	}

	/// <summary>
	/// Builds the request principal for an api key. The name claim holds the key owner's user id,
	/// so game clients can act as their key's owner without a jwt.
	/// </summary>
	private static ClaimsPrincipal CreatePrincipal(ApiKey apiKey)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, apiKey.UserId.ToString(CultureInfo.InvariantCulture)),
			new(ClaimTypes.Name, apiKey.UserId.ToString(CultureInfo.InvariantCulture)),
			new(ApiKeyClaims.KeyId, apiKey.Id.ToString(CultureInfo.InvariantCulture)),
			new(ApiKeyClaims.GameId, apiKey.GameId.ToString(CultureInfo.InvariantCulture)),
			new(ApiKeyClaims.Permissions, ((int)apiKey.Permissions).ToString(CultureInfo.InvariantCulture))
		};

		return new ClaimsPrincipal(new ClaimsIdentity(claims, ApiKeyClaims.AuthenticationType, ClaimTypes.Name, ClaimTypes.Role));
	}

	private async Task RecordUsageAsync(HttpContext context, IApiKeyRepository apiKeyRepository, ApiKey apiKey)
	{
		DateTime now = DateTime.UtcNow;
		if (apiKey.LastUsedAt.HasValue && now - apiKey.LastUsedAt.Value < LastUsedWriteInterval)
		{
			return;
		}

		try
		{
			apiKey.LastUsedAt = now;
			await apiKeyRepository.UpdateAsync(apiKey);
		}
		catch (Exception ex)
		{
			// Usage tracking is best effort, it must not fail an otherwise valid request.
			logger.LogWarning(ex, "Could not update LastUsedAt for api key {ApiKeyId}.", apiKey.Id);
		}
	}

	private static async Task RejectAsync(HttpContext context, int statusCode, string message)
	{
		context.Response.StatusCode = statusCode;
		await context.Response.WriteAsJsonAsync(new { Message = message });
	}
}
