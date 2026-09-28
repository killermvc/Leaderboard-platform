namespace Leaderboard.Dtos;

/// <summary>
/// Data Transfer Object for LeaderboardEntry entity.
/// </summary>
public class LeaderboardEntryDto
{
    /// <summary>
    /// The account of the player, null when the player is only known by the name a game client gave.
    /// </summary>
    public int? UserId { get; set; }
	/// <summary>
	/// The name of the player, either from the account or given by a game client.
	/// </summary>
    public string? UserName { get; set; }
	/// <summary>
	/// The score value achieved by the player.
	/// </summary>
    public int Score { get; set; }
}
