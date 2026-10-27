namespace Leaderboard.Services;

/// <summary>
/// Defines a service for processing Clerk webhook events.
/// </summary>
public interface IClerkWebhookService
{
	/// <summary>
	/// Processes a Clerk webhook event.
	/// </summary>
	/// <param name="payload">The JSON payload of the webhook event.</param>
	/// <param name="eventId">The ID of the webhook event.</param>
	/// <param name="timestamp">The timestamp of the webhook event.</param>
	/// <param name="signature">The signature of the webhook event.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>The result of the processing operation.</returns>
	Task<ClerkWebhookResult> ProcessAsync(
		string payload,
		string eventId,
		string timestamp,
		string signature,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Enumerates the possible results of processing a Clerk webhook event.
/// </summary>
public enum ClerkWebhookResult
{
	/// <summary>
	/// The webhook event was successfully processed and stored in the database.
	/// </summary>
	Processed,
	/// <summary>
	/// The webhook event has already been processed and stored in the database.
	/// </summary>
	AlreadyProcessed,
	/// <summary>
	/// The webhook event was rejected due to an invalid signature, indicating that the request may not have originated from Clerk or has been tampered with.
	/// </summary>
	InvalidSignature,
	/// <summary>
	/// The webhook event was rejected due to an invalid payload, indicating that the request does not conform to the expected structure or is missing required information.
	/// </summary>
	InvalidPayload
}