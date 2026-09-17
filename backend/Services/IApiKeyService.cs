namespace Leaderboard.Services;

public interface IApiKeyService
{
    public string GenerateApiKey(int length = 32);
}
