using System.ComponentModel.DataAnnotations;

#pragma warning disable CS1591

namespace Leaderboard.Models;

public class ClerkWebhookEvent
{
	[Key]
	[MaxLength(255)]
	public string Id { get; set; } = string.Empty;

	public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

#pragma warning restore CS1591
