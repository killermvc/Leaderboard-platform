namespace Leaderboard.Models;

[Flags]
public enum ApiKeyPermissions
{
    None = 0,
    SubmitScores = 1 << 0,
    ReadScores = 1 << 1,
    ReadLeaderboard = 1 << 2
}
