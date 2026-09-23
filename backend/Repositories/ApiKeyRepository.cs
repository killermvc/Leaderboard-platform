using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;

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
	/// Updates an existing api key in the database.
	/// </summary>
	public async Task UpdateAsync(ApiKey apiKey)
	{
		_context.ApiKeys.Update(apiKey);
		await _context.SaveChangesAsync();
	}
}