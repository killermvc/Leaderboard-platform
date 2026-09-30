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
	/// <summary>
	/// The optional ID of the user who owns this game.
	/// </summary>
	public int? OwnerId { get; set; }
	/// <summary>
	/// Indicates whether the game is currently accepting score submissions.
	/// If false, users will not be able to submit scores for this game.
	/// This doesn't affect api key submissions.
	/// </summary>
	public required bool IsSubmitAllowed {get; set;} = true;
}