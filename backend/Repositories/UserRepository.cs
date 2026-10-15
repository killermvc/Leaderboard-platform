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
	/// Gets local users matching Clerk subject identifiers.
	/// </summary>
	public async Task<List<User>> GetUsersByClerkIdsAsync(IReadOnlyCollection<string> clerkUserIds)
	{
		return await _context.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.Where(u => u.ClerkUserId != null && clerkUserIds.Contains(u.ClerkUserId))
			.ToListAsync();
	}

	/// <summary>
	/// Gets an existing local user by Clerk subject identifier, or creates a new one if not found.
	/// </summary>
	public async Task<User> GetOrCreateUserByClerkIdAsync(string clerkUserId)
	{
		User? user = await _context.Users
			.Include(u => u.UserRoles)
			.ThenInclude(ur => ur.Role)
			.FirstOrDefaultAsync(u => u.ClerkUserId == clerkUserId);

		if (user is null)
		{
			user = new User { ClerkUserId = clerkUserId };
			_context.Users.Add(user);
			await _context.SaveChangesAsync();
		}

		return user;
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
		Role persisted = role.Id == 0
			? await GetOrCreateRoleByNameAsync(role.Name)
			: role;

		bool alreadyHas = await _context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == persisted.Id);
		if (alreadyHas)
		{
			return;
		}

		var userRole = new UserRole
		{
			UserId = user.Id,
			RoleId = persisted.Id
		};
		await _context.UserRoles.AddAsync(userRole);
		await _context.SaveChangesAsync();
	}

	/// <summary>
	/// Gets an existing role by name, or creates it when missing.
	/// </summary>
	public async Task<Role> GetOrCreateRoleByNameAsync(string roleName)
	{
		Role? role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
		if (role is not null)
		{
			return role;
		}

		role = new Role { Name = roleName };
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();
		return role;
	}

	/// <summary>
	/// Returns true when at least one active user holds the named role.
	/// </summary>
	public Task<bool> AnyUserInRoleAsync(string roleName)
	{
		return _context.UserRoles
			.Include(ur => ur.Role)
			.Include(ur => ur.User)
			.AnyAsync(ur => ur.Role != null && ur.Role.Name == roleName && ur.User != null && ur.User.IsActive);
	}

}