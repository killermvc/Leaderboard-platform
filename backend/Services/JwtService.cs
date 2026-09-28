using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Leaderboard.Models;

namespace Leaderboard.Services;

/// <summary>
/// Service responsible for generating JWT tokens for authenticated users.
/// </summary>
public class JwtService(IConfiguration configuration) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;

	/// <summary>
	/// Generates a JWT token for the specified user, including their roles as claims, and signs it using the configured secret key. The token is set to expire in one day.
	/// </summary>
	/// <param name="user">The user for whom to generate the token.</param>
	/// <returns>The generated JWT token.</returns>
	public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Id.ToString())
        };

        foreach (UserRole userRole in user.UserRoles)
        {
            claims.Add(new(ClaimTypes.Role, userRole.Role.Name));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
