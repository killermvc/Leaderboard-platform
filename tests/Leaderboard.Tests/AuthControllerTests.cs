using System.Security.Claims;
using Leaderboard.Controllers;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class AuthControllerTests
{
	[Fact]
	public async Task GetCurrentUser_WithValidClaim_ReturnsUserSummary()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.GetUserByIdAsync(4)).ReturnsAsync(new User { Id = 4, ClerkUserId = "user_123" });
		var controller = new AuthController(users.Object, Mock.Of<IScoreRepository>(), Mock.Of<IClerkDirectoryService>());
		controller.ControllerContext = CreateContext("4");

		var result = await controller.GetCurrentUser();

		var response = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(4, response.Value!.GetType().GetProperty("Id")!.GetValue(response.Value));
		Assert.Equal("user_123", response.Value.GetType().GetProperty("ClerkUserId")!.GetValue(response.Value));
	}

	[Fact]
	public async Task PromoteToAdmin_WhenNoAdminExists_AllowsAuthenticatedBootstrap()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.AnyUserInRoleAsync("Admin")).ReturnsAsync(false);
		users.Setup(item => item.GetUserByIdAsync(7)).ReturnsAsync(new User { Id = 7, ClerkUserId = "user_7" });
		users.Setup(item => item.GetOrCreateRoleByNameAsync("Admin")).ReturnsAsync(new Role { Id = 1, Name = "Admin" });
		var controller = new AuthController(users.Object, Mock.Of<IScoreRepository>(), Mock.Of<IClerkDirectoryService>());
		controller.ControllerContext = CreateContext("7");

		var result = await controller.PromoteToAdmin(7);

		Assert.IsType<OkObjectResult>(result);
		users.Verify(item => item.AddRoleToUserAsync(It.IsAny<User>(), It.Is<Role>(r => r.Name == "Admin" && r.Id == 1)), Times.Once);
	}

	[Fact]
	public async Task PromoteToAdmin_WhenAdminExists_ForbidsNonAdmin()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.AnyUserInRoleAsync("Admin")).ReturnsAsync(true);
		var controller = new AuthController(users.Object, Mock.Of<IScoreRepository>(), Mock.Of<IClerkDirectoryService>());
		controller.ControllerContext = CreateContext("7");

		var result = await controller.PromoteToAdmin(7);

		Assert.IsType<ForbidResult>(result);
	}

	private static ControllerContext CreateContext(string userId) => new()
	{
		HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, userId)], "test"))
		}
	};
}