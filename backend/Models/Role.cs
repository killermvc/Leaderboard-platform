using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Models;

/// <summary>
/// Represents a role that can be assigned to users in the application.
/// </summary>
public class Role
{
	/// <summary>
	/// Gets or sets the unique identifier for the role.
	/// </summary>
	public int Id {get; set;}
	/// <summary>
	/// Gets or sets the name of the role.
	/// </summary>
	[Required]
	public string Name {get; set;} = null!;
	/// <summary>
	/// Gets or sets the collection of user roles associated with this role.
	/// </summary>
	public List<UserRole> UserRoles { get; set;} = [];
}