namespace Leaderboard.Services;

/// <summary>
/// Interface for a service that generates and hashes API keys.
/// </summary>/
public interface IApiKeyService
{
	/// <summary>
	/// Generates a new API key of the specified length using a secure random number generator and encodes it in Base64 URL format.
	/// </summary>
	/// <param name="length">The length of the API key to generate.</param>
	/// <returns>The generated API key.</returns>
    public string GenerateApiKey(int length = 32);
	/// <summary>
	/// Hashes the provided API key using SHA-256 and returns the hash as a lowercase hexadecimal string.
	/// </summary>
	/// <param name="apiKey">The API key to hash.</param>
	/// <returns>The hashed API key.</returns>
    public string HashApiKey(string apiKey);
}