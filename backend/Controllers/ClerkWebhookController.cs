using System.Text;
using Leaderboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591

namespace Leaderboard.Controllers;

[ApiController]
[Route("api/webhooks/clerk")]
public sealed class ClerkWebhookController(IClerkWebhookService webhookService) : ControllerBase
{
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

#pragma warning restore CS1591
