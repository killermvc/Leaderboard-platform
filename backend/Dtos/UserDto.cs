namespace Leaderboard.Dtos;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class UserDto
{
	/// <summary>
	/// The unique identifier of the user.
	/// </summary>
    public int Id { get; set; }
	/// <summary>
	/// The username of the user.
	/// </summary>
    public string Username { get; set; } = string.Empty;
	/// <summary>
	/// The number of scores the user has submitted.
	/// </summary>
    public int ScoresCount { get; set; }
	/// <summary>
	/// The number of games the user has played.
	/// </summary>
    public int GamesPlayedCount { get; set; }
	/// <summary>
	/// The date and time when the user was created.
	/// </summary>
    public DateTime? CreatedAt { get; set; }
}