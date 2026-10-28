using System.Security.Claims;
using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leaderboard.Controllers;
#pragma warning disable CS1591

/// <summary>
/// Provides public leaderboard metadata and game-scoped leaderboard management.
/// </summary>
[ApiController]
[Route("api/games/{gameId:int}/leaderboards")]
public class LeaderboardController(ILeaderboardRepository repository) : ControllerBase
{
	private readonly ILeaderboardRepository _repository = repository;

	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<LeaderboardDto>>> List(
		int gameId, CancellationToken cancellationToken = default)
	{
		var boards = await _repository.GetByGameIdAsync(gameId, cancellationToken);
		return Ok(boards.Select(ToDto).ToList());
	}
	[HttpGet("{leaderboardId:int}")]
	public async Task<ActionResult<LeaderboardDto>> Get(
		int gameId, int leaderboardId, CancellationToken cancellationToken = default)
	{
		var board = await _repository.GetByIdAsync(gameId, leaderboardId, cancellationToken);
		return board is null ? NotFound() : Ok(ToDto(board));
	}
	[HttpPost]
	[Authorize]
	public async Task<ActionResult<LeaderboardDto>> Create(
		int gameId, [FromBody] CreateLeaderboardRequest request,
		CancellationToken cancellationToken = default)
	{
		if (!await HasManagePermissionAsync(gameId, cancellationToken))
		{
			return Forbid();
		}
		try
		{
			var board = await _repository.CreateAsync(gameId, request.Name, request.Description, cancellationToken);
			return CreatedAtAction(nameof(Get), new { gameId, leaderboardId = board.Id }, ToDto(board));
		}
		catch (ArgumentException exception)
		{
			return BadRequest(new { Message = exception.Message });
		}
		catch (InvalidOperationException exception)
		{
			return Conflict(new { Message = exception.Message });
		}
		catch (KeyNotFoundException exception)
		{
			return NotFound(new { Message = exception.Message });
		}
	}

	[HttpPut("{leaderboardId:int}")]
	[Authorize]
	public async Task<ActionResult<LeaderboardDto>> Update(
		int gameId, int leaderboardId, [FromBody] UpdateLeaderboardRequest request,
		CancellationToken cancellationToken = default)
	{
		if (!await HasManagePermissionAsync(gameId, cancellationToken))
		{
			return Forbid();
		}

		try
		{
			var board = await _repository.UpdateAsync(
				gameId, leaderboardId, request.Name, request.Description, request.IsActive, cancellationToken);
			if (board is null)
			{
				return NotFound();
			}

			return Ok(ToDto(board));
		}
		catch (ArgumentException exception)
		{
			return BadRequest(new { Message = exception.Message });
		}
		catch (InvalidOperationException exception)
		{
			return Conflict(new { Message = exception.Message });
		}
	}

	[HttpDelete("{leaderboardId:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Delete(
		int gameId, int leaderboardId, CancellationToken cancellationToken = default)
	{
		var result = await _repository.DeleteAsync(gameId, leaderboardId, cancellationToken);
		return result switch
		{
			LeaderboardDeleteResult.NotFound => NotFound(),
			LeaderboardDeleteResult.HasScores => Conflict(new
			{
				Message = "A leaderboard with scores cannot be deleted."
			}),
			_ => NoContent()
		};
	}
	private async Task<bool> HasManagePermissionAsync(int gameId, CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return false;
		}

		return await _repository.CanManageAsync(gameId, userId, cancellationToken);
	}

	private bool TryGetUserId(out int userId) =>
		int.TryParse(User.FindFirstValue(ClaimTypes.Name), out userId);

	private static LeaderboardDto ToDto(Leaderboard.Models.Leaderboard board) => new()
	{
		Id = board.Id,
		GameId = board.GameId,
		Name = board.Name,
		Description = board.Description,
		IsActive = board.IsActive,
		CreatedAt = board.CreatedAt,
		UpdatedAt = board.UpdatedAt
	};
}
#pragma warning restore CS1591
