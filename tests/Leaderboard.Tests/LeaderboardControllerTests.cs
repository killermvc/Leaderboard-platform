using System.Security.Claims;
using Leaderboard.Controllers;
using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class LeaderboardControllerTests
{
    [Fact]
    public async Task Get_returns_board_dto_for_public_read()
    {
        var repository = new Mock<ILeaderboardRepository>();
        repository.Setup(item => item.GetByIdAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Leaderboard.Models.Leaderboard
            {
                Id = 3, GameId = 7, Name = "Ranked", IsActive = false
            });
        var controller = CreateController(repository.Object);

        var result = await controller.Get(7, 3);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<LeaderboardDto>(response.Value);
        Assert.Equal("Ranked", dto.Name);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public async Task Create_without_management_permission_returns_forbidden()
    {
        var repository = new Mock<ILeaderboardRepository>();
        repository.Setup(item => item.CanManageAsync(7, 12, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = CreateController(repository.Object, 12);

        var result = await controller.Create(7, new CreateLeaderboardRequest { Name = "Ranked" });

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Delete_scored_board_returns_conflict()
    {
        var repository = new Mock<ILeaderboardRepository>();
        repository.Setup(item => item.DeleteAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LeaderboardDeleteResult.HasScores);
        var controller = CreateController(repository.Object, 10, "Admin");

        var result = await controller.Delete(7, 3);

        Assert.IsType<ConflictObjectResult>(result);
    }

    private static LeaderboardController CreateController(
        ILeaderboardRepository repository, int? userId = null, string? role = null)
    {
        var controller = new LeaderboardController(repository);
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Name, userId.Value.ToString()));
        }
        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
        return controller;
    }
}
