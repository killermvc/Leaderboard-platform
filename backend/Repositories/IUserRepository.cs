using Leaderboard.Models;

namespace Leaderboard.Repositories;

/// <summary>
/// Provides persistence operations for users.
/// </summary>
public interface IUserRepository
{
	/// <summary>
	/// Gets a user by identifier.
	/// </summary>
	/// <param name="id">The user identifier.</param>
	/// <returns>The matching user, or <see langword="null"/> if no user is found.</returns>
	public Task<User?> GetUserByIdAsync(int id);
	/// <summary>
	/// Gets a user by the Clerk subject identifier.
	/// </summary>
	public Task<User?> GetUserByClerkIdAsync(string clerkUserId);
	/// <summary>
	/// Gets local users matching Clerk subject identifiers.
	/// </summary>
	public Task<List<User>> GetUsersByClerkIdsAsync(IReadOnlyCollection<string> clerkUserIds);

	/// <summary>
	/// Gets an existing local user by Clerk subject identifier, or creates a new one if not found.
	/// </summary>
	/// <param name="clerkUserId">The Clerk user identifier.</param>
	/// <returns>The local user (existing or newly created).</returns>
	public Task<User> GetOrCreateUserByClerkIdAsync(string clerkUserId);
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
}