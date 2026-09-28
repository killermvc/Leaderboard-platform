using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;

/// <summary>
/// Repository for managing game moderators in the database.
/// </summary>
public class GameModeratorRepository(AppDbContext context) : IGameModeratorRepository
{
    private readonly AppDbContext _context = context;

    /// <summary>
    /// Adds a user as a moderator for a specific game.
    /// </summary>
    public async Task AddModeratorAsync(int gameId, int userId)
    {
        // Check if already a moderator
        var existing = await _context.GameModerators
            .FirstOrDefaultAsync(gm => gm.GameId == gameId && gm.UserId == userId);

        if (existing != null)
        {
            throw new InvalidOperationException("User is already a moderator for this game.");
        }

        var gameModerator = new GameModerator
        {
            GameId = gameId,
            UserId = userId,
            AssignedAt = DateTime.UtcNow
        };

        await _context.GameModerators.AddAsync(gameModerator);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a user as a moderator from a specific game.
    /// </summary>
    public async Task RemoveModeratorAsync(int gameId, int userId)
    {
        var gameModerator = await _context.GameModerators
            .FirstOrDefaultAsync(gm => gm.GameId == gameId && gm.UserId == userId);

        if (gameModerator == null)
        {
            throw new KeyNotFoundException("User is not a moderator for this game.");
        }

        _context.GameModerators.Remove(gameModerator);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all moderators for a specific game.
    /// </summary>
    public async Task<List<GameModerator>> GetModeratorsForGameAsync(int gameId)
    {
        return await _context.GameModerators
            .Include(gm => gm.User)
            .Include(gm => gm.Game)
            .Where(gm => gm.GameId == gameId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all games that a user is a moderator for.
    /// </summary>
    public async Task<List<GameModerator>> GetGamesByModeratorAsync(int userId)
    {
        return await _context.GameModerators
            .Include(gm => gm.Game)
            .Include(gm => gm.User)
            .Where(gm => gm.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a user is a moderator for a specific game.
    /// </summary>
    public async Task<bool> IsModeratorAsync(int gameId, int userId)
    {
        return await _context.GameModerators
            .AnyAsync(gm => gm.GameId == gameId && gm.UserId == userId);
    }

    /// <summary>
    /// Checks if a user is a global moderator (has the "Moderator" role).
    /// </summary>
    public async Task<bool> IsGlobalModeratorAsync(int userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Moderator");
    }

    /// <summary>
    /// Checks if a user can moderate a specific game.
    /// Returns true if the user is a game-specific moderator or a global moderator
    /// (when the game has no specific moderators).
    /// </summary>
    public async Task<bool> CanModerateGameAsync(int gameId, int userId)
    {
        // First, check if the user is a game-specific moderator
        if (await IsModeratorAsync(gameId, userId))
        {
            return true;
        }

        // If the game has no moderators, check if the user is a global moderator
        var hasGameModerators = await _context.GameModerators
            .AnyAsync(gm => gm.GameId == gameId);

        if (!hasGameModerators)
        {
            return await IsGlobalModeratorAsync(userId);
        }

        return false;
    }
}
