using System.Security.Claims;
using Leaderboard.Controllers;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BCryptClass = BCrypt.Net.BCrypt;

namespace Leaderboard.Tests;

public class AuthControllerTests
{
	[Fact]
	public async Task Register_WithNewUsername_CreatesUser()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.GetUserByNameAsync("Ryu")).ReturnsAsync((User?)null);
		var controller = new AuthController(users.Object, Mock.Of<IJwtService>(), Mock.Of<IScoreRepository>());

		var result = await controller.Register(new CredentialsRequest { UserName = "Ryu", Password = "secret" });

		Assert.IsType<OkObjectResult>(result);
		users.Verify(item => item.AddUserAsync(It.Is<User>(user => user.Username == "Ryu")), Times.Once);
	}

	[Fact]
	public async Task Register_WithExistingUsername_ReturnsBadRequest()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.GetUserByNameAsync("Ryu")).ReturnsAsync(new User { Username = "Ryu" });
		var controller = new AuthController(users.Object, Mock.Of<IJwtService>(), Mock.Of<IScoreRepository>());

		var result = await controller.Register(new CredentialsRequest { UserName = "Ryu", Password = "secret" });

		Assert.IsType<BadRequestObjectResult>(result);
		users.Verify(item => item.AddUserAsync(It.IsAny<User>()), Times.Never);
	}

	[Fact]
	public async Task GetCurrentUser_WithValidClaim_ReturnsUserSummary()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.GetUserByIdAsync(4)).ReturnsAsync(new User { Id = 4, Username = "Ryu" });
		var controller = new AuthController(users.Object, Mock.Of<IJwtService>(), Mock.Of<IScoreRepository>());
		controller.ControllerContext = CreateContext("4");

		var result = await controller.GetCurrentUser();

		var response = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(4, response.Value!.GetType().GetProperty("Id")!.GetValue(response.Value));
		Assert.Equal("Ryu", response.Value.GetType().GetProperty("Username")!.GetValue(response.Value));
	}

	private static ControllerContext CreateContext(string userId) => new()
	{
		HttpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, userId)], "test"))
		}
	};
}