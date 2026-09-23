using System.ComponentModel.DataAnnotations;

namespace Leaderboard.Models;

public class ApiKey
{
	[Key]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
	public string KeyPrefix { get; set; } = null!;
    public string KeyHash { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public int UserId { get; set; }

    public ApiKeyPermissions Permissions { get; set; }

	public DateTime? LastUsedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
	public DateTime? RevokedAt { get; set; }
    public bool IsRevoked { get { return RevokedAt.HasValue; } }

}
