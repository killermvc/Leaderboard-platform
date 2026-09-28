using Leaderboard.Models;

namespace Leaderboard.Dtos;

/// <summary>Request to create an API key.</summary>
public class CreateApiKeyRequest
{
    /// <summary>The API key name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The associated game identifier.</summary>
    public int GameId { get; set; }
    /// <summary>The permissions granted to the API key.</summary>
    public ApiKeyPermissions? Permissions { get; set; }
    /// <summary>The API key expiration time.</summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>Represents an API key.</summary>
public class ApiKeyDto
{
    /// <summary>The API key identifier.</summary>
    public int Id { get; set; }
    /// <summary>The API key name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The creation time.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The last usage time.</summary>
    public DateTime? LastUsedAt { get; set; }
    /// <summary>The expiration time.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>The revocation time.</summary>
    public DateTime? RevokedAt { get; set; }
}

/// <summary>Represents an API key returned immediately after creation.</summary>
public class CreatedApiKeyDto
{
    /// <summary>The API key identifier.</summary>
    public int Id { get; set; }
    /// <summary>The API key name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The API key secret.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>The creation time.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>The expiration time.</summary>
    public DateTime? ExpiresAt { get; set; }
}