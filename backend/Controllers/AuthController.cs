using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using Leaderboard.Repositories;
using Leaderboard.Models;
using Leaderboard.Dtos;
using Leaderboard.Services;
using Microsoft.AspNetCore.Authorization;

namespace Leaderboard.Controllers;

/// <summary>
/// Provides authentication and user account endpoints.
///</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IUserRepository _userRepository;
	private readonly IScoreRepository _scoreRepository;
	private readonly IClerkDirectoryService _clerkDirectory;

	/// <summary>
	/// Initializes the authentication controller.
	///</summary>
	public AuthController(IUserRepository userRepository, IScoreRepository scoreRepository, IClerkDirectoryService clerkDirectoryService)
	{
		_userRepository = userRepository;
		_scoreRepository = scoreRepository;
		_clerkDirectory = clerkDirectoryService;
	}

	/// <summary>
	/// Promotes a user to administrator.
	///</summary>
	[HttpPut("promote/{userId}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> PromoteToAdmin([FromRoute] int userId)
	{
		User? user = await _userRepository.GetUserByIdAsync(userId);

		if(user == null)
		{
			return NotFound(new {Message = "User not found"});
		}

		await _userRepository.AddRoleToUserAsync(user, new Role { Name = "Admin" });

		return Ok(new {Message = "User promoted to Admin successfully"});
	}

	// GET: api/auth/me
	/// <summary>Returns the authenticated user's basic information.</summary>
	[HttpGet("me")]
	[Authorize]
	public async Task<IActionResult> GetCurrentUser()
	{
		var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
		if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
		{
			return Unauthorized(new { Message = "Invalid token" });
		}

		User? user = await _userRepository.GetUserByIdAsync(userId);
		if (user == null)
			return NotFound(new { Message = "User not found" });

		return Ok(new
		{
			Id = user.Id,
			ClerkUserId = user.ClerkUserId,
			Roles = user.UserRoles
				.Where(userRole => userRole.Role is not null)
				.Select(userRole => userRole.Role!.Name)
				.ToArray()
		});
	}

	/// <summary>Returns a user's profile and score statistics.</summary>
	[HttpGet("user/{userId}")]
	public async Task<IActionResult> GetUserProfile([FromRoute] int userId)
	{
		User? user = await _userRepository.GetUserByIdAsync(userId);
		if (user == null)
			return NotFound(new { Message = "User not found" });

		var scores = await _scoreRepository.GetScoresByUserAsync(userId, 1000, 0);

		// Get rankings for each game the user has played
		var gameRankings = new List<object>();
		var gamesPlayed = scores.Select(s => s.Game).DistinctBy(g => g.Id).ToList();

		foreach (var game in gamesPlayed)
		{
			var rank = await _scoreRepository.GetRankAsync(game.Id, userId);
			var userBestScore = scores.Where(s => s.Game.Id == game.Id).Max(s => s.Value);
			gameRankings.Add(new {
				GameId = game.Id,
				GameName = game.Name,
				Rank = rank,
				BestScore = userBestScore,
				ScoresSubmitted = scores.Count(s => s.Game.Id == game.Id)
			});
		}

		var recentScores = scores.OrderByDescending(s => s.DateAchieved).Take(10).Select(s => new {
			Id = s.Id,
			GameId = s.Game.Id,
			GameName = s.Game.Name,
			Value = s.Value,
			DateAchieved = s.DateAchieved
		}).ToList();

		return Ok(new {
			Id = user.Id,
			Username = await _clerkDirectory.GetDisplayNameAsync(user.ClerkUserId ?? string.Empty),
			ClerkUserId = user.ClerkUserId,
			CreatedAt = user.CreatedAt,
			TotalScoresSubmitted = scores.Count,
			GamesPlayedCount = gamesPlayed.Count,
			GameRankings = gameRankings,
			RecentScores = recentScores
		});
	}
}

