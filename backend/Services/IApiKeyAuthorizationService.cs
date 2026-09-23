using System.Security.Claims;

namespace Leaderboard.Services;

public interface IApiKeyAuthorizationService
{
	/// <summary>
	/// Determines whether a user can manage api keys for a given game.
	/// This is the single source of truth for api key management permissions.
	/// </summary>
	Task<bool> CanManageKeysAsync(ClaimsPrincipal user, int gameId);
}