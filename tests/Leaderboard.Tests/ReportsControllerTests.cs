using Leaderboard.Controllers;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class ReportsControllerTests
{
	[Fact]
	public async Task GetTopPlayersReport_WhenDateRangeIsInvalid_ReturnsBadRequest()
	{
		var repository = new Mock<IScoreRepository>();
		var controller = new ReportsController(repository.Object, NullLogger<ReportsController>.Instance);

		var result = await controller.GetTopPlayersReport(DateTime.Parse("2026-01-02"), DateTime.Parse("2026-01-01"));

		Assert.IsType<BadRequestObjectResult>(result);
		repository.Verify(item => item.GetTopPlayersAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()), Times.Never);
	}

	[Fact]
	public async Task GetTopPlayersReport_WithResults_ReturnsMappedReport()
	{
		var repository = new Mock<IScoreRepository>();
		repository.Setup(item => item.GetTopPlayersAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), 5))
			.ReturnsAsync([new LeaderboardEntry { UserId = 4, UserName = "Ryu", Score = 9000 }]);
		var controller = new ReportsController(repository.Object, NullLogger<ReportsController>.Instance);
		var start = DateTime.Parse("2026-01-01");
		var end = DateTime.Parse("2026-01-31");

		var result = await controller.GetTopPlayersReport(start, end, 5);

		var response = Assert.IsType<OkObjectResult>(result);
		var report = response.Value!;
		Assert.Equal(start, report.GetType().GetProperty("start_date")!.GetValue(report));
		var players = Assert.IsAssignableFrom<IEnumerable<object>>(report.GetType().GetProperty("top_players")!.GetValue(report));
		Assert.Single(players);
	}
}