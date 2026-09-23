using Leaderboard.Models;

namespace Leaderboard.Repositories;

public interface IApiKeyRepository
{
	/// <summary>
	/// Adds a new api key to the database.
	/// </summary>
	Task AddAsync(ApiKey apiKey);

	/// <summary>
	/// Retrieves all api keys for a specific game.
	/// </summary>
	Task<List<ApiKey>> GetKeysForGameAsync(int gameId);

	/// <summary>
	/// Retrieves an api key by its ID.
	/// </summary>
	Task<ApiKey?> GetByIdAsync(int id);

	/// <summary>
	/// Updates an existing api key in the database.
	/// </summary>
	Task UpdateAsync(ApiKey apiKey);
}