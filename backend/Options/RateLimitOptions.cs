namespace Leaderboard.Options;

public sealed class RateLimitOptions
{
	public ScoreSubmissionRateLimitOptions ScoreSubmission { get; set; } = new();
}

public sealed class ScoreSubmissionRateLimitOptions
{
	public int IpCapacity { get; set; } = 30;
	public double IpRefillPerSecond { get; set; } = 1;
	public int ApiKeyCapacity { get; set; } = 120;
	public double ApiKeyRefillPerSecond { get; set; } = 4;
	public int StateTtlSeconds { get; set; } = 300;
}