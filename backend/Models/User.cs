using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Models;
/// <summary>
/// Represents a local application user linked to a Clerk identity.
/// </summary>
public class User
{
	/// <summary>
	/// The unique identifier for the user.
	/// </summary>
	[Key]
	public int Id {get; set;}
	/// <summary>
	/// The stable subject identifier issued by Clerk for externally managed users.
	/// </summary>
	[MaxLength(255)]
	public string? ClerkUserId { get; set; }
	/// <summary>
	/// The profile image URL synced from Clerk (via webhooks). Null when none is set.
	/// </summary>
	[MaxLength(2048)]
	public string? ProfileImageUrl { get; set; }
	/// <summary>
	/// Whether the linked Clerk identity can access the application.
	/// </summary>
	public bool IsActive { get; set; } = true;
	/// <summary>
	/// A collection of roles assigned to the user, allowing for role-based access control within the application. This establishes a many-to-many relationship with the Role entity through the UserRole join table.
	/// </summary>/
	public List<UserRole> UserRoles {get; set;} = [];
	/// <summary>
	/// The timestamp indicating when the user account was created.
	/// This is automatically set to the current date and time when the user is created, and can be used for auditing and tracking purposes.
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}