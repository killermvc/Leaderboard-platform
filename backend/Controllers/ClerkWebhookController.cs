using System.Text;
using Leaderboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Leaderboard.Controllers;

/// <summary>
/// Controller for handling Clerk webhooks. This controller receives webhook events from Clerk, validates the signature, and processes the payload accordingly.
/// </summary>
[ApiController]
[Route("api/webhooks/clerk")]
public sealed class ClerkWebhookController(IClerkWebhookService webhookService) : ControllerBase
{
	/// <summary>
	/// Receives a webhook event from Clerk. The method reads the request body, extracts necessary headers for signature validation, and processes the payload using the injected webhook service. It returns appropriate HTTP responses based on the result of the processing.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns></returns>
	[HttpPost]
	[AllowAnonymous]
	public async Task<IActionResult> Receive(CancellationToken cancellationToken)
	{
		using StreamReader reader = new(Request.Body, Encoding.UTF8);
		string payload = await reader.ReadToEndAsync(cancellationToken);
		string eventId = Request.Headers["svix-id"].ToString();
		string timestamp = Request.Headers["svix-timestamp"].ToString();
		string signature = Request.Headers["svix-signature"].ToString();

		ClerkWebhookResult result = await webhookService.ProcessAsync(
			payload,
			eventId,
			timestamp,
			signature,
			cancellationToken);

		return result switch
		{
			ClerkWebhookResult.InvalidSignature => Unauthorized(),
			ClerkWebhookResult.InvalidPayload => BadRequest(),
			_ => Ok()
		};
	}
}