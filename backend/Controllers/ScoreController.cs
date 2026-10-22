using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Dtos;
using Leaderboard.Services;
using Leaderboard.Exceptions;
using Leaderboard.Errors;

namespace Leaderboard.Controllers;

/// <summary>
/// Controller for managing score submissions, leaderboards, and user scores in the leaderboard system.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ScoreController(
		IScoreRepository scoreRepository,
		IGameRepository gameRepository,
		IUserRepository userRepository,
		IClerkDirectoryService clerkDirectoryService,
		ILogger<ScoreController> logger
	) : ControllerBase
{

	private readonly IScoreRepository _scoreRepository = scoreRepository;
	private readonly IGameRepository _gameRepository = gameRepository;
	private readonly IUserRepository _userRepository = userRepository;
	private readonly IClerkDirectoryService _clerkDirectory = clerkDirectoryService;
	private readonly ILogger _logger = logger;

	/// <summary>
	/// Submits a score for a specific game. Accessible by any authenticated user.
	/// </summary>
	//POST /scores
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> SubmitScore([FromBody] ScoreRequest request)
    {
        if (!ModelState.IsValid)
        {
            throw new ValidationException(ErrorCodes.ValidationFailed, "Invalid request data.", ModelState);
        }

        var userIdClaim = User.Identity?.Name;

		if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedException(ErrorCodes.InvalidToken, "Invalid user token.");
        }

        var game = await _gameRepository.GetGameByIdAsync(request.GameId);
        if (game is null)
        {
            throw new NotFoundException(ErrorCodes.GameNotFound, "Game not found.");
        }
        if (!game.SubmitsAllowed)
        {
            throw new ValidationException(ErrorCodes.SubmissionsNotAllowed, "Submissions are not allowed for this game.");
        }

        // Submit the score through the repository
        await _scoreRepository.SubmitScoreAsync(userId, request.GameId, request.Score, request.Title, request.Description);
        return Ok("Score submitted successfully.");
    }

	/// <summary>
	/// Gets the leaderboard for a specific game, limited to a certain number of top players. Accessible by any authenticated user.
	/// </summary>
	//GET /leaderboard/<game_id>
    [HttpGet]
    [Route("leaderboard/{gameId}")]
    public async Task<IActionResult> GetLeaderboard(int gameId, [FromQuery] int limit = 10)
    {
        try
        {
            var leaderboard = await _scoreRepository.GetLeaderboardAsync(gameId, limit);
			await EnrichLeaderboardAsync(leaderboard);
			return Ok(leaderboard);
        }
        catch (KeyNotFoundException ex)
        {
            throw new NotFoundException(ErrorCodes.GameNotFound, ex.Message);
        }
    }

	/// <summary>
	/// Gets the rank of a specific user in a specific game's leaderboard. Accessible by any authenticated user.
	/// </summary>
	//GET /leaderboard/<game_id>/rank/<user_id>
	[HttpGet]
	[Route("leaderboard/{gameId}/rank/{userId}")]
	public async Task<IActionResult> GetRank(int gameId, int userId)
	{
		var rank = await _scoreRepository.GetRankAsync(gameId, userId);
		if(rank == null)
		{
			throw new NotFoundException(ErrorCodes.ScoreNotFound, "User not found in leaderboard.");
		}
		return Ok(new
        {
            gameId,
            userId,
            rank
        });
	}

	/// <summary>
	/// Gets the top N players for a specific game. Accessible by any authenticated user.
	/// </summary>
	[HttpGet]
	[Route("leaderboard/{gameId}/top/{limit}")]
	public async Task<IActionResult> GetTopPlayers(int gameId, int limit)
	{
		var leaderboard = await _scoreRepository.GetLeaderboardAsync(gameId, limit);
		await EnrichLeaderboardAsync(leaderboard);
		return Ok(new
        {
            gameId,
			limit,
            leaderboard
        });
	}

	/// <summary>
	/// Gets a paginated list of scores submitted by a specific user. Accessible by any authenticated user.
	/// </summary>
	[HttpGet]
	[Route("scores/user/{userId}")]
	public async Task<IActionResult> GetScoresByUser(int userId, [FromQuery] int limit = 10, [FromQuery] int offset = 0)
	{
		var scores = await _scoreRepository.GetScoresByUserAsync(userId, limit, offset);
		var displayNames = await GetDisplayNamesAsync(scores.SelectMany(s => new[] { s.User?.ClerkUserId }));
		var dtos = scores.Select(s => new ScoreDto
		{
			Id = s.Id,
			User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = DisplayOrId(s.User?.ClerkUserId, displayNames) },
			PlayerName = s.PlayerName,
			Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
			Value = s.Value,
			DateAchieved = s.DateAchieved,
			Title = s.Title,
			Description = s.Description
		}).ToList();

		return Ok(dtos);
	}

	/// <summary>
	/// Gets a paginated list of recent score submissions (posts) visible to everyone, regardless of status.
	/// </summary>
	[HttpGet]
	[Route("scores/recent")]
	public async Task<IActionResult> GetRecentScores([FromQuery] int limit = 10, [FromQuery] int offset = 0)
	{
		var scores = await _scoreRepository.GetRecentScoresAsync(limit, offset);
		var displayNames = await GetDisplayNamesAsync(scores.SelectMany(s => new[] { s.User?.ClerkUserId, s.ReviewedBy?.ClerkUserId }));
		var dtos = scores.Select(s => new ScoreDto
		{
			Id = s.Id,
			User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = DisplayOrId(s.User?.ClerkUserId, displayNames) },
			PlayerName = s.PlayerName,
			Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
			Value = s.Value,
			DateAchieved = s.DateAchieved,
			Title = s.Title,
			Description = s.Description,
			Status = s.Status,
			ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = DisplayOrId(s.ReviewedBy.ClerkUserId, displayNames) },
			ReviewedAt = s.ReviewedAt,
			RejectionReason = s.RejectionReason
		}).ToList();

		return Ok(dtos);
	}

	/// <summary>
	/// Gets a single score submission (post) by its ID. Publicly accessible.
	/// </summary>
	[HttpGet]
	[Route("scores/{scoreId}")]
	public async Task<IActionResult> GetScoreById(int scoreId)
	{
		var s = await _scoreRepository.GetByIdAsync(scoreId);
		if (s == null)
		{
			throw new NotFoundException(ErrorCodes.ScoreNotFound, "Score not found.");
		}

		var displayNames = await GetDisplayNamesAsync(new[] { s.User?.ClerkUserId, s.ReviewedBy?.ClerkUserId });
		var dto = new ScoreDto
		{
			Id = s.Id,
			User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = DisplayOrId(s.User?.ClerkUserId, displayNames) },
			PlayerName = s.PlayerName,
			Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
			Value = s.Value,
			DateAchieved = s.DateAchieved,
			Title = s.Title,
			Description = s.Description,
			Status = s.Status,
			ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = DisplayOrId(s.ReviewedBy.ClerkUserId, displayNames) },
			ReviewedAt = s.ReviewedAt,
			RejectionReason = s.RejectionReason
		};

		return Ok(dto);
	}

	/// <summary>
	/// Looks up the top approved score for a user in a specific game.
	/// Returns the score ID so the client can navigate to the score post.
	/// </summary>
	[HttpGet]
	[Route("scores/lookup")]
	public async Task<IActionResult> LookupScore([FromQuery] int gameId, [FromQuery] int userId)
	{
		var score = await _scoreRepository.GetTopScoreByUserAndGameAsync(gameId, userId);
		if (score == null)
		{
			throw new NotFoundException(ErrorCodes.ScoreNotFound, "No approved score found for this user in this game.");
		}

		return Ok(new { scoreId = score.Id });
	}

	/// <summary>
	/// Gets all score submissions (posts) visible to everyone, regardless of status.
	/// </summary>
	[HttpGet]
	[Route("scores/submissions")]
	public async Task<IActionResult> GetAllSubmissions([FromQuery] int limit = 20, [FromQuery] int offset = 0)
	{
		var scores = await _scoreRepository.GetAllSubmissionsAsync(limit, offset);
		var displayNames = await GetDisplayNamesAsync(scores.SelectMany(s => new[] { s.User?.ClerkUserId, s.ReviewedBy?.ClerkUserId }));
		var dtos = scores.Select(s => new ScoreDto
		{
			Id = s.Id,
			User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = DisplayOrId(s.User?.ClerkUserId, displayNames) },
			PlayerName = s.PlayerName,
			Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
			Value = s.Value,
			DateAchieved = s.DateAchieved,
			Title = s.Title,
			Description = s.Description,
			Status = s.Status,
			ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = DisplayOrId(s.ReviewedBy.ClerkUserId, displayNames) },
			ReviewedAt = s.ReviewedAt,
			RejectionReason = s.RejectionReason
		}).ToList();

		return Ok(dtos);
	}

	/// <summary>
	/// Gets the current user's own score submissions, including pending ones.
	/// </summary>
	[HttpGet]
	[Route("scores/my-submissions")]
	[Authorize]
	public async Task<IActionResult> GetMySubmissions([FromQuery] int limit = 20, [FromQuery] int offset = 0)
	{
		var userIdClaim = User.Identity?.Name;
		if (!int.TryParse(userIdClaim, out int userId))
		{
			throw new UnauthorizedException(ErrorCodes.InvalidToken, "Invalid user token.");
		}

		var scores = await _scoreRepository.GetAllScoresByUserAsync(userId, limit, offset);
		var displayNames = await GetDisplayNamesAsync(scores.SelectMany(s => new[] { s.User?.ClerkUserId, s.ReviewedBy?.ClerkUserId }));
		var dtos = scores.Select(s => new ScoreDto
		{
			Id = s.Id,
			User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = DisplayOrId(s.User?.ClerkUserId, displayNames) },
			PlayerName = s.PlayerName,
			Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
			Value = s.Value,
			DateAchieved = s.DateAchieved,
			Title = s.Title,
			Description = s.Description,
			Status = s.Status,
			ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = DisplayOrId(s.ReviewedBy.ClerkUserId, displayNames) },
			ReviewedAt = s.ReviewedAt,
			RejectionReason = s.RejectionReason
		}).ToList();

		return Ok(dtos);
	}

	private async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
		IEnumerable<string?> clerkUserIds,
		CancellationToken cancellationToken = default)
	{
		var ids = clerkUserIds
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id!)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		if (ids.Length == 0)
		{
			return new Dictionary<string, string>(StringComparer.Ordinal);
		}

		return await _clerkDirectory.GetDisplayNamesAsync(ids, cancellationToken);
	}

	private static string DisplayOrId(string? clerkUserId, IReadOnlyDictionary<string, string> displayNames) =>
		string.IsNullOrWhiteSpace(clerkUserId)
			? string.Empty
			: displayNames.TryGetValue(clerkUserId, out string? displayName) ? displayName : clerkUserId;

	private async Task EnrichLeaderboardAsync(List<LeaderboardEntry> leaderboard, CancellationToken cancellationToken = default)
	{
		var ids = leaderboard
			.Where(entry => entry.UserId.HasValue && !string.IsNullOrWhiteSpace(entry.UserName))
			.Select(entry => entry.UserName!)
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		if (ids.Length == 0)
		{
			return;
		}

		var displayNames = await _clerkDirectory.GetDisplayNamesAsync(ids, cancellationToken);
		foreach (LeaderboardEntry entry in leaderboard)
		{
			if (entry.UserId.HasValue && !string.IsNullOrWhiteSpace(entry.UserName) &&
				displayNames.TryGetValue(entry.UserName!, out string? displayName))
			{
				entry.UserName = displayName;
			}
		}
	}
}

/// <summary>
/// Represents a request to submit a score for a game.
/// </summary>
public class ScoreRequest
{
    /// <summary>
    /// The ID of the game for which the score is being submitted.
    /// </summary>
    public required int GameId { get; set; }
    /// <summary>
    /// The score achieved.
    /// </summary>
    public int Score { get; set; }
    /// <summary>
    /// The title of the score submission.
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// The description of the score submission.
    /// </summary>
    public string? Description { get; set; }
}