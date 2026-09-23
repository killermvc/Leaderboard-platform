using Leaderboard.Models;

namespace Leaderboard.Dtos;

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public int GameId { get; set; }
    public ApiKeyPermissions? Permissions { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class CreatedApiKeyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}