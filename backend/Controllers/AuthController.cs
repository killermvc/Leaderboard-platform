using Microsoft.AspNetCore.Mvc;
using BCryptClass = BCrypt.Net.BCrypt;
using System.Security.Claims;

using Leaderboard.Repositories;
using Leaderboard.Models;
using Leaderboard.Services;
using Leaderboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Leaderboard.Controllers;

/// <summary>
/// Provides authentication and user account endpoints.
///</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IUserRepository _userRepository;
	private readonly IJwtService _jwtService;
	private readonly IScoreRepository _scoreRepository;

	/// <summary>
	/// Initializes the authentication controller.
	///</summary>
	public AuthController(IUserRepository userRepository, IJwtService jwtService, IScoreRepository scoreRepository)
	{
		_userRepository = userRepository;
		_jwtService = jwtService;
		_scoreRepository = scoreRepository;
	}

	/// <summary>
	/// Registers a new user.
	///</summary>
	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] CredentialsRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.UserName))
		{
			return BadRequest(new { Message = "Username is required" });
		}

		if (await _userRepository.GetUserByNameAsync(request.UserName) != null)
		{
			return BadRequest(new { Message = "Username already exists" });
		}

		var user = new User
		{
			Username = request.UserName,
			PasswordHash = BCryptClass.HashPassword(request.Password)
		};
		await _userRepository.AddUserAsync(user);
		return Ok(new { Message = "User registered successfully" });
	}
	/// <summary>
	/// Authenticates a user and returns a JWT token if successful.
	/// </summary>
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] CredentialsRequest request)
	{
		User? user = await _userRepository.GetUserByNameAsync(request.UserName!);

		if(user == null || !BCryptClass.Verify(request.Password, user.PasswordHash))
		{
			return Unauthorized(new {Message = "Invalid username or password"});
		}

		// Debug: Log roles being added to token
		Console.WriteLine($"User {user.Username} has {user.UserRoles.Count} roles:");
		foreach (var ur in user.UserRoles)
		{
			Console.WriteLine($"  - Role: {ur.Role?.Name ?? "(null)"}");
		}

		string token = _jwtService.GenerateToken(user);

		return Ok(new {Token = token, Message = "Login successful"});
	}

	/// <summary>Changes the authenticated user's password.</summary>
	[HttpPut]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
	{
		var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
		if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
		{
			return Unauthorized(new { Message = "Invalid token" });
		}

		User? user = await _userRepository.GetUserByIdAsync(userId);

		if(user == null || !BCryptClass.Verify(request.OldPassword, user.PasswordHash))
		{
			return Unauthorized(new {Message = "Invalid password"});
		}

		user.PasswordHash = BCryptClass.HashPassword(request.NewPassword);
		await _userRepository.UpdateUserAsync(user);

		return Ok(new {Message = "Password changed successfully"});
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
			Username = user.Username,
			Roles = user.UserRoles
				.Where(userRole => userRole.Role is not null)
				.Select(userRole => userRole.Role!.Name)
				.ToArray()
		});
	}

	/// <summary>Updates the authenticated user's username.</summary>
	[HttpPut("username")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
	{
		var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
		if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
		{
			return Unauthorized(new { Message = "Invalid token" });
		}

		if (string.IsNullOrWhiteSpace(request.NewUserName))
			return BadRequest(new { Message = "Invalid username" });

		if (await _userRepository.GetUserByNameAsync(request.NewUserName!) != null)
			return BadRequest(new { Message = "Username already exists" });

		User? user = await _userRepository.GetUserByIdAsync(userId);
		if (user == null)
			return NotFound(new { Message = "User not found" });

		user.Username = request.NewUserName;
		await _userRepository.UpdateUserAsync(user);

		return Ok(new { Message = "Username updated successfully" });
	}

	/// <summary>
	/// Searches for users.
	///</summary>
	[HttpGet("search")]
	public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int limit = 20)
	{
		if (string.IsNullOrWhiteSpace(q))
			return BadRequest(new { Message = "Search query is required" });

		var users = await _userRepository.SearchUsersAsync(q, limit);
		var userDtos = new List<UserDto>();

		foreach (var u in users)
		{
			var scores = await _scoreRepository.GetScoresByUserAsync(u.Id, 1000, 0);
			userDtos.Add(new UserDto
			{
				Id = u.Id,
				Username = u.Username,
				ScoresCount = scores.Count,
				GamesPlayedCount = scores.Select(s => s.Game.Id).Distinct().Count(),
				CreatedAt = u.CreatedAt
			});
		}

		return Ok(userDtos);
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
			Username = user.Username,
			CreatedAt = user.CreatedAt,
			TotalScoresSubmitted = scores.Count,
			GamesPlayedCount = gamesPlayed.Count,
			GameRankings = gameRankings,
			RecentScores = recentScores
		});
	}
}

/// <summary>
/// Contains username and password credentials.
///</summary>
public class CredentialsRequest
{
	/// <summary>
	/// Gets or sets the username.
	///</summary>
	public string? UserName {get; set;}
	/// <summary>
	/// Gets or sets the password.
	///</summary>
	public string? Password {get; set;}
}

/// <summary>
/// Contains a replacement username.
///</summary>
public class UpdateUsernameRequest
{
	/// <summary>
	/// Gets or sets the new username.
	///</summary>
	public string? NewUserName { get; set; }
}

/// <summary>
/// Contains the current and replacement passwords.
///</summary>
public class ChangePasswordRequest
{
	/// <summary>
	/// Gets or sets the current password.
	///</summary>
	public string? OldPassword { get; set; }
	/// <summary>
	/// Gets or sets the replacement password.
	///</summary>
	public string? NewPassword { get; set; }
}
