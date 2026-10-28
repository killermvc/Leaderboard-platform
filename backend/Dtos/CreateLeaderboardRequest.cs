namespace Leaderboard.Dtos;

/// <summary>
/// Request to create a leaderboard for a game.
/// </summary>
public class CreateLeaderboardRequest
{
	/// <summary>The requested display name.</summary>
	public string Name { get; set; } = string.Empty;
	/// <summary>The optional description.</summary>
	public string? Description { get; set; }
}
