using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Leaderboard.Services;

public class ApiKeyService
{
    public string GenerateApiKey(int length = 32)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(length);
        return WebEncoders.Base64UrlEncode(bytes);
	}
}
