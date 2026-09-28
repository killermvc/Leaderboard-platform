using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Leaderboard.Dtos;
using Leaderboard.Middleware;
using Leaderboard.Models;
using Leaderboard.Repositories;

namespace Leaderboard.Controllers.V1;

/// <summary>
/// Score submission for game clients, authenticated with a per game api key instead of a jwt.
/// Scores submitted here are not tied to a user account, the game client sends the name of the
/// player, and they are approved on submission so they reach the leaderboard without moderation.
/// </summary>
[ApiController]
[Route("api/v1/scores")]
public class ScoreController(
		IScoreRepository scoreRepository,
		IGameRepository gameRepository,
		ILogger<ScoreController> logger
	) : ControllerBase
{
	private readonly IScoreRepository _scoreRepository = scoreRepository;
	private readonly IGameRepository _gameRepository = gameRepository;
	private readonly ILogger _logger = logger;

	/// <summary>
	/// Submits a score for a named player on behalf of a game client.
	/// The score belongs to the game the api key is scoped to and is approved immediately.
	/// </summary>
	/// <response code="201">The score was created and is on the leaderboard. Repeating the same submission ID returns the original score.</response>
	/// <response code="400">The payload is missing, malformed or the game does not accept submissions.</response>
	/// <response code="401">The request was not authenticated with an api key.</response>
	/// <response code="403">The api key lacks the submit permission or is scoped to another game.</response>
	/// <response code="404">The game of the api key does not exist.</response>
	/// <response code="429">The score-submission rate limit was exceeded. The response includes a Retry-After header in seconds.</response>
	/// <response code="503">API-key validation is temporarily unavailable.</response>
	/// <response code="500">An unexpected server error occurred.</response>
	[ProducesResponseType(typeof(ScoreDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	[HttpPost("submit")]
	[Authorize]
	public async Task<IActionResult> SubmitScore([FromBody] SubmitScoreRequest request)
	{
		// A jwt cannot submit here, the player behind the score is identified by name only
		if (User.GetApiKeyId() is null)
		{
			return Unauthorized(new { Message = "An api key is required to submit scores." });
		}

		if (!User.HasApiKeyPermission(ApiKeyPermissions.SubmitScores))
		{
			return Forbid();
		}

		int keyGameId = User.GetApiKeyGameId()
			?? throw new InvalidOperationException("The api key is missing its game scope.");

		// The game id is optional in the payload, the key already knows the game it may submit to
		if (request.GameId.HasValue && request.GameId.Value != keyGameId)
		{
			return Forbid();
		}

		Game? game = await _gameRepository.GetGameByIdAsync(keyGameId);
		if (game is null)
		{
			return NotFound(new { Message = "Game not found." });
		}

		if (!game.SubmitsAllowed)
		{
			return BadRequest(new { Message = "Submissions are not allowed for this game." });
		}

		try
		{
			Score score = await _scoreRepository.SubmitNamedScoreAsync(
				keyGameId,
				request.Name!,
				request.Score,
				request.SubmissionId!,
				request.Title,
				request.Description);

			_logger.LogInformation(
				"Api key {ApiKeyId} submitted score {ScoreValue} of player {PlayerName} for game {GameId}, score {ScoreId}.",
				User.GetApiKeyId(), score.Value, score.PlayerName, keyGameId, score.Id);

			return StatusCode(StatusCodes.Status201Created, ToScoreDto(score));
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { Message = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { Message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error submitting score for game {GameId}.", keyGameId);
			return StatusCode(500, new { Message = "An error occurred while submitting the score." });
		}
	}

	/// <summary>
	/// Gets the highest approved score submitted by a named player for the game's api key.
	/// </summary>
	/// <remarks>
	/// Send the key in the X-API-Key header. Clients that cannot set headers may use the apiKey
	/// or api_key query parameter instead. The key must have ReadScores permission and be scoped
	/// to the gameId in the route.
	/// </remarks>
	/// <response code="200">The player's highest approved score.</response>
	/// <response code="401">The API key is missing, invalid, revoked, or expired.</response>
	/// <response code="403">The key lacks ReadScores permission or is scoped to another game.</response>
	/// <response code="404">No approved score exists for the player, or the game does not exist.</response>
	/// <response code="503">API-key validation is temporarily unavailable.</response>
	/// <response code="500">An unexpected server error occurred.</response>
	[ProducesResponseType(typeof(ScoreDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	[HttpGet("game/{gameId}/player/{playerName}")]
	[Authorize]
	public async Task<IActionResult> GetScoreByPlayerName(int gameId, string playerName)
	{
		if (!TryAuthorizeGameRead(gameId, ApiKeyPermissions.ReadScores, out IActionResult? authorizationResult))
		{
			return authorizationResult!;
		}

		Score? score = await _scoreRepository.GetBestNamedScoreByGameAsync(gameId, playerName);
		return score is null
			? NotFound(new { Message = "Score not found." })
			: Ok(ToScoreDto(score));
	}

	/// <summary>
	/// Gets the complete approved leaderboard for the game's api key.
	/// </summary>
	/// <remarks>
	/// Send the key in the X-API-Key header. Clients that cannot set headers may use the apiKey
	/// or api_key query parameter instead. The key must have ReadLeaderboard permission and be
	/// scoped to the gameId in the route.
	/// </remarks>
	/// <response code="200">The approved leaderboard, ordered from highest to lowest score.</response>
	/// <response code="401">The API key is missing, invalid, revoked, or expired.</response>
	/// <response code="403">The key lacks ReadLeaderboard permission or is scoped to another game.</response>
	/// <response code="404">The game does not exist.</response>
	/// <response code="503">API-key validation is temporarily unavailable.</response>
	/// <response code="500">An unexpected server error occurred.</response>
	[ProducesResponseType(typeof(List<LeaderboardEntry>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	[HttpGet("game/{gameId}/leaderboard")]
	[Authorize]
	public async Task<IActionResult> GetLeaderboard(int gameId)
	{
		if (!TryAuthorizeGameRead(gameId, ApiKeyPermissions.ReadLeaderboard, out IActionResult? authorizationResult))
		{
			return authorizationResult!;
		}

		try
		{
			List<LeaderboardEntry> leaderboard = await _scoreRepository.GetLeaderboardAsync(gameId, int.MaxValue);
			return Ok(leaderboard);
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(new { Message = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving leaderboard for game {GameId}.", gameId);
			return StatusCode(500, new { Message = "An error occurred while retrieving the leaderboard." });
		}
	}

	private bool TryAuthorizeGameRead(int gameId, ApiKeyPermissions permission, out IActionResult? result)
	{
		if (User.GetApiKeyId() is null)
		{
			result = Unauthorized(new { Message = "An api key is required to read scores." });
			return false;
		}

		if (!User.HasApiKeyPermission(permission))
		{
			result = Forbid();
			return false;
		}

		if (User.GetApiKeyGameId() != gameId)
		{
			result = Forbid();
			return false;
		}

		result = null;
		return true;
	}

	private static ScoreDto ToScoreDto(Score s) => new()
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

/// <summary>
/// Payload of a score submitted by a game client.
/// </summary>
public class SubmitScoreRequest
{
	/// <summary>
	/// The game the score belongs to. Optional, it defaults to the game the api key is scoped to
	/// and must match it when given.
	/// </summary>
	public int? GameId { get; set; }

	/// <summary>
	/// The score value reported by the game.
	/// </summary>
	/// <example>12500</example>
	public int Score { get; set; }

	/// <summary>
	/// Client-generated unique identifier for this submission. Retrying with the same identifier
	/// returns the original score instead of creating another submission.
	/// </summary>
	[Required]
	[StringLength(128, MinimumLength = 1)]
	public string? SubmissionId { get; set; }

	/// <summary>
	/// The name of the player. The score is not tied to a user account, so this name is how the
	/// player shows up on the leaderboard. Same name, same leaderboard entry, best score counts.
	/// </summary>
	[Required]
	[RegularExpression(@".*\S.*", ErrorMessage = "Name cannot be empty or whitespace.")]
	[StringLength(64, MinimumLength = 1)]
	public string? Name { get; set; }

	/// <summary>
	/// Optional title of the score post. Defaults to "GameName - ScoreValue".
	/// </summary>
	/// <example>Level 7 completed</example>
	public string? Title { get; set; }

	/// <summary>
	/// Optional description of the score post.
	/// </summary>
	/// <example>Finished the bonus route without taking damage.</example>
	public string? Description { get; set; }
}
