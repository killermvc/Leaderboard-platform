using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;

/// <summary>
/// Repository for managing games in the database.
/// </summary>
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
	/// Updates an existing game in the database.
	/// </summary>
	public async Task UpdateAsync(Game game)
	{
		_context.Games.Update(game);
		await _context.SaveChangesAsync();
	}


	/// <summary>
	/// Retrieves a game by its ID from the database.
	/// </summary>
	/// <param name="id">The ID of the game to retrieve.</param>
	/// <returns>The game with the specified ID, or null if not found.</returns>
	public async Task<Game?> GetGameByIdAsync(int id)
	{
		return await _context.Games.Include(g => g.Owner).FirstOrDefaultAsync(g => g.Id == id);
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

	/// <summary>
	/// Retrieves all games from the database.
	/// </summary>
	/// <returns>A list of all games in the database.</returns>
	public async Task<List<Game>> GetAllGamesAsync()
	{
		return await _context.Games.Include(g => g.Owner).ToListAsync();
	}

	/// <summary>
	/// Retrieves all games associated with a specific player by their ID.
	/// This method queries the Scores table to find all unique games that the player has participated in, based on their scores.
	/// </summary>
	/// <param name="playerId">The ID of the player.</param>
	/// <returns>A list of all games associated with the player.</returns>
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