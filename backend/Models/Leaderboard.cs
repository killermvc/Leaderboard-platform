namespace Leaderboard.Models;

/// <summary>
/// Represents a ranking board belonging to a game.
/// </summary>
public class Leaderboard
{
	/// <summary>
	/// The unique identifier for the leaderboard.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The game that owns the leaderboard.
	/// </summary>
	public int GameId { get; set; }

	/// <summary>
	/// The game that owns the leaderboard.
	/// </summary>
	public Game Game { get; set; } = null!;

	/// <summary>
	/// The display name of the leaderboard.
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	/// Optional description of the leaderboard.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Whether the leaderboard accepts new submissions.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	/// The timestamp when the leaderboard was created.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// The timestamp when the leaderboard was last updated.
	/// </summary>
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
