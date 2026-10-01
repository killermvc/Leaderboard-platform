using Leaderboard.Models;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Repositories;

/// <summary>
/// Repository for managing users in the database.
/// </summary>
public class UserRepository(AppDbContext context) : IUserRepository
{
    private readonly AppDbContext _context = context;

	/// <summary>
	/// Retrieves a user by their username from the database.
	/// </summary>
	/// <param name="username">The username of the user to retrieve.</param>
	/// <returns>The user with the specified username, or null if not found.</returns>
    public async Task<User?> GetUserByNameAsync(string username)
	{
		return await _context.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.FirstOrDefaultAsync(u => u.Username == username);
	}

	/// <summary>
	/// Retrieves a user by their ID from the database.
	/// </summary>
	/// <param name="id">The ID of the user to retrieve.</param>
	/// <returns>The user with the specified ID, or null if not found.</returns>

	public async Task<User?> GetUserByIdAsync(int id)
	{
		return await _context.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.FirstOrDefaultAsync(u => u.Id == id);
	}

	/// <summary>
	/// Retrieves a user by their Clerk user ID from the database.
	/// </summary>
	/// <param name="clerkUserId">The Clerk user ID of the user to retrieve.</param>
	/// <returns>The user with the specified Clerk user ID, or null if not found.</returns>
	public async Task<User?> GetUserByClerkIdAsync(string clerkUserId)
	{
		return await _context.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);
	}

	/// <summary>
	/// Adds a new user to the database asynchronously.
	/// </summary>
	/// <param name="user">The user entity to add.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddUserAsync(User user)
	{
		await _context.Users.AddAsync(user);
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Updates an existing user in the database asynchronously.
	/// </summary>
	/// <param name="user">The user entity to update.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task UpdateUserAsync(User user)
	{
		_context.Users.Update(user);
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Adds a role to a user in the database asynchronously.
	/// </summary>
	/// <param name="user">The user to add the role to.</param>
	/// <param name="role">The role to add.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task AddRoleToUserAsync(User user, Role role)
	{
		var userRole = new UserRole
		{
			UserId = user.Id,
			RoleId = role.Id
		};
		await _context.UserRoles.AddAsync(userRole);
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Searches for users in the database asynchronously.
	/// </summary>
	/// <param name="query">The search query.</param>
	/// <param name="limit">The maximum number of results to return.</param>
	/// <returns>A list of users matching the search query.</returns>
	public async Task<List<User>> SearchUsersAsync(string query, int limit)
	{
		return await _context.Users
			.AsNoTracking()
			.Where(u => EF.Functions.Like(u.Username, $"%{query}%"))
			.Take(limit)
			.ToListAsync();
	}
}