using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Models;

/// <summary>
/// Represents a game in the leaderboard system. Each game can have its own set of scores, API keys, and other related data.
/// </summary>
public class Game
{
	/// <summary>
	/// The unique identifier for the game.
	/// </summary>
	public int Id {get; set;}
	/// <summary>
	/// The name of the game.
	/// </summary>
	public required string Name {get; set;}
	/// <summary>
	/// The description of the game.
	/// </summary>
	public required string Description {get; set;}
	/// <summary>
	/// The URL of the image associated with the game.
	/// </summary>
	public string? ImageUrl {get; set;}
	/// <summary>
	/// Wether or not the game is currently accepting score submissions.
	///</summary>
	public bool SubmitsAllowed {get; set;} = true;
}