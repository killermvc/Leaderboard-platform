using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Models;

/// <summary>
/// Represents an API key that can be used to authenticate requests to the leaderboard API.
/// </summary>
public class ApiKey
{
	/// <summary>
	/// The unique identifier for the API key.
	/// </summary>
	[Key]
    public int Id { get; set; }
	/// <summary>
	/// The name of the API key, which can be used to identify it.
	/// </summary>
    public string Name { get; set; } = null!;
	/// <summary>
	/// The SHA-256 hash of the API key value. This is used for secure storage and comparison of API keys.
	/// </summary>
    public string KeyHash { get; set; } = null!;
	/// <summary>
	/// The ID of the game that this API key is associated with.
	/// This allows for game-scoped API keys, where each game can have its own set of API keys.
	/// </summary>
    public int GameId { get; set; }
	/// <summary>
	/// The game that this API key is associated with.
	/// This is a navigation property that allows for easy access to the related Game entity.
	/// </summary>
    public Game Game { get; set; } = null!;
	/// <summary>
	/// The ID of the user who created this API key. This allows for tracking which user created which API keys, and can be used for auditing and management purposes.
	/// </summary>
    public int UserId { get; set; }
	/// <summary>
	/// The permissions that are granted to this API key.
	/// </summary>
    public ApiKeyPermissions Permissions { get; set; }
	/// <summary>
	/// The timestamp of the last time this API key was used.
	/// This can be used for monitoring and auditing purposes, to see when an API key was last utilized.
	/// </summary>
	public DateTime? LastUsedAt { get; set; }
	/// <summary>
	/// The timestamp of when this API key was created.
	/// This is automatically set when the API key is created, and can be used for auditing
	/// </summary>
    public DateTime CreatedAt { get; set; }
	/// <summary>
	/// The timestamp of when this API key will expire. If this is null, the API key does not expire.
	/// </summary>
    public DateTime? ExpiresAt { get; set; }
	/// <summary>
	/// The timestamp of when this API key was revoked. If this is null, the API key has not been revoked.
	/// </summary>
	public DateTime? RevokedAt { get; set; }
	/// <summary>
	/// Indicates whether this API key has been revoked.This is a convenience property that checks if the RevokedAt timestamp is set.
	/// </summary>
    public bool IsRevoked { get { return RevokedAt.HasValue; } }

}
