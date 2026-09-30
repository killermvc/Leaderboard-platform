using Leaderboard.Models;

namespace Leaderboard.Repositories;

/// <summary>
/// Defines the contract for a repository that manages game moderators in the database, providing methods for adding, removing, and retrieving moderators for specific games, as well as checking moderator permissions.
/// </summary>
public interface IGameModeratorRepository
{
    /// <summary>
    /// Adds a user as a moderator for a specific game.
    /// </summary>
    Task AddModeratorAsync(int gameId, int userId);

    /// <summary>
    /// Removes a user as a moderator from a specific game.
    /// </summary>
    Task RemoveModeratorAsync(int gameId, int userId);

    /// <summary>
    /// Gets all moderators for a specific game.
    /// </summary>
    Task<List<GameModerator>> GetModeratorsForGameAsync(int gameId);

    /// <summary>
    /// Gets all games that a user is a moderator for.
    /// </summary>
    Task<List<GameModerator>> GetGamesByModeratorAsync(int userId);

    /// <summary>
    /// Checks if a user is a moderator for a specific game.
    /// </summary>
    Task<bool> IsModeratorAsync(int gameId, int userId);

    /// <summary>
    /// Checks if a user is a global moderator (has the "Moderator" role).
    /// </summary>
    Task<bool> IsGlobalModeratorAsync(int userId);

    /// <summary>
    /// Checks if a user can moderate a specific game.
    /// Returns true if the user is a game-specific moderator or a global moderator
    /// (when the game has no specific moderators).
    /// </summary>
    Task<bool> CanModerateGameAsync(int gameId, int userId);

    /// <summary>
    /// Checks whether a user owns or administers a game.
    /// </summary>
    Task<bool> CanManageGameAsync(int gameId, int userId);
}
