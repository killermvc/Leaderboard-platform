using System.Security.Claims;
using Leaderboard.Models;
using Leaderboard.Options;
using Leaderboard.Repositories;
using Microsoft.Extensions.Options;

namespace Leaderboard.Middleware;

/// <summary>
/// Maps an authenticated Clerk subject to the local user used by leaderboard records.
/// </summary>
public sealed class ClerkUserMiddleware(RequestDelegate next)
{
	private readonly RequestDelegate _next = next;

	/// <summary>
	/// Processes the HTTP request to map the Clerk user to a local user in the database.
	/// </summary>
	public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, IOptions<ClerkOptions>? clerkOptions = null)
	{
		string? clerkUserId = GetClerkUserId(context.User);
		if (context.User.Identity?.IsAuthenticated == true &&
			!string.IsNullOrWhiteSpace(clerkUserId))
		{
			User? user = await userRepository.GetUserByClerkIdAsync(clerkUserId);
			if (user is null)
			{
				user = new User { ClerkUserId = clerkUserId };
				await userRepository.AddUserAsync(user);
				user = await userRepository.GetUserByClerkIdAsync(clerkUserId) ?? user;
			}

			if (!user.IsActive)
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				return;
			}

			await EnsureSeedAdminAsync(user, clerkOptions, userRepository);

			var identity = (ClaimsIdentity)context.User.Identity!;
			foreach (Claim claim in identity.FindAll(ClaimTypes.Name).ToList())
				identity.RemoveClaim(claim);
			foreach (Claim claim in identity.FindAll(ClaimTypes.Role).ToList())
				identity.RemoveClaim(claim);
			identity.AddClaim(new Claim(ClaimTypes.Name, user.Id.ToString()));
			foreach (UserRole userRole in user.UserRoles)
			{
				if (userRole.Role is not null)
					identity.AddClaim(new Claim(ClaimTypes.Role, userRole.Role.Name));
			}
		}

		await _next(context);
	}

	private static string? GetClerkUserId(ClaimsPrincipal principal)
	{
		return principal.FindFirstValue("sub")
			?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
	}

	private static async Task EnsureSeedAdminAsync(User user, IOptions<ClerkOptions>? clerkOptions, IUserRepository userRepository)
	{
		string seeds = clerkOptions?.Value.AdminSeedClerkIds ?? string.Empty;
		if (string.IsNullOrWhiteSpace(seeds) ||
			string.IsNullOrWhiteSpace(user.ClerkUserId))
		{
			return;
		}

		bool isSeed = seeds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Any(id => string.Equals(id, user.ClerkUserId, StringComparison.Ordinal));
		if (!isSeed)
		{
			return;
		}

		bool alreadyAdmin = user.UserRoles.Any(ur => ur.Role is not null && ur.Role.Name == "Admin");
		if (alreadyAdmin)
		{
			return;
		}

		Role adminRole = await userRepository.GetOrCreateRoleByNameAsync("Admin");
		await userRepository.AddRoleToUserAsync(user, adminRole);
		User? refreshed = await userRepository.GetUserByClerkIdAsync(user.ClerkUserId);
		if (refreshed is not null)
		{
			user.UserRoles = refreshed.UserRoles;
		}
	}
}