using System.Security.Claims;

using Leaderboard.Repositories;

namespace Leaderboard.Services;

public class ApiKeyAuthorizationService(IGameModeratorRepository gameModeratorRepository) : IApiKeyAuthorizationService
{
	private readonly IGameModeratorRepository _gameModeratorRepository = gameModeratorRepository;

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

		return await _gameModeratorRepository.CanModerateGameAsync(gameId, userId);
	}
}