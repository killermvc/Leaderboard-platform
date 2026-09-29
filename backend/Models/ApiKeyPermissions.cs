namespace Leaderboard.Models;

/// <summary>
/// Defines the permissions that can be granted to an API key.
/// These permissions control what actions the API key is allowed to perform when interacting with the leaderboard API.
/// </summary>
[Flags]
public enum ApiKeyPermissions
{
	/// <summary>
	/// No permissions are granted. The API key cannot perform any actions.
	/// </summary>
    None = 0,
	/// <summary>
	/// Permission to submit scores to the leaderboard.
	/// </summary>
    SubmitScores = 1 << 0,
	/// <summary>
	/// Permission to read scores from the leaderboard.
	/// </summary>
    ReadScores = 1 << 1,
	/// <summary>
	/// Permission to read leaderboard data.
	/// </summary>
    ReadLeaderboard = 1 << 2,
	/// <summary>
	/// Permission to delete scores submitted by a game client.
	/// </summary>
	DeleteScores = 1 << 3
}
