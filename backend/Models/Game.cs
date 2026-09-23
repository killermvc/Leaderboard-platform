using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Models;

public class Game
{
	public int Id {get; set;}
	public required string Name {get; set;}
	public required string Description {get; set;}
	public string? ImageUrl {get; set;}
	public bool SubmitsAllowed {get; set;} = true;
}