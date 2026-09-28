using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;

public class GameRepository(AppDbContext context) : IGameRepository
{
	private AppDbContext _context = context;

	/// <summary>
	/// Adds a new game to the database.
	/// </summary>
	/// <param name="game">The game to add.</param>
	public async Task AddAsync(Game game)
	{
		await _context.Games.AddAsync(game);
		await _context.SaveChangesAsync();
	}


	/// <summary>
	/// Retrieves a game by its ID from the database.
	/// </summary>
	/// <param name="id">The ID of the game to retrieve.</param>
	/// <returns>The game with the specified ID, or null if not found.</returns>
	public async Task<Game?> GetGameByIdAsync(int id)
	{
		return await _context.Games.FirstOrDefaultAsync(g => g.Id == id);
	}

	/// <summary>
	/// Retrieves a game by its name from the database.
	/// </summary>
	/// <param name="name">The name of the game to retrieve.</param>
	/// <returns>The game with the specified name, or null if not found.</returns>
	public async Task<Game?> GetGameByNameAsync(string name)
	{
		return await _context.Games.FirstOrDefaultAsync(g => g.Name == name);
	}

	public async Task<List<Game>> GetAllGamesAsync()
	{
		return await _context.Games.ToListAsync();
	}

	public async Task<List<Game>> GetGamesByPlayerIdAsync(int playerId)
	{
		var games = await _context.Scores
			.Where(s => s.UserId == playerId)
			.Select(s => s.Game)
			.Distinct()
			.ToListAsync();
		return games;
	}
}