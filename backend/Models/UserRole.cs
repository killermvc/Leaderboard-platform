namespace Leaderboard.Models;

/// <summary>
/// Represents the association between a user and a role in the application, enabling role-based access control.
/// </summary>
public class UserRole
{
	/// <summary>
	/// The unique identifier for the user-role association.
	/// </summary>
    public int UserId {get; set;}
	/// <summary>
	/// The user associated with this role. This is a navigation property that allows for easy access to the related User entity.
	/// </summary>
	public User User {get; set;} = null!;
	/// <summary>
	/// The unique identifier for the role associated with this user. This establishes a relationship between the UserRole and the Role entity.
	/// </summary>
	public int RoleId {get; set;}
	/// <summary>
	/// The role associated with this user. This is a navigation property that allows for easy access to the related Role entity.
	/// </summary>
	public Role Role {get; set;} = null!;
}
