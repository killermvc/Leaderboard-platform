using Leaderboard.Models;

namespace Leaderboard.Repositories;

public interface IScoreRepository
{
	public Task SubmitScoreAsync(int userId, int gameId, int score, string? title = null, string? description = null);

	/// <summary>
	/// Submits a score for a player that has a name but no user account, as game clients do through the api.
	/// The score is approved right away and written to the leaderboard of the game immediately.
	/// </summary>
	public Task<Score> SubmitNamedScoreAsync(int gameId, string playerName, int score, string submissionId, string? title = null, string? description = null);

	public Task<Score?> GetByIdAsync(int id);
	public Task<Score?> GetBestNamedScoreByGameAsync(int gameId, string playerName);
	public Task<List<LeaderboardEntry>> GetLeaderboardAsync(int game, int limit);
	public Task<long?> GetRankAsync(int game, int user);
	public Task<List<LeaderboardEntry>> GetTopPlayersAsync(DateTime start_date, DateTime end_date, int limit);
	public Task<List<Score>> GetScoresByUserAsync(int userId, int limit, int offset);
	public Task<List<Score>> GetRecentScoresAsync(int limit, int offset);

	/// <summary>
	/// Gets the top approved score for a user in a specific game.
	/// Used to look up the score post from a leaderboard entry.
	/// </summary>
	public Task<Score?> GetTopScoreByUserAndGameAsync(int gameId, int userId);

	/// <summary>
	/// Gets all score submissions (posts) visible to everyone, regardless of status.
	/// </summary>
	public Task<List<Score>> GetAllSubmissionsAsync(int limit, int offset);

	/// <summary>
	/// Gets all scores by a user, including pending and rejected ones.
	/// For the user to see their own submissions.
	/// </summary>
	public Task<List<Score>> GetAllScoresByUserAsync(int userId, int limit, int offset);

	// Score approval methods
	/// <summary>
	/// Approves a pending score. Only approved scores appear on leaderboards.
	/// </summary>
	public Task ApproveScoreAsync(int scoreId, int moderatorId);

	/// <summary>
	/// Rejects a pending score with an optional reason.
	/// </summary>
	public Task RejectScoreAsync(int scoreId, int moderatorId, string? reason = null);

	/// <summary>
	/// Gets all pending scores for a specific game.
	/// </summary>
	public Task<List<Score>> GetPendingScoresForGameAsync(int gameId, int limit, int offset);

	/// <summary>
	/// Gets all pending scores across all games (for global moderators).
	/// </summary>
	public Task<List<Score>> GetAllPendingScoresAsync(int limit, int offset);

	/// <summary>
	/// Gets pending scores for games that have no specific moderators.
	/// </summary>
	public Task<List<Score>> GetPendingScoresForUnmoderatedGamesAsync(int limit, int offset);
}