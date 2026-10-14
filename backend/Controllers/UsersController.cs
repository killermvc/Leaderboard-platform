using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591

namespace Leaderboard.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
	IClerkDirectoryService clerkDirectoryService,
	IUserRepository userRepository,
	IScoreRepository scoreRepository) : ControllerBase
{
	[HttpGet("search")]
	public async Task<IActionResult> Search(
		[FromQuery] string q,
		[FromQuery] int limit = 20,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(q))
		{
			return BadRequest(new { Message = "Search query is required" });
		}

		try
		{
			IReadOnlyList<ClerkDirectoryUser> clerkUsers = await clerkDirectoryService.SearchUsersAsync(q, limit, cancellationToken);

			var results = new List<UserDto>();
			foreach (ClerkDirectoryUser clerkUser in clerkUsers)
			{
				// Get or create local user for this Clerk user
				User user = await userRepository.GetOrCreateUserByClerkIdAsync(clerkUser.ClerkUserId);

				// Skip inactive users (deleted in Clerk)
				if (!user.IsActive)
				{
					continue;
				}

				// Keep the stored avatar in sync with Clerk (covers users created before webhook sync)
				if (clerkUser.ImageUrl != user.ProfileImageUrl)
				{
					user.ProfileImageUrl = clerkUser.ImageUrl;
					await userRepository.UpdateUserAsync(user);
				}

				var scores = await scoreRepository.GetScoresByUserAsync(user.Id, 1000, 0);
				results.Add(new UserDto
				{
					Id = user.Id,
					Username = clerkUser.DisplayName,
					ScoresCount = scores.Count,
					GamesPlayedCount = scores.Select(score => score.Game.Id).Distinct().Count(),
					CreatedAt = user.CreatedAt,
					ProfileImageUrl = user.ProfileImageUrl
				});
			}

			return Ok(results);
		}
		catch (InvalidOperationException ex)
		{
			return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = ex.Message });
		}
	}
}

#pragma warning restore CS1591
