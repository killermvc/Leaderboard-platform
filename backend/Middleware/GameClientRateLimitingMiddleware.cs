using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

using Leaderboard.Options;

namespace Leaderboard.Middleware;

/// <summary>
/// Middleware that enforces rate limiting on score submission requests, based on the client's IP address and the presented API key.
/// </summary>
public sealed class GameClientRateLimitingMiddleware(
	RequestDelegate next,
	IOptions<RateLimitOptions> options,
	ILogger<GameClientRateLimitingMiddleware> logger)
{
	private readonly RequestDelegate _next = next;
	private readonly ILogger<GameClientRateLimitingMiddleware> _logger = logger;
	private readonly ScoreSubmissionRateLimitOptions _options = options.Value.ScoreSubmission;
	private readonly ConcurrentDictionary<string, TokenBucket> _ipBuckets = new();
	private readonly ConcurrentDictionary<string, TokenBucket> _apiKeyBuckets = new();

	/// <summary>
	/// Processes a request, enforcing rate limits on score submission requests based on the client's IP address and the presented API key.
	/// </summary>
	public async Task InvokeAsync(HttpContext context)
	{
		if (!IsScoreSubmission(context))
		{
			await _next(context);
			return;
		}

		DateTimeOffset now = DateTimeOffset.UtcNow;
		string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		if (!TryConsume(_ipBuckets, ip, _options.IpCapacity, _options.IpRefillPerSecond, now, out TimeSpan ipRetryAfter))
		{
			await RejectAsync(context, ipRetryAfter);
			return;
		}

		string? presentedKey = ApiKeyAuthenticationMiddleware.ExtractPresentedKey(context);
		if (!string.IsNullOrWhiteSpace(presentedKey))
		{
			string keyFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(presentedKey)));
			if (!TryConsume(_apiKeyBuckets, keyFingerprint, _options.ApiKeyCapacity, _options.ApiKeyRefillPerSecond, now, out TimeSpan keyRetryAfter))
			{
				await RejectAsync(context, keyRetryAfter);
				return;
			}
		}

		CleanupExpiredBuckets(_ipBuckets, now);
		CleanupExpiredBuckets(_apiKeyBuckets, now);
		await _next(context);
	}

	private bool TryConsume(
		ConcurrentDictionary<string, TokenBucket> buckets,
		string partition,
		int capacity,
		double refillPerSecond,
		DateTimeOffset now,
		out TimeSpan retryAfter)
	{
		if (capacity <= 0 || refillPerSecond <= 0)
		{
			retryAfter = TimeSpan.FromSeconds(1);
			return false;
		}

		TokenBucket bucket = buckets.GetOrAdd(partition, _ => new TokenBucket(capacity, now));
		bool allowed = bucket.TryConsume(capacity, refillPerSecond, now, out retryAfter);
		return allowed;
	}

	private void CleanupExpiredBuckets(ConcurrentDictionary<string, TokenBucket> buckets, DateTimeOffset now)
	{
		TimeSpan ttl = TimeSpan.FromSeconds(Math.Max(1, _options.StateTtlSeconds));
		foreach (var entry in buckets)
		{
			if (now - entry.Value.LastSeen > ttl)
			{
				buckets.TryRemove(new KeyValuePair<string, TokenBucket>(entry.Key, entry.Value));
			}
		}
	}

	private static bool IsScoreSubmission(HttpContext context)
	{
		return HttpMethods.IsPost(context.Request.Method) &&
			context.Request.Path.Equals("/api/v1/scores/submit", StringComparison.OrdinalIgnoreCase);
	}

	private async Task RejectAsync(HttpContext context, TimeSpan retryAfter)
	{
		int retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
		_logger.LogWarning("Rate limit exceeded for score submission from {IpAddress}.", context.Connection.RemoteIpAddress);
		context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
		context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
		await context.Response.WriteAsJsonAsync(new
		{
			Message = "Too many score submissions. Try again later.",
			RetryAfterSeconds = retryAfterSeconds
		});
	}

	private sealed class TokenBucket(int capacity, DateTimeOffset now)
	{
		private readonly object _gate = new();
		private double _tokens = capacity;
		private DateTimeOffset _lastRefill = now;

		public DateTimeOffset LastSeen { get; private set; } = now;

		public bool TryConsume(int capacity, double refillPerSecond, DateTimeOffset now, out TimeSpan retryAfter)
		{
			lock (_gate)
			{
				TimeSpan elapsed = now - _lastRefill;
				_tokens = Math.Min(capacity, _tokens + elapsed.TotalSeconds * refillPerSecond);
				_lastRefill = now;
				LastSeen = now;

				if (_tokens >= 1)
				{
					_tokens--;
					retryAfter = TimeSpan.Zero;
					return true;
				}

				retryAfter = TimeSpan.FromSeconds((1 - _tokens) / refillPerSecond);
				return false;
			}
		}
	}
}