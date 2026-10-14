using System.Security.Cryptography;
using System.Text;
using Leaderboard.Models;
using Leaderboard.Options;
using Leaderboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Leaderboard.Tests;

public class ClerkWebhookServiceTests
{
	private const string Secret = "whsec_dGVzdC1zZWNyZXQ=";
	private const string RawSecret = "test-secret";

	[Fact]
	public async Task ProcessAsync_WithInvalidSignature_DoesNotChangeDatabase()
	{
		await using var context = CreateContext();
		var service = CreateService(context);

		var result = await service.ProcessAsync(
			Payload("user.created", "user_123"),
			"evt_123",
			Timestamp(),
			"v1,invalid");

		Assert.Equal(ClerkWebhookResult.InvalidSignature, result);
		Assert.Empty(context.Users);
	}

	[Fact]
	public async Task ProcessAsync_WithCreatedUser_IsIdempotent()
	{
		await using var context = CreateContext();
		var service = CreateService(context);
		string payload = Payload("user.created", "user_123");
		string timestamp = Timestamp();
		string signature = Sign("evt_123", timestamp, payload);

		var firstResult = await service.ProcessAsync(payload, "evt_123", timestamp, signature);
		var duplicateResult = await service.ProcessAsync(payload, "evt_123", timestamp, signature);

		Assert.Equal(ClerkWebhookResult.Processed, firstResult);
		Assert.Equal(ClerkWebhookResult.AlreadyProcessed, duplicateResult);
		Assert.Single(context.Users);
		Assert.Equal("user_123", context.Users.Single().ClerkUserId);
	}

	[Fact]
	public async Task ProcessAsync_WithDeletedUser_DeactivatesUserAndPreservesGame()
	{
		await using var context = CreateContext();
		var user = new User { Id = 4, ClerkUserId = "user_123" };
		var role = new Role { Id = 2, Name = "Admin" };
		var game = new Game { Id = 8, Name = "Test Game", Description = "Test", OwnerId = 4 };
		context.Users.Add(user);
		context.Roles.Add(role);
		context.UserRoles.Add(new UserRole { UserId = 4, RoleId = 2 });
		context.Games.Add(game);
		context.GameModerators.Add(new GameModerator { Id = 9, GameId = 8, UserId = 4 });
		await context.SaveChangesAsync();
		var service = CreateService(context);
		string payload = Payload("user.deleted", "user_123");
		string timestamp = Timestamp();

		var result = await service.ProcessAsync(payload, "evt_deleted", timestamp, Sign("evt_deleted", timestamp, payload));

		Assert.Equal(ClerkWebhookResult.Processed, result);
		Assert.True(await context.Users.AnyAsync(item => item.Id == 4 && !item.IsActive));
		Assert.Null(await context.Games.Where(item => item.Id == 8).Select(item => item.OwnerId).SingleAsync());
		Assert.Empty(context.UserRoles);
		Assert.Empty(context.GameModerators);
	}

	[Fact]
	public async Task ProcessAsync_WithCreatedUser_SyncsImageUrl()
	{
		await using var context = CreateContext();
		var service = CreateService(context);
		string payload = PayloadWithImage("user.created", "user_123", "https://img.clerk.com/avatar.png");
		string timestamp = Timestamp();

		var result = await service.ProcessAsync(payload, "evt_img_create", timestamp, Sign("evt_img_create", timestamp, payload));

		Assert.Equal(ClerkWebhookResult.Processed, result);
		Assert.Equal("https://img.clerk.com/avatar.png", context.Users.Single().ProfileImageUrl);
	}

	[Fact]
	public async Task ProcessAsync_WithUpdatedUser_UpdatesImageUrl()
	{
		await using var context = CreateContext();
		context.Users.Add(new User { ClerkUserId = "user_123", ProfileImageUrl = "https://img.clerk.com/old.png" });
		await context.SaveChangesAsync();
		var service = CreateService(context);
		string payload = PayloadWithImage("user.updated", "user_123", "https://img.clerk.com/new.png");
		string timestamp = Timestamp();

		var result = await service.ProcessAsync(payload, "evt_img_update", timestamp, Sign("evt_img_update", timestamp, payload));

		Assert.Equal(ClerkWebhookResult.Processed, result);
		Assert.Equal("https://img.clerk.com/new.png", context.Users.Single().ProfileImageUrl);
	}

	[Fact]
	public async Task ProcessAsync_WithUpdatedUserWithoutImageProperty_KeepsStoredImageUrl()
	{
		await using var context = CreateContext();
		context.Users.Add(new User { ClerkUserId = "user_123", ProfileImageUrl = "https://img.clerk.com/old.png" });
		await context.SaveChangesAsync();
		var service = CreateService(context);
		string payload = Payload("user.updated", "user_123");
		string timestamp = Timestamp();

		var result = await service.ProcessAsync(payload, "evt_img_keep", timestamp, Sign("evt_img_keep", timestamp, payload));

		Assert.Equal(ClerkWebhookResult.Processed, result);
		Assert.Equal("https://img.clerk.com/old.png", context.Users.Single().ProfileImageUrl);
	}

	private static AppDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new AppDbContext(options);
	}

	private static ClerkWebhookService CreateService(AppDbContext context) =>
		new(context, Microsoft.Extensions.Options.Options.Create(new ClerkOptions { WebhookSecret = Secret }));

	private static string Payload(string eventType, string clerkUserId) =>
		$"{{\"type\":\"{eventType}\",\"data\":{{\"id\":\"{clerkUserId}\"}}}}";

	private static string PayloadWithImage(string eventType, string clerkUserId, string? imageUrl) =>
		imageUrl is null
			? $@"{{""type"":""{eventType}"",""data"":{{""id"":""{clerkUserId}"",""image_url"":null}}}}"
			: $@"{{""type"":""{eventType}"",""data"":{{""id"":""{clerkUserId}"",""image_url"":""{imageUrl}""}}}}";

	private static string Timestamp() => TimestampFor(string.Empty);

	private static string TimestampFor(string _) => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

	private static string Sign(string eventId, string timestamp, string payload)
	{
		byte[] hash = HMACSHA256.HashData(
			Encoding.UTF8.GetBytes(RawSecret),
			Encoding.UTF8.GetBytes($"{eventId}.{timestamp}.{payload}"));
		return $"v1,{Convert.ToBase64String(hash)}";
	}
}
