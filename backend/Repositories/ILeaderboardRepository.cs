using Leaderboard.Models;
using LeaderboardEntity = Leaderboard.Models.Leaderboard;

namespace Leaderboard.Repositories;
#pragma warning disable CS1591

public enum LeaderboardDeleteResult
{
	NotFound,
	HasScores,
	Deleted
}
public interface ILeaderboardRepository
{
	Task<IReadOnlyList<LeaderboardEntity>> GetByGameIdAsync(int gameId, CancellationToken cancellationToken = default);
	Task<LeaderboardEntity?> GetByIdAsync(int gameId, int leaderboardId, CancellationToken cancellationToken = default);
	Task<LeaderboardEntity?> GetByIdAsync(int leaderboardId, CancellationToken cancellationToken = default);
	Task<LeaderboardEntity> CreateAsync(int gameId, string name, string? description, CancellationToken cancellationToken = default);
	Task<LeaderboardEntity?> UpdateAsync(int gameId, int leaderboardId, string name, string? description, bool isActive, CancellationToken cancellationToken = default);
	Task<LeaderboardDeleteResult> DeleteAsync(int gameId, int leaderboardId, CancellationToken cancellationToken = default);
	Task<bool> CanManageAsync(int gameId, int userId, CancellationToken cancellationToken = default);
}
#pragma warning restore CS1591
