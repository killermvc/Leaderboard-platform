using Leaderboard.Models;

namespace Leaderboard.Services;

/// <summary>
/// Interface for a service that generates JWT tokens.
/// </summary>
public interface IJwtService
{
	/// <summary>
	/// Generates a JWT token for the specified user.
	/// </summary>
	/// <param name="user">The user for whom to generate the token.</param>
	/// <returns>The generated JWT token.</returns>
    string GenerateToken(User user);
}
