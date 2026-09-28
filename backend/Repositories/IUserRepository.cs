using Leaderboard.Models;

namespace Leaderboard.Repositories;

/// <summary>
/// Provides persistence operations for users.
/// </summary>
public interface IUserRepository
{
	/// <summary>
	/// Gets a user by username.
	/// </summary>
	/// <param name="username">The username to search for.</param>
	/// <returns>The matching user, or <see langword="null"/> if no user is found.</returns>
	public Task<User?> GetUserByNameAsync(string username);
	/// <summary>
	/// Gets a user by identifier.
	/// </summary>
	/// <param name="id">The user identifier.</param>
	/// <returns>The matching user, or <see langword="null"/> if no user is found.</returns>
	public Task<User?> GetUserByIdAsync(int id);
	/// <summary>
	/// Adds a user.
	/// </summary>
	/// <param name="user">The user to add.</param>
	public Task AddUserAsync(User user);
	/// <summary>
	/// Updates a user.
	/// </summary>
	/// <param name="user">The user to update.</param>
	public Task UpdateUserAsync(User user);

	/// <summary>
	/// Adds a role to a user.
	/// </summary>
	/// <param name="user">The user to modify.</param>
	/// <param name="role">The role to add.</param>
	public Task AddRoleToUserAsync(User user, Role role);
	/// <summary>
	/// Searches for users matching a query.
	/// </summary>
	/// <param name="query">The search query.</param>
	/// <param name="limit">The maximum number of users to return.</param>
	/// <returns>A list of matching users.</returns>
	public Task<List<User>> SearchUsersAsync(string query, int limit);
}