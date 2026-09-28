using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;

using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Dtos;

namespace Leaderboard.Controllers;

/// <summary>
/// Controller for managing score moderation and game moderators.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModerationController(
    IScoreRepository scoreRepository,
    IGameModeratorRepository gameModeratorRepository,
    IGameRepository gameRepository,
    IUserRepository userRepository,
    ILogger<ModerationController> logger
) : ControllerBase
{
    private readonly IScoreRepository _scoreRepository = scoreRepository;
    private readonly IGameModeratorRepository _gameModeratorRepository = gameModeratorRepository;
    private readonly IGameRepository _gameRepository = gameRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Approves a pending score. Only game moderators or global moderators can approve scores.
    /// </summary>
    [HttpPost("scores/{scoreId}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveScore(int scoreId)
    {
        var userIdClaim = User.Identity?.Name;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
            // Get the score to check which game it belongs to
            var score = await _scoreRepository.GetByIdAsync(scoreId);
            if (score == null)
            {
                return NotFound("Score not found.");
            }

            // Check if user can moderate this game
            if (!await _gameModeratorRepository.CanModerateGameAsync(score.Game.Id, userId))
            {
                return Forbid("You are not authorized to moderate scores for this game.");
            }

            // Prevent moderators from approving their own scores
            if (score.UserId == userId)
            {
                return BadRequest("You cannot approve your own score.");
            }

            await _scoreRepository.ApproveScoreAsync(scoreId, userId);
            return Ok(new { Message = "Score approved successfully." });
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
            _logger.LogError(ex, "Error approving score.");
            return StatusCode(500, "An error occurred while approving the score.");
        }
    }

    /// <summary>
    /// Rejects a pending score with an optional reason.
    /// </summary>
    [HttpPost("scores/{scoreId}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectScore(int scoreId, [FromBody] RejectScoreRequest? request = null)
    {
        Console.WriteLine($"RejectScore called. Request is null: {request == null}, Reason: {request?.Reason ?? "(null)"}");
        var userIdClaim = User.Identity?.Name;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
            // Get the score to check which game it belongs to
            var score = await _scoreRepository.GetByIdAsync(scoreId);
            if (score == null)
            {
                return NotFound("Score not found.");
            }

            // Check if user can moderate this game
            if (!await _gameModeratorRepository.CanModerateGameAsync(score.Game.Id, userId))
            {
                return Forbid("You are not authorized to moderate scores for this game.");
            }

            // Prevent moderators from rejecting their own scores
            if (score.UserId == userId)
            {
                return BadRequest("You cannot reject your own score.");
            }

            await _scoreRepository.RejectScoreAsync(scoreId, userId, request?.Reason);
            return Ok(new { Message = "Score rejected successfully." });
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
            _logger.LogError(ex, "Error rejecting score.");
            return StatusCode(500, "An error occurred while rejecting the score.");
        }
    }

    /// <summary>
    /// Gets pending scores for a specific game (for game moderators).
    /// </summary>
    [HttpGet("games/{gameId}/pending-scores")]
    [Authorize]
    public async Task<IActionResult> GetPendingScoresForGame(int gameId, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var userIdClaim = User.Identity?.Name;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
            // Check if user can moderate this game
            if (!await _gameModeratorRepository.CanModerateGameAsync(gameId, userId))
            {
                return Forbid("You are not authorized to view pending scores for this game.");
            }

            var scores = await _scoreRepository.GetPendingScoresForGameAsync(gameId, limit, offset);
            var dtos = scores.Select(MapToScoreDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending scores.");
            return StatusCode(500, "An error occurred while retrieving pending scores.");
        }
    }

    /// <summary>
    /// Gets all pending scores that the current user can moderate.
    /// For game moderators: returns pending scores for their assigned games.
    /// For global moderators: returns pending scores for games without specific moderators.
    /// </summary>
    [HttpGet("pending-scores")]
    [Authorize]
    public async Task<IActionResult> GetPendingScores([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var userIdClaim = User.Identity?.Name;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
            var allPendingScores = new List<Score>();

            // Get games the user is a moderator for
            var moderatedGames = await _gameModeratorRepository.GetGamesByModeratorAsync(userId);

            foreach (var gm in moderatedGames)
            {
                var gamePendingScores = await _scoreRepository.GetPendingScoresForGameAsync(gm.GameId, limit, offset);
                allPendingScores.AddRange(gamePendingScores);
            }

            // If user is a global moderator, also get scores for unmoderated games
            if (await _gameModeratorRepository.IsGlobalModeratorAsync(userId))
            {
                var unmoderatedScores = await _scoreRepository.GetPendingScoresForUnmoderatedGamesAsync(limit, offset);
                allPendingScores.AddRange(unmoderatedScores);
            }

            // Remove duplicates and order by date
            var uniqueScores = allPendingScores
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .OrderBy(s => s.DateAchieved)
                .Take(limit)
                .ToList();

            var dtos = uniqueScores.Select(MapToScoreDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending scores.");
            return StatusCode(500, "An error occurred while retrieving pending scores.");
        }
    }

    /// <summary>
    /// Adds a moderator to a game. Only admins can do this.
    /// </summary>
    [HttpPost("games/{gameId}/moderators/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddGameModerator(int gameId, int userId)
    {
        try
        {
            var game = await _gameRepository.GetGameByIdAsync(gameId);
            if (game == null)
            {
                return NotFound("Game not found.");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            await _gameModeratorRepository.AddModeratorAsync(gameId, userId);
            return Ok(new { Message = $"User {user.Username} is now a moderator for {game.Name}." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding game moderator.");
            return StatusCode(500, "An error occurred while adding the game moderator.");
        }
    }

    /// <summary>
    /// Removes a moderator from a game. Only admins can do this.
    /// </summary>
    [HttpDelete("games/{gameId}/moderators/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveGameModerator(int gameId, int userId)
    {
        try
        {
            await _gameModeratorRepository.RemoveModeratorAsync(gameId, userId);
            return Ok(new { Message = "Moderator removed successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing game moderator.");
            return StatusCode(500, "An error occurred while removing the game moderator.");
        }
    }

    /// <summary>
    /// Gets all moderators for a specific game.
    /// </summary>
    [HttpGet("games/{gameId}/moderators")]
    public async Task<IActionResult> GetGameModerators(int gameId)
    {
        try
        {
            var game = await _gameRepository.GetGameByIdAsync(gameId);
            if (game == null)
            {
                return NotFound("Game not found.");
            }

            var moderators = await _gameModeratorRepository.GetModeratorsForGameAsync(gameId);
            var dtos = moderators.Select(gm => new GameModeratorDto
            {
                Id = gm.Id,
                UserId = gm.UserId,
                User = new GameModeratorUserDto
                {
                    Id = gm.User.Id,
                    Username = gm.User.Username
                },
                GameId = gm.GameId,
                GameName = gm.Game.Name,
                AssignedAt = gm.AssignedAt
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game moderators.");
            return StatusCode(500, "An error occurred while retrieving game moderators.");
        }
    }

    /// <summary>
    /// Gets all games the current user is a moderator for.
    /// </summary>
    [HttpGet("my-games")]
    [Authorize]
    public async Task<IActionResult> GetMyModeratedGames()
    {
        var userIdClaim = User.Identity?.Name;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
            var games = await _gameModeratorRepository.GetGamesByModeratorAsync(userId);
            var dtos = games.Select(gm => new GameDto
            {
                Id = gm.Game.Id,
                Name = gm.Game.Name,
                Description = gm.Game.Description,
                ImageUrl = gm.Game.ImageUrl
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving moderated games.");
            return StatusCode(500, "An error occurred while retrieving moderated games.");
        }
    }

    /// <summary>
    /// Checks if the current user can moderate a specific game.
    /// </summary>
    [HttpGet("games/{gameId}/can-moderate")]
    [Authorize]
    public async Task<IActionResult> CanModerateGame(int gameId)
    {
        var userIdClaim = User.Identity?.Name;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid user token.");
        }

        try
        {
            var canModerate = await _gameModeratorRepository.CanModerateGameAsync(gameId, userId);
            return Ok(new { CanModerate = canModerate });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking moderation rights.");
            return StatusCode(500, "An error occurred while checking moderation rights.");
        }
    }

    private static ScoreDto MapToScoreDto(Score s) => new()
    {
        Id = s.Id,
        User = s.User == null ? null : new UserDto { Id = s.User.Id, Username = s.User.Username },
        PlayerName = s.PlayerName,
        Game = s.Game == null ? null : new GameDto { Id = s.Game.Id, Name = s.Game.Name },
        Value = s.Value,
        DateAchieved = s.DateAchieved,
        Title = s.Title,
        Description = s.Description,
        Status = s.Status,
        ReviewedBy = s.ReviewedBy == null ? null : new UserDto { Id = s.ReviewedBy.Id, Username = s.ReviewedBy.Username },
        ReviewedAt = s.ReviewedAt,
        RejectionReason = s.RejectionReason
    };
}

/// <summary>Request payload for rejecting a score.</summary>
public class RejectScoreRequest
{
    /// <summary>Gets or sets the reason for rejecting the score.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>Basic user information for a game moderator.</summary>
public class GameModeratorUserDto
{
    /// <summary>Gets or sets the user's identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the user's username.</summary>
    public string Username { get; set; } = string.Empty;
}

/// <summary>Represents a moderator assigned to a game.</summary>
public class GameModeratorDto
{
    /// <summary>Gets or sets the moderator assignment identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the moderator user's identifier.</summary>
    public int UserId { get; set; }
    /// <summary>Gets or sets the moderator user.</summary>
    public GameModeratorUserDto User { get; set; } = null!;
    /// <summary>Gets or sets the game identifier.</summary>
    public int GameId { get; set; }
    /// <summary>Gets or sets the game name.</summary>
    public string GameName { get; set; } = string.Empty;
    /// <summary>Gets or sets the assignment date and time.</summary>
    public DateTime AssignedAt { get; set; }
}
