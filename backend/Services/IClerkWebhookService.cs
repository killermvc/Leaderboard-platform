namespace Leaderboard.Services;

#pragma warning disable CS1591

public interface IClerkWebhookService
{
	Task<ClerkWebhookResult> ProcessAsync(
		string payload,
		string eventId,
		string timestamp,
		string signature,
		CancellationToken cancellationToken = default);
}

public enum ClerkWebhookResult
{
	Processed,
	AlreadyProcessed,
	InvalidSignature,
	InvalidPayload
}

#pragma warning restore CS1591
