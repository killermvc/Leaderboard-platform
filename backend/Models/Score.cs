namespace Leaderboard.Models;

/// <summary>
/// Represents a score submitted by a user or a game client.
/// </summary>
public class Score
{
	/// <summary>
	/// The unique identifier for this score.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The account that submitted this score.
	/// Null for scores submitted by a game client through the api, those carry a <see cref="PlayerName"/> instead.
	/// </summary>
	public int? UserId { get; set; }
	/// <summary>
	/// The user account that submitted this score.
	/// Null for scores submitted by a game client through the api, those carry a <see cref="PlayerName"/> instead.
	/// </summary>
	public User? User { get; set; }
	/// <summary>
	/// The game this score belongs to.
	/// </summary>
	public required Game Game { get; set; }
	/// <summary>
	/// The game this score belongs to.
	/// </summary>
	public int GameId { get; set; }
	/// <summary>
	/// The numeric value of the score.
	/// </summary>
	public int Value { get; set; }
	/// <summary>
	/// The date and time when this score was achieved.
	/// This is automatically set to the current date and time when the score is created.
	/// </summary>
	public DateTime DateAchieved { get; set; }

	/// <summary>
	/// Client-generated identifier used to make game-client score submissions idempotent.
	/// Null for scores submitted by registered users.
	/// </summary>
	public string? SubmissionId { get; set; }

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
