using Leaderboard.Controllers;
using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class DisplayNameEnrichmentTests
{
	[Fact]
	public async Task GetScoresByUser_ResolvesDisplayNamesInsteadOfClerkIds()
	{
		var scores = new Mock<IScoreRepository>();
		scores.Setup(r => r.GetScoresByUserAsync(4, 10, 0)).ReturnsAsync(new List<Score>
		{
			new()
			{
				Id = 1,
				User = new User { Id = 4, ClerkUserId = "user_123" },
				Game = new Game { Id = 7, Name = "Arcade", Description = "d", SubmitsAllowed = true },
				Value = 9000,
				Status = ScoreStatus.Approved
			}
		});
		var directory = new Mock<IClerkDirectoryService>();
		directory.Setup(d => d.GetDisplayNamesAsync(
				It.IsAny<IReadOnlyCollection<string>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["user_123"] = "alice"
			});
		var controller = new ScoreController(
			scores.Object,
			Mock.Of<IGameRepository>(),
			Mock.Of<IUserRepository>(),
			directory.Object,
			NullLogger<ScoreController>.Instance);

		var result = await controller.GetScoresByUser(4);

		var ok = Assert.IsType<OkObjectResult>(result);
		var dtos = Assert.IsType<List<ScoreDto>>(ok.Value);
		Assert.Equal("alice", dtos[0].User!.Username);
	}

	[Fact]
	public async Task GetLeaderboard_ResolvesDisplayNamesInsteadOfClerkIds()
	{
		var scores = new Mock<IScoreRepository>();
		scores.Setup(r => r.GetLeaderboardAsync(7, 10)).ReturnsAsync(new List<LeaderboardEntry>
		{
			new() { UserId = 4, UserName = "user_123", Score = 9000 },
			new() { UserId = null, UserName = "guest-player", Score = 100 }
		});
		var directory = new Mock<IClerkDirectoryService>();
		directory.Setup(d => d.GetDisplayNamesAsync(
				It.IsAny<IReadOnlyCollection<string>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["user_123"] = "alice"
			});
		var controller = new ScoreController(
			scores.Object,
			Mock.Of<IGameRepository>(),
			Mock.Of<IUserRepository>(),
			directory.Object,
			NullLogger<ScoreController>.Instance);

		var result = await controller.GetLeaderboard(7, 10);

		var ok = Assert.IsType<OkObjectResult>(result);
		var entries = Assert.IsType<List<LeaderboardEntry>>(ok.Value);
		Assert.Equal("alice", entries[0].UserName);
		Assert.Equal("guest-player", entries[1].UserName);
	}
}
