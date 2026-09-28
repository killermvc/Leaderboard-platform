namespace Leaderboard.Dtos;

public class LeaderboardEntryDto
{
    /// <summary>
    /// The account of the player, null when the player is only known by the name a game client gave.
    /// </summary>
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public int Score { get; set; }
}
