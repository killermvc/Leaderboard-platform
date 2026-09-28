using Leaderboard.Models;

namespace Leaderboard.Repositories;

/// <summary>
/// Defines the contract for a repository that manages games in the database, providing methods for adding, retrieving, and listing games, as well as retrieving games associated with specific players.
/// </summary>
public interface IGameRepository
{
	/// <summary>
	/// Adds a new game to the database.
	/// </summary>
	/// <param name="game">The game to add.</param>
	public Task AddAsync(Game game);
	/// <summary>
	/// Retrieves a game by its ID from the database.
	/// </summary>
	/// <param name="id">The ID of the game to retrieve.</param>
	public Task<Game?> GetGameByIdAsync(int id);
	/// <summary>
	/// Retrieves a game by its name from the database.
	/// </summary>
	/// <param name="name">The name of the game to retrieve.</param>
	public Task<Game?> GetGameByNameAsync(string name);
	/// <summary>
	/// Retrieves all games from the database.
	/// </summary>
	public Task<List<Game>> GetAllGamesAsync();
	/// <summary>
	/// Retrieves all games associated with a specific player by their ID.
	/// This method queries the Scores table to find all unique games that the player has participated in, based on their scores.
	/// </summary>
	/// <param name="playerId">The ID of the player for whom to retrieve games.</param>
	public Task<List<Game>> GetGamesByPlayerIdAsync(int playerId);
}