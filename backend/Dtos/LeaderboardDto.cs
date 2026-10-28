namespace Leaderboard.Dtos;

/// <summary>
/// Public representation of a game leaderboard.
/// </summary>
public class LeaderboardDto
{
	/// <summary>The leaderboard identifier.</summary>
	public int Id { get; set; }
	/// <summary>The parent game identifier.</summary>
	public int GameId { get; set; }
	/// <summary>The display name.</summary>
	public string Name { get; set; } = string.Empty;
	/// <summary>The optional description.</summary>
	public string? Description { get; set; }
	/// <summary>Whether new scores can be submitted.</summary>
	public bool IsActive { get; set; }
	/// <summary>When the leaderboard was created.</summary>
	public DateTime CreatedAt { get; set; }
	/// <summary>When the leaderboard was last updated.</summary>
	public DateTime UpdatedAt { get; set; }
}
