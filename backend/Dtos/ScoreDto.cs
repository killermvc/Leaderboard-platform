using Leaderboard.Models;

namespace Leaderboard.Dtos;

/// <summary>
/// Data Transfer Object for Score entity.
/// </summary>
public class ScoreDto
{
    /// <summary>
    /// The score identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The user associated with the score. Null if submitted by a game client without a user account.
    /// </summary>
    public UserDto? User { get; set; }

    /// <summary>
    /// The name given to this score when it is not tied to a user account.
    /// </summary>
    public string? PlayerName { get; set; }

    /// <summary>
    /// The game associated with the score.
    /// </summary>
    public GameDto? Game { get; set; }

    /// <summary>
    /// The score value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// The date and time when the score was achieved.
    /// </summary>
    public DateTime DateAchieved { get; set; }

    /// <summary>
    /// The score title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The score description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The score status.
    /// </summary>
    public ScoreStatus Status { get; set; }

    /// <summary>
    /// The user who reviewed the score.
    /// </summary>
    public UserDto? ReviewedBy { get; set; }

    /// <summary>
    /// The date and time when the score was reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// The reason the score was rejected.
    /// </summary>
    public string? RejectionReason { get; set; }
}