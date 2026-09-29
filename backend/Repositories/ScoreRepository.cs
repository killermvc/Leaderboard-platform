using System.Globalization;
using System.Linq.Expressions;
using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using StackExchange.Redis;

namespace Leaderboard.Repositories;

/// <summary>Provides score submission, moderation, and leaderboard operations.</summary>
/// <param name="context">The application database context.</param>
/// <param name="multiplexer">The Redis connection multiplexer.</param>
public class ScoreRepository(AppDbContext context, ConnectionMultiplexer multiplexer) : IScoreRepository
{

	private readonly AppDbContext _context = context;
	private readonly IDatabase _redisDb = multiplexer.GetDatabase();

	/// <summary>
	/// Redis member prefix for a leaderboard entry owned by a user account.
	/// </summary>
	private const string UserMemberPrefix = "u:";

	/// <summary>
	/// Redis member prefix for a leaderboard entry owned by a name given by a game client,
	/// holding the normalized (trimmed, lowercased) player name.
	/// </summary>
	private const string PlayerNameMemberPrefix = "g:";

	/// <summary>
	/// Versioned leaderboard key, bumped when the member format changes so caches written by an
	/// older build are never read back with the wrong member format. Keys without a version are
	/// stale and can be dropped with: redis-cli --scan --pattern 'leaderboard:[0-9]*' | xargs -r redis-cli del
	/// </summary>
	private const string LeaderboardKeyPrefix = "leaderboard:v2:";

	/// <summary>
	/// Submits a score for a user in a specific game.
	/// The score is saved with a Pending status and must be approved before appearing on leaderboards.
	/// If the game or user does not exist, a KeyNotFoundException is thrown.
	/// If the new score is lower than or equal to the user's existing highest approved score, an InvalidOperationException is thrown.
	/// </summary>
	/// <param name="userId">The ID of the user submitting the score.</param>
	/// <param name="gameId">The ID of the game for which the score is being submitted.</param>
	/// <param name="scoreValue">The score value to be submitted.</param>
	/// <param name="title">Optional title of the score post.</param>
	/// <param name="description">Optional description of the score post.</param>
	/// <exception cref="KeyNotFoundException">Thrown when the specified game or user ID is not found.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the new score is not higher than the existing highest approved score.</exception>
	public async Task SubmitScoreAsync(int userId, int gameId, int scoreValue, string? title = null, string? description = null)
	{
		Game game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId)
			?? throw new KeyNotFoundException($"Game with ID {gameId} not found.");
		User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
			?? throw new KeyNotFoundException($"User with ID {userId} not found");

		// Check if the user already has a higher or equal approved score for this game
		var existingHighScore = await _context.Scores
			.Where(s => s.UserId == userId && s.Game.Id == gameId && s.Status == ScoreStatus.Approved)
			.MaxAsync(s => (int?)s.Value);

		if (existingHighScore.HasValue && scoreValue <= existingHighScore.Value)
		{
			throw new InvalidOperationException($"New score ({scoreValue}) must be higher than current high score ({existingHighScore.Value}).");
		}

		var score = new Score
		{
			Value = scoreValue,
			User = user,
			Game = game,
			Title = title ?? $"{game.Name} - {scoreValue}",
			Description = description,
			Status = ScoreStatus.Pending // Score starts as pending
		};

		_context.Scores.Add(score);
		await _context.SaveChangesAsync();

