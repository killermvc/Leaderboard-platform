namespace Leaderboard.Options;

/// <summary>
/// Options for configuring rate limiting in the application.
/// </summary>
public sealed class RateLimitOptions
{
	/// <summary>
	/// Options for rate limiting score submissions.
	/// </summary>
	public ScoreSubmissionRateLimitOptions ScoreSubmission { get; set; } = new();
}

/// <summary>
/// Options for rate limiting score submissions.
/// </summary>
public sealed class ScoreSubmissionRateLimitOptions
{
	/// <summary>
	/// The maximum number of requests allowed from a single IP address within the specified time window.
	/// </summary>
	public int IpCapacity { get; set; } = 30;
	/// <summary>
	/// The rate at which tokens are replenished for a single IP address, in tokens per second. This determines how quickly an IP address can regain the ability to make requests after reaching the capacity limit.
	/// </summary>
	public double IpRefillPerSecond { get; set; } = 1;
	/// <summary>
	/// The maximum number of requests allowed using a single API key within the specified time window.
	/// </summary>
	public int ApiKeyCapacity { get; set; } = 120;
	/// <summary>
	/// The rate at which tokens are replenished for a single API key, in tokens per second. This determines how quickly an API key can regain the ability to make requests after reaching the capacity limit.
	/// </summary>
	public double ApiKeyRefillPerSecond { get; set; } = 4;
	/// <summary>
	/// The time-to-live (TTL) for the rate limiting state in seconds. This determines how long the rate limiting data is stored in Redis before it expires and is removed. A longer TTL can help maintain rate limiting state across server restarts, while a shorter TTL can reduce memory usage in Redis.
	/// </summary>
	public int StateTtlSeconds { get; set; } = 300;
}