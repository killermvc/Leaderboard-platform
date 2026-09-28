namespace Leaderboard.Dtos;

/// <summary>
/// Data Transfer Object for Game entity.
/// </summary>
public class GameDto
{
	/// <summary>
	/// The unique identifier of the game.
	/// </summary>
    public int Id { get; set; }
	/// <summary>
	/// The name of the game.
	/// </summary>
    public string Name { get; set; } = string.Empty;
	/// <summary>
	/// A brief description of the game.
	/// </summary>
    public string Description { get; set; } = string.Empty;
	/// <summary>
	/// The URL to an image representing the game. This can be used for display purposes in the UI.
	/// </summary>
    public string? ImageUrl { get; set; }
}