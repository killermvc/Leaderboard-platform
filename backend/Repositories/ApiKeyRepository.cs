using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;

/// <summary>
/// Repository for managing API keys in the database.
/// </summary>
public class ApiKeyRepository(AppDbContext context) : IApiKeyRepository
{
	private readonly AppDbContext _context = context;

	/// <summary>
	/// Adds a new api key to the database.
	/// </summary>
	public async Task AddAsync(ApiKey apiKey)
	{
		await _context.ApiKeys.AddAsync(apiKey);
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Retrieves all api keys for a specific game.
	/// </summary>
	public async Task<List<ApiKey>> GetKeysForGameAsync(int gameId)
	{
		return await _context.ApiKeys
			.AsNoTracking()
			.Where(k => k.GameId == gameId)
			.OrderByDescending(k => k.CreatedAt)
			.ToListAsync();
	}

	/// <summary>
	/// Retrieves an api key by its ID.
	/// </summary>
	public async Task<ApiKey?> GetByIdAsync(int id)
	{
		return await _context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id);
	}

	/// <summary>
	/// Retrieves an api key by the hash of its secret value.
	/// </summary>
	public async Task<ApiKey?> GetByHashAsync(string keyHash)
	{
		return await _context.ApiKeys
			.AsNoTracking()
			.FirstOrDefaultAsync(k => k.KeyHash == keyHash);
	}

	/// <summary>
	/// Checks whether another api key in the game already uses the given name.
	/// </summary>
	public async Task<bool> HasKeyWithNameAsync(int gameId, string name, int excludeKeyId)
	{
		return await _context.ApiKeys
			.AnyAsync(k => k.GameId == gameId && k.Name == name && k.Id != excludeKeyId);
	}

	/// <summary>
	/// Revokes an existing api key and creates a new one in a single transaction.
	/// </summary>
	public async Task RegenerateAsync(ApiKey existingKey, ApiKey newKey)
	{
		existingKey.RevokedAt = DateTime.UtcNow;
		_context.ApiKeys.Update(existingKey);
		await _context.ApiKeys.AddAsync(newKey);
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Updates an existing api key in the database.
	/// </summary>
	public async Task UpdateAsync(ApiKey apiKey)
	{
		_context.ApiKeys.Update(apiKey);
		await _context.SaveChangesAsync();
	}
}