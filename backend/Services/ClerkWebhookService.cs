using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Leaderboard.Models;
using Leaderboard.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

#pragma warning disable CS1591

namespace Leaderboard.Services;

public sealed class ClerkWebhookService(
	AppDbContext dbContext,
	IOptions<ClerkOptions> options) : IClerkWebhookService
{
	private static readonly TimeSpan MaximumTimestampAge = TimeSpan.FromMinutes(5);

	public async Task<ClerkWebhookResult> ProcessAsync(
		string payload,
		string eventId,
		string timestamp,
		string signature,
		CancellationToken cancellationToken = default)
	{
		if (!IsValidSignature(payload, eventId, timestamp, signature))
		{
			return ClerkWebhookResult.InvalidSignature;
		}

		using JsonDocument document = JsonDocument.Parse(payload);
		if (!document.RootElement.TryGetProperty("type", out JsonElement typeElement) ||
			!document.RootElement.TryGetProperty("data", out JsonElement dataElement) ||
			!dataElement.TryGetProperty("id", out JsonElement idElement))
		{
			return ClerkWebhookResult.InvalidPayload;
		}

		string eventType = typeElement.GetString() ?? string.Empty;
		string clerkUserId = idElement.GetString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(clerkUserId))
		{
			return ClerkWebhookResult.InvalidPayload;
		}

		if (await dbContext.ClerkWebhookEvents.AnyAsync(item => item.Id == eventId, cancellationToken))
		{
			return ClerkWebhookResult.AlreadyProcessed;
		}

		if (eventType is "user.created" or "user.updated" or "user.deleted")
		{
			User? user = await dbContext.Users
				.Include(item => item.UserRoles)
				.FirstOrDefaultAsync(item => item.ClerkUserId == clerkUserId, cancellationToken);

			if (eventType == "user.deleted")
			{
				if (user is not null)
				{
					user.IsActive = false;
					user.ProfileImageUrl = null;
					dbContext.UserRoles.RemoveRange(user.UserRoles);
					List<GameModerator> moderators = await dbContext.GameModerators
						.Where(item => item.UserId == user.Id)
						.ToListAsync(cancellationToken);
					dbContext.GameModerators.RemoveRange(moderators);
					List<Game> ownedGames = await dbContext.Games
						.Where(item => item.OwnerId == user.Id)
						.ToListAsync(cancellationToken);
					foreach (Game game in ownedGames)
					{
						game.OwnerId = null;
					}
				}
			}
			else
			{
				if (TryExtractImageUrl(dataElement, out string? imageUrl))
				{
					if (user is null)
					{
						dbContext.Users.Add(new User { ClerkUserId = clerkUserId, ProfileImageUrl = imageUrl });
					}
					else if (imageUrl != user.ProfileImageUrl)
					{
						user.ProfileImageUrl = imageUrl;
					}
				}
				else if (user is null)
				{
					dbContext.Users.Add(new User { ClerkUserId = clerkUserId });
				}
			}
		}

		dbContext.ClerkWebhookEvents.Add(new ClerkWebhookEvent { Id = eventId });
		await dbContext.SaveChangesAsync(cancellationToken);
		return ClerkWebhookResult.Processed;
	}

	/// <summary>
	/// Extracts the profile image URL from a Clerk user webhook payload ("image_url").
	/// Returns false when the payload carries no image property, leaving stored value untouched.
	/// A present-but-empty value yields null (image removed).
	/// </summary>
	private static bool TryExtractImageUrl(JsonElement dataElement, out string? imageUrl)
	{
		if ((dataElement.TryGetProperty("image_url", out JsonElement imageElement) ||
			dataElement.TryGetProperty("profile_image_url", out imageElement)) &&
			imageElement.ValueKind == JsonValueKind.String)
		{
			string? raw = imageElement.GetString();
			imageUrl = string.IsNullOrWhiteSpace(raw) ? null : raw;
			return true;
		}

		imageUrl = null;
		return false;
	}

	private bool IsValidSignature(string payload, string eventId, string timestamp, string signature)
	{
		if (string.IsNullOrWhiteSpace(options.Value.WebhookSecret) ||
			!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTimestamp))
		{
			return false;
		}

		DateTimeOffset receivedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
		if (DateTimeOffset.UtcNow - receivedAt > MaximumTimestampAge || receivedAt - DateTimeOffset.UtcNow > MaximumTimestampAge)
		{
			return false;
		}

		string secret = options.Value.WebhookSecret.StartsWith("whsec_", StringComparison.Ordinal)
			? options.Value.WebhookSecret[6..]
			: options.Value.WebhookSecret;
		byte[] key;
		try
		{
			key = Convert.FromBase64String(secret);
		}
		catch (FormatException)
		{
			return false;
		}

		byte[] expected = HMACSHA256.HashData(
			key,
			Encoding.UTF8.GetBytes($"{eventId}.{timestamp}.{payload}"));
		string expectedSignature = Convert.ToBase64String(expected);

		return signature
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(value => value.Split(',', 2))
			.Where(parts => parts.Length == 2 && parts[0] == "v1")
			.Any(parts => CryptographicOperations.FixedTimeEquals(
				Encoding.UTF8.GetBytes(parts[1]),
				Encoding.UTF8.GetBytes(expectedSignature)));
	}
}

#pragma warning restore CS1591
