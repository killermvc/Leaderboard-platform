namespace Leaderboard.Models;

public class Score
{
	public int Id { get; set; }

	/// <summary>
	/// The account that submitted this score.
	/// Null for scores submitted by a game client through the api, those carry a <see cref="PlayerName"/> instead.
	/// </summary>
	public int? UserId { get; set; }
	public User? User { get; set; }

	public required Game Game { get; set; }
	public int Value { get; set; }
	public DateTime DateAchieved { get; set; }

	/// <summary>
	/// The name given to this score when it is not tied to a user account.
	/// Null for scores submitted by a registered user, their username is displayed then.
	/// </summary>
	public string? PlayerName { get; set; }

	/// <summary>
	/// The title of the score submission post.
	/// Defaults to "GameName - ScoreValue".
	/// </summary>
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Optional description provided by the user for this score submission.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// The approval status of this score. Defaults to Pending.
	/// Only approved scores are shown on the leaderboard.
	/// Scores coming from a game client through the api are set to Approved on submission, they skip moderation.
	/// </summary>
	public ScoreStatus Status { get; set; } = ScoreStatus.Pending;

	/// <summary>
	/// The moderator who reviewed this score (approved or rejected).
	/// Null if the score is still pending.
	/// </summary>
	public User? ReviewedBy { get; set; }

	/// <summary>
	/// The date and time when the score was reviewed.
	/// Null if the score is still pending.
	/// </summary>
	public DateTime? ReviewedAt { get; set; }

	/// <summary>
	/// Optional reason provided by the moderator when rejecting a score.
	/// </summary>
	public string? RejectionReason { get; set; }
}
