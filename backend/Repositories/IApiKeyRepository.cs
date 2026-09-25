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
	/// Retrieves an api key by the hash of its secret value.
	/// </summary>
	Task<ApiKey?> GetByHashAsync(string keyHash);

	/// <summary>
	/// Checks whether another api key in the game already uses the given name.
	/// </summary>
	Task<bool> HasKeyWithNameAsync(int gameId, string name, int excludeKeyId);

	/// <summary>
	/// Revokes an existing api key and creates a new one in a single transaction.
	/// </summary>
	Task RegenerateAsync(ApiKey existingKey, ApiKey newKey);

	/// <summary>
	/// Updates an existing api key in the database.
	/// </summary>
	Task UpdateAsync(ApiKey apiKey);
}