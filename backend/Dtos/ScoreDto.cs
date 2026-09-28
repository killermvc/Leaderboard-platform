using System;
using Leaderboard.Models;

namespace Leaderboard.Dtos;

public class ScoreDto
{
    public int Id { get; set; }
    public UserDto? User { get; set; }

    /// <summary>
    /// The name given to this score when it is not tied to a user account.
    /// </summary>
    public string? PlayerName { get; set; }

    public GameDto? Game { get; set; }
    public int Value { get; set; }
    public DateTime DateAchieved { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScoreStatus Status { get; set; }
    public UserDto? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}