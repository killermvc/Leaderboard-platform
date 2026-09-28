namespace Leaderboard.Models;

/// <summary>
/// Represents a moderator assignment for a specific game.
/// Game moderators can approve or reject scores submitted for their assigned games.
/// </summary>
public class GameModerator
{
	/// <summary>
	/// The unique identifier for the game moderator assignment.
	/// </summary>
    public int Id { get; set; }
	/// <summary>
	/// The ID of the game that this moderator is assigned to. This establishes a relationship between the GameModerator and the Game entity.
	/// </summary>
    public int GameId { get; set; }
	/// <summary>
	/// The game that this moderator is assigned to. This is a navigation property that allows for easy access to the related Game entity.
	/// </summary>
    public Game Game { get; set; } = null!;
	/// <summary>
	/// The ID of the user who is assigned as a moderator for the game. This establishes a relationship between the GameModerator and the User entity.
	/// </summary>
    public int UserId { get; set; }
	/// <summary>
	/// The user who is assigned as a moderator for the game. This is a navigation property that allows for easy access to the related User entity.
	/// </summary>
    public User User { get; set; } = null!;
	/// <summary>
	/// The timestamp of when this moderator assignment was created. This is automatically set when the assignment is created, and can be used for auditing and tracking purposes.
	/// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