		// Note: Score is NOT added to Redis leaderboard until approved
	}

	/// <summary>
	/// Submits a score on behalf of a player that only has a name, as game clients do through the api.
	/// The score is not tied to a user account and is approved right away, so it lands on the
	/// leaderboard of the game immediately. The latest submission replaces the player's current
	/// leaderboard value, even when it is lower than the previous submission.
	/// </summary>
	/// <param name="gameId">The ID of the game for which the score is being submitted.</param>
	/// <param name="playerName">The name given to the player by the game client.</param>
	/// <param name="scoreValue">The score value to be submitted.</param>
	/// <param name="submissionId">Client-generated identifier that must be unique for the game.</param>
	/// <param name="title">Optional title of the score post.</param>
	/// <param name="description">Optional description of the score post.</param>
	/// <returns>The created score, approved and with its generated id.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the specified game ID is not found.</exception>
	public async Task<Score> SubmitNamedScoreAsync(int gameId, string playerName, int scoreValue, string submissionId, string? title = null, string? description = null)
	{
		Game game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId)
			?? throw new KeyNotFoundException($"Game with ID {gameId} not found.");

		Score? existingScore = await _context.Scores
			.Include(s => s.Game)
			.FirstOrDefaultAsync(s => s.GameId == gameId && s.SubmissionId == submissionId);
		if (existingScore is not null)
		{
			throw new InvalidOperationException($"Submission ID '{submissionId}' has already been used for this game.");
		}

		string trimmedName = playerName.Trim();

		var score = new Score
		{
			Value = scoreValue,
			Game = game,
			PlayerName = trimmedName,
			SubmissionId = submissionId,
			Title = string.IsNullOrWhiteSpace(title) ? $"{game.Name} - {scoreValue}" : title.Trim(),
			Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
			// Set explicitly because the column defaults to Pending for moderated submissions
			Status = ScoreStatus.Approved,
			DateAchieved = DateTime.UtcNow
		};

		_context.Scores.Add(score);
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			_context.Entry(score).State = EntityState.Detached;
			bool submissionExists = await _context.Scores
				.AnyAsync(s => s.GameId == gameId && s.SubmissionId == submissionId);
			if (!submissionExists)
			{
				throw;
			}

			throw new InvalidOperationException($"Submission ID '{submissionId}' has already been used for this game.");
		}

		// Game client scores are approved on submission, so they go straight to the leaderboard
		var leaderboardKey = GetLeaderboardKey(gameId);
		await EnsureLeaderboardCachedAsync(leaderboardKey, gameId);

		string member = GetPlayerNameMember(trimmedName);
		// ZADD overwrites the member so the latest submission becomes the player's current score.
		await _redisDb.SortedSetAddAsync(leaderboardKey, member, scoreValue);

		return score;
	}

	/// <summary>
	/// Retrieves a score from the SQL database by its ID.
	/// </summary>
	/// <param name="id">The ID of the score to retrieve.</param>
	/// <returns>The score with the specified ID, or null if no such score exists.</returns>

	public async Task<Score?> GetByIdAsync(int id)
	{
		return await _context.Scores
			.Include(s => s.User)
			.Include(s => s.Game)
			.Include(s => s.ReviewedBy)
			.FirstOrDefaultAsync(s => s.Id == id);
	}

	/// <summary>Gets the latest approved score for a named player in a game.</summary>
	/// <param name="gameId">The ID of the game.</param>
	/// <param name="playerName">The player's name.</param>
	/// <returns>The latest approved score, or null when none exists.</returns>
	public async Task<Score?> GetBestNamedScoreByGameAsync(int gameId, string playerName)
	{
		string normalizedName = NormalizePlayerName(playerName);

		return await _context.Scores
			.AsNoTracking()
			.Include(s => s.Game)
			.Where(s => s.Game.Id == gameId
				&& s.Status == ScoreStatus.Approved
				&& s.PlayerName != null
				&& s.PlayerName.ToLower() == normalizedName)
			.OrderByDescending(s => s.DateAchieved)
			.ThenByDescending(s => s.Id)
			.FirstOrDefaultAsync();
	}

	private static string GetLeaderboardKey(int gameId) => $"{LeaderboardKeyPrefix}{gameId}";

	/// <summary>
	/// Makes sure the cached leaderboard of a game is complete before an entry is written to it.
	/// A score that creates the key would otherwise leave the rest of the game off the board until
	/// the key is dropped.
	/// </summary>
	private async Task EnsureLeaderboardCachedAsync(string leaderboardKey, int gameId)
	{
		if (!await _redisDb.KeyExistsAsync(leaderboardKey))
		{
			await UpdateRedisDbForGame(gameId);
		}
	}

	private static string GetUserMember(int userId) => $"{UserMemberPrefix}{userId}";

	private static string GetPlayerNameMember(string playerName) => $"{PlayerNameMemberPrefix}{NormalizePlayerName(playerName)}";

	/// <summary>
	/// Normalizes a player name so the same player always maps to the same leaderboard member,
	/// no matter how the game client cased or padded it.
	/// </summary>
	private static string NormalizePlayerName(string playerName) => playerName.Trim().ToLowerInvariant();

	/// <summary>
	/// Splits a redis member into the player it belongs to. Members look like "u:{userId}" for an
	/// account or "g:{normalizedPlayerName}" for a name given by a game client.
	/// </summary>
	private static bool TryParseMember(string? member, out int? userId, out string? playerName)
	{
		userId = null;
		playerName = null;

		if (string.IsNullOrWhiteSpace(member))
		{
			return false;
		}

		if (member.StartsWith(UserMemberPrefix, StringComparison.Ordinal)
			&& int.TryParse(member.AsSpan(UserMemberPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedUserId))
		{
			userId = parsedUserId;
			return true;
		}

		if (member.StartsWith(PlayerNameMemberPrefix, StringComparison.Ordinal) && member.Length > PlayerNameMemberPrefix.Length)
		{
			playerName = member[PlayerNameMemberPrefix.Length..];
			return true;
		}

		return false;
	}

	/// <summary>
	/// Rebuilds the leaderboard for a given game in Redis from the SQL database.
	/// Only includes approved scores, keeping the best one per user account and the latest one per
	/// name given by a game client.
	/// </summary>
	/// <param name="gameId">The ID of the game.</param>
	private async Task UpdateRedisDbForGame(int gameId)
	{
		var approvedScores = await _context.Scores
			.AsNoTracking()
			.Where(s => s.Game.Id == gameId && s.Status == ScoreStatus.Approved)
			.Select(s => new { s.Id, s.UserId, s.PlayerName, s.Value, s.DateAchieved })
			.ToListAsync();

		var bestPerPlayer = new Dictionary<string, int>(StringComparer.Ordinal);
		var latestNamedScore = new Dictionary<string, (DateTime DateAchieved, int Id)>(StringComparer.Ordinal);
		foreach (var approvedScore in approvedScores)
		{
			string? member = approvedScore.UserId.HasValue
				? GetUserMember(approvedScore.UserId.Value)
				: string.IsNullOrWhiteSpace(approvedScore.PlayerName)
					? null
					: GetPlayerNameMember(approvedScore.PlayerName);

			// A score with neither an account nor a name cannot be ranked
			if (member is null)
			{
				continue;
			}

			if (approvedScore.UserId.HasValue)
			{
				if (!bestPerPlayer.TryGetValue(member, out int currentBest) || approvedScore.Value > currentBest)
				{
					bestPerPlayer[member] = approvedScore.Value;
				}
			}
			else if (!latestNamedScore.TryGetValue(member, out var currentLatest)
				|| approvedScore.DateAchieved > currentLatest.DateAchieved
				|| approvedScore.DateAchieved == currentLatest.DateAchieved && approvedScore.Id > currentLatest.Id)
			{
				bestPerPlayer[member] = approvedScore.Value;
				latestNamedScore[member] = (approvedScore.DateAchieved, approvedScore.Id);
			}
		}

		if (bestPerPlayer.Count == 0)
		{
			throw new KeyNotFoundException($"No approved scores found for game ID {gameId}");
		}

		// Replace the key so entries that no longer have an approved score disappear
		await _redisDb.KeyDeleteAsync(GetLeaderboardKey(gameId));

		// Cache the leaderboard in Redis
		var entries = bestPerPlayer.Select(entry => new SortedSetEntry(entry.Key, entry.Value)).ToArray();
		await _redisDb.SortedSetAddAsync(GetLeaderboardKey(gameId), entries);
	}

	/// <summary>
	/// Retrieves the top N players from the leaderboard for a given game.
	/// Only approved scores are included.
	/// If the leaderboard doesn't exist or is empty in redis, it is cached from the sql database.
	/// </summary>
	/// <param name="gameId">The ID of the game.</param>
	/// <param name="limit">The number of top players to retrieve.</param>
	/// <returns>A list of leaderboard entries.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when no approved scores are found for the specified game ID.</exception>
	public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int gameId, int limit)
	{
		var leaderboardKey = GetLeaderboardKey(gameId);

		// If the leaderboard doesn't exist or is empty, update it
		if (
			!await _redisDb.KeyExistsAsync(leaderboardKey)
			|| await _redisDb.SortedSetLengthAsync(leaderboardKey) == 0)
		{
			try
			{
				await UpdateRedisDbForGame(gameId);
			}
			catch (KeyNotFoundException)
			{
				throw new KeyNotFoundException($"No approved scores found for game ID {gameId}");
			}
		}

		// Fetch the top players from Redis
		var leaderboardEntries = await _redisDb.SortedSetRangeByRankWithScoresAsync(leaderboardKey, 0, limit - 1, order: Order.Descending);

		return await MapRedisEntriesAsync(gameId, leaderboardEntries);
	}

	/// <summary>
	/// Maps redis leaderboard members to leaderboard entries, resolving the display name of every
	/// player from the SQL database. A single query per kind of player keeps this off the hot path
	/// of a per entry lookup.
	/// </summary>
	private async Task<List<LeaderboardEntry>> MapRedisEntriesAsync(int gameId, IEnumerable<SortedSetEntry> redisEntries)
	{
		var parsedEntries = new List<(SortedSetEntry Entry, int? UserId, string? PlayerName)>();
		foreach (var entry in redisEntries)
		{
			if (TryParseMember(entry.Element, out int? userId, out string? playerName))
			{
				parsedEntries.Add((entry, userId, playerName));
			}
		}

		var userIds = parsedEntries.Where(e => e.UserId.HasValue).Select(e => e.UserId!.Value).Distinct().ToList();
		var playerNames = parsedEntries.Where(e => e.PlayerName is not null).Select(e => e.PlayerName!).Distinct(StringComparer.Ordinal).ToList();
		var usernames = await GetUsernamesAsync(userIds);
		var playerNameDisplayNames = await GetPlayerNameDisplayNamesAsync(gameId, playerNames);

		var leaderboard = new List<LeaderboardEntry>();
		foreach (var (entry, userId, playerName) in parsedEntries)
		{
			leaderboard.Add(new LeaderboardEntry
			{
				UserId = userId,
				// Without a display name the normalized name is still a truthful label for the entry
				UserName = userId.HasValue
					? usernames.GetValueOrDefault(userId.Value)
					: playerNameDisplayNames.GetValueOrDefault(playerName!) ?? playerName,
				Score = (int)entry.Score
			});
		}

		return leaderboard;
	}

	/// <summary>
	/// Builds a predicate matching a column against any of the given values, so the players of one
	/// leaderboard page can be resolved with a single query.
	/// A disjunction of comparisons is used because Contains over a collection is not translated by
	/// the mysql provider of this project. The values must not be empty.
	/// </summary>
	private static Expression<Func<TEntity, bool>> IsAnyOf<TEntity, TValue>(
		Expression<Func<TEntity, bool>> condition,
		IReadOnlyCollection<TValue> values,
		Expression<Func<TEntity, TValue>> column)
	{
		ParameterExpression parameter = condition.Parameters[0];
		Expression columnBody = new ParameterReplacer(column.Parameters[0], parameter).Visit(column.Body)!;

		Expression? matchesAnyValue = null;
		foreach (TValue value in values)
		{
			Expression isValue = Expression.Equal(columnBody, Expression.Constant(value, column.Body.Type));
			matchesAnyValue = matchesAnyValue is null ? isValue : Expression.OrElse(matchesAnyValue, isValue);
		}

		return Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(condition.Body, matchesAnyValue!), condition.Parameters);
	}

	/// <inheritdoc cref="IsAnyOf{TEntity, TValue}(Expression{Func{TEntity, bool}}, IReadOnlyCollection{TValue}, Expression{Func{TEntity, TValue}})"/>
	private static Expression<Func<TEntity, bool>> IsAnyOf<TEntity, TValue>(IReadOnlyCollection<TValue> values, Expression<Func<TEntity, TValue>> column)
		=> IsAnyOf(_ => true, values, column);

	/// <summary>
	/// Rebinds the parameter of one expression to the parameter of another, so a column expression
	/// can be embedded in a bigger predicate.
	/// </summary>
	private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
	}

	/// <summary>
	/// Looks up the usernames of the given user ids in a single query.
	/// </summary>
	private async Task<Dictionary<int, string>> GetUsernamesAsync(List<int> userIds)
	{
		if (userIds.Count == 0)
		{
			return [];
		}

		var users = await _context.Users
			.AsNoTracking()
			.Where(IsAnyOf<User, int>(userIds, u => u.Id))
			.Select(u => new { u.Id, u.Username })
			.ToListAsync();

		return users.ToDictionary(u => u.Id, u => u.Username);
	}

	/// <summary>
	/// Looks up the display name to show for the normalized player names of a leaderboard page in a
	/// single query, keeping the name behind the player's best approved score.
	/// </summary>
	private async Task<Dictionary<string, string>> GetPlayerNameDisplayNamesAsync(int gameId, List<string> playerNames)
	{
		if (playerNames.Count == 0)
		{
			return [];
		}

		var bestScores = await _context.Scores
			.AsNoTracking()
			.Where(IsAnyOf<Score, string?>(
				s => s.Game.Id == gameId && s.Status == ScoreStatus.Approved && s.PlayerName != null,
				playerNames,
				s => s.PlayerName!.ToLower()))
			.OrderByDescending(s => s.Value)
			.Select(s => new { s.PlayerName })
			.ToListAsync();

		var wanted = new HashSet<string>(playerNames, StringComparer.Ordinal);
		var displayNames = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var bestScore in bestScores)
		{
			if (bestScore.PlayerName is null)
			{
				continue;
			}

			// The scores are ordered by value, so the first name found for a player is their best one
			string normalized = NormalizePlayerName(bestScore.PlayerName);
			if (wanted.Remove(normalized))
			{
				displayNames[normalized] = bestScore.PlayerName;
			}
		}

		return displayNames;
	}

	/// <summary>
	/// Retrieves the rank of a specific user in the leaderboard for a given game.
	/// Only considers approved scores.
	/// If the leaderboard is not available in Redis, it is cached from the sql database first.
	/// If the game has no approved scores, a KeyNotFoundException is thrown.
	/// </summary>
	/// <param name="gameId">The ID of the game.</param>
	/// <param name="userId">The ID of the user whose rank is being retrieved.</param>
	/// <returns>The rank of the user in the leaderboard, or null if the user is not ranked.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the specified game has no approved scores.</exception>
	public async Task<long?> GetRankAsync(int gameId, int userId)
	{
		var leaderboardKey = GetLeaderboardKey(gameId);

		// If the leaderboard doesn't exist in Redis, cache the whole game, a single user is not a leaderboard
		if (!await _redisDb.KeyExistsAsync(leaderboardKey))
		{
			await UpdateRedisDbForGame(gameId);
		}

		// Fetch the rank of the user from Redis
		var rank = await _redisDb.SortedSetRankAsync(leaderboardKey, GetUserMember(userId));

		return rank.HasValue ? rank + 1 : null;
	}


	/// <summary>
	/// Retrieves the top N players across all games based on their top approved score submitted
	/// between the given start and end dates.
	/// </summary>
	/// <param name="start_date">The start date of the time period for which to retrieve top players.</param>
	/// <param name="end_date">The end date of the time period for which to retrieve top players.</param>
	/// <param name="limit">The number of top players to retrieve.</param>
	/// <returns>A list of leaderboard entries containing the user ID, username, and score of each player.</returns>
	public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(DateTime start_date, DateTime end_date, int limit)
	{
		var topPlayers = await _context.Scores
			.Where(s => s.DateAchieved >= start_date && s.DateAchieved <= end_date && s.Status == ScoreStatus.Approved)
			.OrderByDescending(s => s.Value)
			.Take(limit)
			.Select(s => new LeaderboardEntry
			{
				UserId = s.UserId,
				// Scores without an account are displayed with the name the game client gave them
				UserName = s.User!.Username ?? s.PlayerName,
				Score = s.Value
			})
			.ToListAsync();

		return topPlayers;
	}

	/// <summary>Gets approved scores submitted by a user.</summary>
	/// <param name="userId">The user's ID.</param>
	/// <param name="limit">The maximum number of scores to return.</param>
	/// <param name="offset">The number of scores to skip.</param>
	public Task<List<Score>> GetScoresByUserAsync(int userId, int limit, int offset)
	{
		return _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Where(s => s.UserId == userId && s.Status == ScoreStatus.Approved)
			.OrderByDescending(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}

	/// <summary>
	/// Gets all scores by a user, including pending and rejected ones.
	/// For the user to see their own submissions.
	/// </summary>
	public Task<List<Score>> GetAllScoresByUserAsync(int userId, int limit, int offset)
	{
		return _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Include(s => s.ReviewedBy)
			.Where(s => s.UserId == userId)
			.OrderByDescending(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}

	/// <summary>Gets recent approved scores.</summary>
	/// <param name="limit">The maximum number of scores to return.</param>
	/// <param name="offset">The number of scores to skip.</param>
	public Task<List<Score>> GetRecentScoresAsync(int limit, int offset)
	{
		return _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Where(s => s.Status == ScoreStatus.Approved)
			.OrderByDescending(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}

	/// <summary>
	/// Gets the top approved score for a user in a specific game.
	/// </summary>
	public async Task<Score?> GetTopScoreByUserAndGameAsync(int gameId, int userId)
	{
		return await _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Where(s => s.Game.Id == gameId && s.UserId == userId && s.Status == ScoreStatus.Approved)
			.OrderByDescending(s => s.Value)
			.FirstOrDefaultAsync();
	}

	/// <summary>
	/// Gets all score submissions (posts) visible to everyone, regardless of status.
	/// </summary>
	public Task<List<Score>> GetAllSubmissionsAsync(int limit, int offset)
	{
		return _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Include(s => s.ReviewedBy)
			.OrderByDescending(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}

	// ==================== Score Approval Methods ====================

	/// <summary>
	/// Approves a pending score and adds it to the Redis leaderboard.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the score is not pending or has no user account.</exception>
	public async Task ApproveScoreAsync(int scoreId, int moderatorId)
	{
		var score = await _context.Scores
			.Include(s => s.User)
			.Include(s => s.Game)
			.FirstOrDefaultAsync(s => s.Id == scoreId)
			?? throw new KeyNotFoundException($"Score with ID {scoreId} not found.");

		if (score.Status != ScoreStatus.Pending)
		{
			throw new InvalidOperationException($"Score is not pending. Current status: {score.Status}");
		}

		// Only scores of a user account go through moderation, api submissions are approved on creation
		if (!score.UserId.HasValue)
		{
			throw new InvalidOperationException("Score is not associated with a user and cannot be moderated.");
		}

		var moderator = await _context.Users.FirstOrDefaultAsync(u => u.Id == moderatorId)
			?? throw new KeyNotFoundException($"Moderator with ID {moderatorId} not found.");

		score.Status = ScoreStatus.Approved;
		score.ReviewedBy = moderator;
		score.ReviewedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		// Add the approved score to Redis leaderboard
		var leaderboardKey = GetLeaderboardKey(score.Game.Id);
		await EnsureLeaderboardCachedAsync(leaderboardKey, score.Game.Id);

		// Get the user's current highest approved score for this game
		var highestScore = await _context.Scores
			.Where(s => s.UserId == score.UserId && s.Game.Id == score.Game.Id && s.Status == ScoreStatus.Approved)
			.MaxAsync(s => (int?)s.Value) ?? 0;

		// Update Redis with the highest score
		await _redisDb.SortedSetAddAsync(leaderboardKey, GetUserMember(score.UserId.Value), highestScore);
	}

	/// <summary>
	/// Rejects a pending score with an optional reason.
	/// </summary>
	public async Task RejectScoreAsync(int scoreId, int moderatorId, string? reason = null)
	{
		var score = await _context.Scores
			.Include(s => s.User)
			.Include(s => s.Game)
			.FirstOrDefaultAsync(s => s.Id == scoreId)
			?? throw new KeyNotFoundException($"Score with ID {scoreId} not found.");

		if (score.Status != ScoreStatus.Pending)
		{
			throw new InvalidOperationException($"Score is not pending. Current status: {score.Status}");
		}

		var moderator = await _context.Users.FirstOrDefaultAsync(u => u.Id == moderatorId)
			?? throw new KeyNotFoundException($"Moderator with ID {moderatorId} not found.");

		score.Status = ScoreStatus.Rejected;
		score.ReviewedBy = moderator;
		score.ReviewedAt = DateTime.UtcNow;
		score.RejectionReason = reason;

		// Explicitly mark RejectionReason as modified to ensure EF tracks it
		_context.Entry(score).Property(s => s.RejectionReason).IsModified = true;

		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Gets all pending scores for a specific game.
	/// </summary>
	public async Task<List<Score>> GetPendingScoresForGameAsync(int gameId, int limit, int offset)
	{
		return await _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Where(s => s.Game.Id == gameId && s.Status == ScoreStatus.Pending)
			.OrderBy(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}

	/// <summary>
	/// Gets all pending scores across all games.
	/// </summary>
	public async Task<List<Score>> GetAllPendingScoresAsync(int limit, int offset)
	{
		return await _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Where(s => s.Status == ScoreStatus.Pending)
			.OrderBy(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}

	/// <summary>
	/// Gets pending scores for games that have no specific moderators.
	/// </summary>
	public async Task<List<Score>> GetPendingScoresForUnmoderatedGamesAsync(int limit, int offset)
	{
		// Use a subquery to find games without moderators - this translates to SQL properly
		return await _context.Scores
			.AsNoTracking()
			.Include(s => s.User)
			.Include(s => s.Game)
			.Where(s => s.Status == ScoreStatus.Pending &&
				!_context.GameModerators.Any(gm => gm.GameId == s.Game.Id))
			.OrderBy(s => s.DateAchieved)
			.Skip(offset)
			.Take(limit)
			.ToListAsync();
	}
}

/// <summary>Represents a player and score on a leaderboard.</summary>
public class LeaderboardEntry
{
	/// <summary>
	/// The account of the player, null when the player is only known by the name a game client gave.
	/// </summary>
	/// <summary>The account ID, or null for a name-only player.</summary>
	public int? UserId { get; set; }
	/// <summary>The player's display name.</summary>
	public string? UserName { get; set; }
	/// <summary>The player's score.</summary>
	public int Score { get; set; }
}
