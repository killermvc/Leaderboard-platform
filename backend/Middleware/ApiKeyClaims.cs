using System.Globalization;
using System.Security.Claims;

using Leaderboard.Models;

namespace Leaderboard.Middleware;

/// <summary>
/// Claim types written by <see cref="ApiKeyAuthenticationMiddleware"/> plus helpers
/// for controllers to read the authenticated api key context.
/// </summary>
public static class ApiKeyClaims
{
	public const string AuthenticationType = "ApiKey";

	public const string KeyId = "apikey:id";
	public const string GameId = "apikey:gameId";
	public const string Permissions = "apikey:permissions";

	/// <summary>
	/// Gets the id of the api key that authenticated the request, or null when the
	/// request was authenticated by other means (jwt, anonymous).
	/// </summary>
	public static int? GetApiKeyId(this ClaimsPrincipal user)
	{
		return ParseInt(user.FindFirst(KeyId)?.Value);
	}

	/// <summary>
	/// Gets the game the authenticating api key is scoped to, or null when the
	/// request was not authenticated with an api key.
	/// </summary>
	public static int? GetApiKeyGameId(this ClaimsPrincipal user)
	{
		return ParseInt(user.FindFirst(GameId)?.Value);
	}

	/// <summary>
	/// Gets the permissions granted to the authenticating api key.
	/// </summary>
	public static ApiKeyPermissions GetApiKeyPermissions(this ClaimsPrincipal user)
	{
		string? value = user.FindFirst(Permissions)?.Value;
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int permissions)
			? (ApiKeyPermissions)permissions
			: ApiKeyPermissions.None;
	}

	/// <summary>
	/// Checks whether the authenticating api key was granted every requested permission.
	/// Always false when the request was not authenticated with an api key.
	/// </summary>
	public static bool HasApiKeyPermission(this ClaimsPrincipal user, ApiKeyPermissions permission)
	{
		if (permission == ApiKeyPermissions.None)
		{
			return false;
		}

		ApiKeyPermissions granted = user.GetApiKeyPermissions();
		return (granted & permission) == permission;
	}

	private static int? ParseInt(string? value)
	{
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;
	}
}
