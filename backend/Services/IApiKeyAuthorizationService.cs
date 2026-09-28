using System.Security.Claims;

namespace Leaderboard.Services;

/// <summary>
/// Interface for a service that authorizes API key actions based on user roles and game moderation status.
/// This service provides a single source of truth for determining whether a user can manage API keys for a specific game.
/// </summary>
public interface IApiKeyAuthorizationService
{
	/// <summary>
	/// Determines whether a user can manage api keys for a given game.
	/// This is the single source of truth for api key management permissions.
	/// </summary>
	Task<bool> CanManageKeysAsync(ClaimsPrincipal user, int gameId);
}