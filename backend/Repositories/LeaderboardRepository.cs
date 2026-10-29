using Leaderboard.Models;
using LeaderboardEntity = Leaderboard.Models.Leaderboard;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;
#pragma warning disable CS1591

public class LeaderboardRepository(AppDbContext context, IGameModeratorRepository gameModeratorRepository) : ILeaderboardRepository
{
	private readonly AppDbContext _context = context;
	private readonly IGameModeratorRepository _gameModeratorRepository = gameModeratorRepository;

	public async Task<IReadOnlyList<LeaderboardEntity>> GetByGameIdAsync(
		int gameId, CancellationToken cancellationToken = default) =>
		await _context.Leaderboards
			.Where(board => board.GameId == gameId)
			.OrderBy(board => board.Id)
			.ToListAsync(cancellationToken);

	public Task<LeaderboardEntity?> GetByIdAsync(
		int gameId, int leaderboardId, CancellationToken cancellationToken = default) =>
		_context.Leaderboards.FirstOrDefaultAsync(
			board => board.GameId == gameId && board.Id == leaderboardId, cancellationToken);

	public Task<LeaderboardEntity?> GetByIdAsync(
		int leaderboardId, CancellationToken cancellationToken = default) =>
		_context.Leaderboards.FirstOrDefaultAsync(
			board => board.Id == leaderboardId, cancellationToken);

	public async Task<LeaderboardEntity> CreateAsync(
		int gameId, string name, string? description, CancellationToken cancellationToken = default)
	{
		var trimmedName = NormalizeName(name);
		if (!await _context.Games.AnyAsync(game => game.Id == gameId, cancellationToken))
		{
			throw new KeyNotFoundException("Game not found.");
		}
		if (await HasNameAsync(gameId, trimmedName, null, cancellationToken))
		{
			throw new InvalidOperationException("A leaderboard with this name already exists for the game.");
		}

		var leaderboard = new LeaderboardEntity
		{
			GameId = gameId,
			Name = trimmedName,
			Description = description,
			IsActive = true
		};
		_context.Leaderboards.Add(leaderboard);
		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (IsConstraintConflict(exception))
		{
			throw new InvalidOperationException(
				"A leaderboard with this name already exists for the game.", exception);
		}
		return leaderboard;
	}

	public async Task<LeaderboardEntity?> UpdateAsync(
		int gameId, int leaderboardId, string name, string? description, bool isActive,
		CancellationToken cancellationToken = default)
	{
		var leaderboard = await GetByIdAsync(gameId, leaderboardId, cancellationToken);
		if (leaderboard is null)
		{
			return null;
		}

		var trimmedName = NormalizeName(name);
		if (await HasNameAsync(gameId, trimmedName, leaderboardId, cancellationToken))
		{
			throw new InvalidOperationException("A leaderboard with this name already exists for the game.");
		}

		leaderboard.Name = trimmedName;
		leaderboard.Description = description;
		leaderboard.IsActive = isActive;
		leaderboard.UpdatedAt = DateTime.UtcNow;
		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (IsConstraintConflict(exception))
		{
			throw new InvalidOperationException(
				"A leaderboard with this name already exists for the game.", exception);
		}
		return leaderboard;
	}

	public async Task<LeaderboardDeleteResult> DeleteAsync(
		int gameId, int leaderboardId, CancellationToken cancellationToken = default)
	{
		var leaderboard = await GetByIdAsync(gameId, leaderboardId, cancellationToken);
		if (leaderboard is null)
		{
			return LeaderboardDeleteResult.NotFound;
		}

		if (await _context.Scores.AnyAsync(score => score.LeaderboardId == leaderboardId, cancellationToken))
		{
			return LeaderboardDeleteResult.HasScores;
		}

		_context.Leaderboards.Remove(leaderboard);
		try
		{
			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException exception) when (IsConstraintConflict(exception))
		{
			return LeaderboardDeleteResult.HasScores;
		}
		return LeaderboardDeleteResult.Deleted;
	}

	public async Task<bool> CanManageAsync(
		int gameId, int userId, CancellationToken cancellationToken = default)
	{
		var isAdmin = await _context.UserRoles
			.AnyAsync(userRole => userRole.UserId == userId && userRole.Role.Name == "Admin", cancellationToken);
		return isAdmin || await _gameModeratorRepository.IsModeratorAsync(gameId, userId);
	}

	private Task<bool> HasNameAsync(
		int gameId, string name, int? excludedId, CancellationToken cancellationToken) =>
		_context.Leaderboards.AnyAsync(
			board => board.GameId == gameId && board.Name.ToUpper() == name.ToUpper() &&
				(!excludedId.HasValue || board.Id != excludedId.Value), cancellationToken);

	private static string NormalizeName(string name)
	{
		var trimmedName = name?.Trim() ?? string.Empty;
		if (trimmedName.Length == 0)
		{
			throw new ArgumentException("Leaderboard name is required.", nameof(name));
		}

		return trimmedName;
	}

	private static bool IsConstraintConflict(DbUpdateException exception)
	{
		var message = exception.ToString();
		return message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
			message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
			message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
			message.Contains("constraint", StringComparison.OrdinalIgnoreCase);
	}
}
#pragma warning restore CS1591
