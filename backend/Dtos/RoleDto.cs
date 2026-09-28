namespace Leaderboard.Dtos;

/// <summary>
/// Data Transfer Object for Role entity.
/// </summary>
public class RoleDto
{
	/// <summary>
	/// The unique identifier of the role.
	/// </summary>
    public int Id { get; set; }
	/// <summary>
	/// The name of the role.
	/// </summary>
    public string Name { get; set; } = string.Empty;
}