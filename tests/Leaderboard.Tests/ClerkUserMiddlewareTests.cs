using System.Security.Claims;
using Leaderboard.Middleware;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class ClerkUserMiddlewareTests
{
	[Fact]
	public async Task MappedNameIdentifierClaim_CreatesLocalUserAndMapsIdentity()
	{
		var users = new Mock<IUserRepository>();
		users.Setup(item => item.GetUserByClerkIdAsync("user_123"))
			.ReturnsAsync((User?)null);
		users.Setup(item => item.AddUserAsync(It.IsAny<User>()))
			.Callback<User>(user => user.Id = 42)
			.Returns(Task.CompletedTask);

		var context = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(
				[new Claim(ClaimTypes.NameIdentifier, "user_123")],
				"Bearer"))
		};
		var middleware = new ClerkUserMiddleware(_ => Task.CompletedTask);

		await middleware.InvokeAsync(context, users.Object);

		users.Verify(item => item.AddUserAsync(It.Is<User>(user =>
			user.ClerkUserId == "user_123" && user.Username == "user_123")), Times.Once);
		Assert.Equal("42", context.User.FindFirstValue(ClaimTypes.Name));
	}
}
