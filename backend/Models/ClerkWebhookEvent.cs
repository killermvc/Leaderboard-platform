using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Models;

/// <summary>
/// Represents a Clerk webhook event that has been processed and stored in the database.
/// </summary>
public class ClerkWebhookEvent
{
	/// <summary>
	/// Gets or sets the unique identifier for the Clerk webhook event.
	/// </summary>
	[Key]
	[MaxLength(255)]
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Get or sets when the Clerk webhook event was processed and stored in the database. This property is automatically set to the current UTC time when the event is created.
	/// </summary>
	public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
