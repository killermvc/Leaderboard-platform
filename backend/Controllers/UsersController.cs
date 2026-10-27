using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leaderboard.Controllers;

/// <summary>
/// Controller for user-related operations, including searching for users in the Clerk directory and retrieving user information from the local database.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController(
	IClerkDirectoryService clerkDirectoryService,
	IUserRepository userRepository,
	IScoreRepository scoreRepository) : ControllerBase
{
	/// <summary>
	/// Searches for users in the Clerk directory and retrieves their information from the local database.
	/// </summary>
	/// <param name="q">The search query.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns></returns>
	[HttpGet("search")]
	[Authorize]
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
			IReadOnlyList<ClerkDirectoryUser> clerkUsers = await clerkDirectoryService.SearchUsersAsync(q, Math.Clamp(limit, 1, 50), cancellationToken);

			var clerkIds = clerkUsers
				.Select(u => u.ClerkUserId)
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.Ordinal)
				.ToList();
			List<User> localUsers = clerkIds.Count == 0
				? []
				: await userRepository.GetUsersByClerkIdsAsync(clerkIds);
			var localByClerkId = localUsers.ToDictionary(u => u.ClerkUserId!, StringComparer.Ordinal);

			var results = new List<UserDto>();
			foreach (ClerkDirectoryUser clerkUser in clerkUsers)
			{
				// Never create local users on search: only surface users already known locally.
				if (!localByClerkId.TryGetValue(clerkUser.ClerkUserId, out User? user))
				{
					continue;
				}

				// Skip inactive users (deleted in Clerk)
				if (!user.IsActive)
				{
					continue;
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