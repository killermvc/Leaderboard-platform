using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Dtos;

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
		ILogger<ScoreController> logger
	) : ControllerBase
{

	private readonly IScoreRepository _scoreRepository = scoreRepository;
	private readonly IGameRepository _gameRepository = gameRepository;
	private readonly IUserRepository _userRepository = userRepository;
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
            return BadRequest(ModelState);
        }

        var userIdClaim = User.Identity?.Name;

		if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
			var game = await _gameRepository.GetGameByIdAsync(request.GameId);
			if (game is null)
			{
				return NotFound("Game not found.");
			}
			if (!game.SubmitsAllowed)
			{
				return BadRequest("Submissions are not allowed for this game.");
			}

            // Submit the score through the repository
            await _scoreRepository.SubmitScoreAsync(userId, request.GameId, request.Score, request.Title, request.Description);
            return Ok("Score submitted successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting score.");
            return StatusCode(500, "An error occurred while submitting the score.");
        }
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
			return Ok(leaderboard);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leaderboard.");
            return StatusCode(500, "An error occurred while retrieving the leaderboard.");
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
		try
		{
			var rank = await _scoreRepository.GetRankAsync(gameId, userId);
			if(rank == null)
			{
				return NotFound("User not found in leaderboard.");
			}
			return Ok(new
            {
                gameId,
                userId,
                rank
            });

		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving rank.");
			return StatusCode(500, "An error occurred while retrieving the rank.");
		}
	}

	/// <summary>
	/// Gets the top N players for a specific game. Accessible by any authenticated user.
	/// </summary>
	[HttpGet]
	[Route("leaderboard/{gameId}/top/{limit}")]
	public async Task<IActionResult> GetTopPlayers(int gameId, int limit)
	{
		try
		{
			var leaderboard = await _scoreRepository.GetLeaderboardAsync(gameId, limit);
			return Ok(new
            {
                gameId,
				limit,
                leaderboard
            });
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving leaderboard.");
			return StatusCode(500, "An error occurred while retrieving the leaderboard.");
		}
	}

	/// <summary>
	/// Gets a paginated list of scores submitted by a specific user. Accessible by any authenticated user.
	/// </summary>
	[HttpGet]
	[Route("scores/user/{userId}")]
	public async Task<IActionResult> GetScoresByUser(int userId, [FromQuery] int limit = 10, [FromQuery] int offset = 0)
	{
		try
		{
			var scores = await _scoreRepository.GetScoresByUserAsync(userId, limit, offset);
			var dtos = scores.Select(s => new ScoreDto
			{
				Id = s.Id,
				User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = s.User?.Username ?? string.Empty },
				PlayerName = s.PlayerName,
				Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
				Value = s.Value,
				DateAchieved = s.DateAchieved,
				Title = s.Title,
				Description = s.Description
			}).ToList();

			return Ok(dtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving scores by user.");
			return StatusCode(500, "An error occurred while retrieving the scores.");
		}
	}

	/// <summary>
	/// Gets a paginated list of recent score submissions (posts) visible to everyone, regardless of status.
	/// </summary>
	[HttpGet]
	[Route("scores/recent")]
	public async Task<IActionResult> GetRecentScores([FromQuery] int limit = 10, [FromQuery] int offset = 0)
	{
		try
		{
			var scores = await _scoreRepository.GetRecentScoresAsync(limit, offset);
			var dtos = scores.Select(s => new ScoreDto
			{
				Id = s.Id,
				User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = s.User?.Username ?? string.Empty },
				PlayerName = s.PlayerName,
				Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
				Value = s.Value,
				DateAchieved = s.DateAchieved,
				Title = s.Title,
				Description = s.Description,
				Status = s.Status,
				ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = s.ReviewedBy.Username },
				ReviewedAt = s.ReviewedAt,
				RejectionReason = s.RejectionReason
			}).ToList();

			return Ok(dtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving recent scores.");
			return StatusCode(500, "An error occurred while retrieving the recent scores.");
		}
	}

	/// <summary>
	/// Gets a single score submission (post) by its ID. Publicly accessible.
	/// </summary>
	[HttpGet]
	[Route("scores/{scoreId}")]
	public async Task<IActionResult> GetScoreById(int scoreId)
	{
		try
		{
			var s = await _scoreRepository.GetByIdAsync(scoreId);
			if (s == null)
			{
				return NotFound("Score not found.");
			}

			var dto = new ScoreDto
			{
				Id = s.Id,
				User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = s.User?.Username ?? string.Empty },
				PlayerName = s.PlayerName,
				Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
				Value = s.Value,
				DateAchieved = s.DateAchieved,
				Title = s.Title,
				Description = s.Description,
				Status = s.Status,
				ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = s.ReviewedBy.Username },
				ReviewedAt = s.ReviewedAt,
				RejectionReason = s.RejectionReason
			};

			return Ok(dto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving score.");
			return StatusCode(500, "An error occurred while retrieving the score.");
		}
	}

	/// <summary>
	/// Looks up the top approved score for a user in a specific game.
	/// Returns the score ID so the client can navigate to the score post.
	/// </summary>
	[HttpGet]
	[Route("scores/lookup")]
	public async Task<IActionResult> LookupScore([FromQuery] int gameId, [FromQuery] int userId)
	{
		try
		{
			var score = await _scoreRepository.GetTopScoreByUserAndGameAsync(gameId, userId);
			if (score == null)
			{
				return NotFound("No approved score found for this user in this game.");
			}

			return Ok(new { scoreId = score.Id });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error looking up score.");
			return StatusCode(500, "An error occurred while looking up the score.");
		}
	}

	/// <summary>
	/// Gets all score submissions (posts) visible to everyone, regardless of status.
	/// </summary>
	[HttpGet]
	[Route("scores/submissions")]
	public async Task<IActionResult> GetAllSubmissions([FromQuery] int limit = 20, [FromQuery] int offset = 0)
	{
		try
		{
			var scores = await _scoreRepository.GetAllSubmissionsAsync(limit, offset);
			var dtos = scores.Select(s => new ScoreDto
			{
				Id = s.Id,
				User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = s.User?.Username ?? string.Empty },
				PlayerName = s.PlayerName,
				Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
				Value = s.Value,
				DateAchieved = s.DateAchieved,
				Title = s.Title,
				Description = s.Description,
				Status = s.Status,
				ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = s.ReviewedBy.Username },
				ReviewedAt = s.ReviewedAt,
				RejectionReason = s.RejectionReason
			}).ToList();

			return Ok(dtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving submissions.");
			return StatusCode(500, "An error occurred while retrieving submissions.");
		}
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
			return Unauthorized("Invalid user token.");
		}

		try
		{
			var scores = await _scoreRepository.GetAllScoresByUserAsync(userId, limit, offset);
			var dtos = scores.Select(s => new ScoreDto
			{
				Id = s.Id,
				User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = s.User?.Username ?? string.Empty },
				PlayerName = s.PlayerName,
				Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name, IsSubmitAllowed = s.Game.SubmitsAllowed },
				Value = s.Value,
				DateAchieved = s.DateAchieved,
				Title = s.Title,
				Description = s.Description,
				Status = s.Status,
				ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = s.ReviewedBy.Username },
				ReviewedAt = s.ReviewedAt,
				RejectionReason = s.RejectionReason
			}).ToList();

			return Ok(dtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving user submissions.");
			return StatusCode(500, "An error occurred while retrieving submissions.");
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