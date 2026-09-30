using System.Security.Claims;

using Leaderboard.Repositories;

namespace Leaderboard.Services;

/// <summary>
/// Service responsible for authorizing API key actions based on user roles and game moderation status.
/// </summary>
public class ApiKeyAuthorizationService(IGameModeratorRepository gameModeratorRepository) : IApiKeyAuthorizationService
{
	private readonly IGameModeratorRepository _gameModeratorRepository = gameModeratorRepository;
	/// <summary>
	/// Determines if the given user can manage API keys for the specified game.
	/// </summary>
	/// <param name="user">The user to check.</param>
	/// <param name="gameId">The ID of the game for which to check API key management permissions.</param>
	public async Task<bool> CanManageKeysAsync(ClaimsPrincipal user, int gameId)
	{
		if (user.IsInRole("Admin"))
		{
			return true;
		}

		if (!int.TryParse(user.FindFirst(ClaimTypes.Name)?.Value, out int userId))
		{
			return false;
		}

		return await _gameModeratorRepository.CanManageGameAsync(gameId, userId);
	}
}