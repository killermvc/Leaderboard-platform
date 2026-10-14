using System.Security.Claims;
using Leaderboard.Models;
using Leaderboard.Repositories;

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
	public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
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
}