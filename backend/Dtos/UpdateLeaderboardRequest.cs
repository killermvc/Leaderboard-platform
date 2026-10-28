namespace Leaderboard.Dtos;

/// <summary>
/// Request to update a game leaderboard.
/// </summary>
public class UpdateLeaderboardRequest
{
	/// <summary>The requested display name.</summary>
	public string Name { get; set; } = string.Empty;
	/// <summary>The optional description.</summary>
	public string? Description { get; set; }
	/// <summary>Whether new scores can be submitted.</summary>
	public bool IsActive { get; set; }
}
