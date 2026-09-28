using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Models;
/// <summary>
/// Represents a user in the application, containing essential information such as username, password hash, roles, and creation timestamp.
/// </summary>
public class User
{
	/// <summary>
	/// The unique identifier for the user.
	/// </summary>
	[Key]
	public int Id {get; set;}
	/// <summary>
	/// The username of the user, which must be unique and is required for authentication and identification within the application.
	/// </summary>
	[Required]
	public string Username {get; set;} = string.Empty;
	/// <summary>
	/// The hashed password of the user, which is required for secure authentication. The actual password is never stored in plain text for security reasons.
	/// </summary>
	[Required]
	public string PasswordHash {get; set;} = string.Empty;
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