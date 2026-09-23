using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Leaderboard.Services;

public class ApiKeyService : IApiKeyService
{
    public string GenerateApiKey(int length = 32)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(length);
        return WebEncoders.Base64UrlEncode(bytes);
	}

    public string HashApiKey(string apiKey)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}