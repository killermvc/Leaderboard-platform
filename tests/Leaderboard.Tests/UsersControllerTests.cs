using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Leaderboard.Controllers;
using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task Search_ReturnsOnlyLocallyKnownUsersWithoutCreating()
    {
        // Arrange
        var clerkDirectoryMock = new Mock<IClerkDirectoryService>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var scoreRepositoryMock = new Mock<IScoreRepository>();

        // Clerk returns 2 users
        clerkDirectoryMock
            .Setup(x => x.SearchUsersAsync("alice", 20, default))
            .ReturnsAsync(new List<ClerkDirectoryUser>
            {
                new ClerkDirectoryUser("clerk_1", "Alice"),
                new ClerkDirectoryUser("clerk_2", "Bob")
            });

        // Only clerk_1 exists locally; clerk_2 must NOT be created on search
        userRepositoryMock
            .Setup(x => x.GetUsersByClerkIdsAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new List<User>
            {
                new User { Id = 1, ClerkUserId = "clerk_1", IsActive = true, CreatedAt = DateTime.UtcNow }
            });

        scoreRepositoryMock
            .Setup(x => x.GetScoresByUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Score>());

        var controller = new UsersController(
            clerkDirectoryMock.Object,
            userRepositoryMock.Object,
            scoreRepositoryMock.Object);

        // Act
        var result = await controller.Search("alice", 20, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
        var userList = users.ToList();

        Assert.Single(userList);
        Assert.Equal("Alice", userList[0].Username);
        userRepositoryMock.Verify(x => x.GetOrCreateUserByClerkIdAsync(It.IsAny<string>()), Times.Never);
        userRepositoryMock.Verify(x => x.AddUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Search_SkipsInactiveUsers()
    {
        // Arrange
        var clerkDirectoryMock = new Mock<IClerkDirectoryService>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var scoreRepositoryMock = new Mock<IScoreRepository>();

        // Clerk returns 2 users (one deleted/inactive)
        clerkDirectoryMock
            .Setup(x => x.SearchUsersAsync("test", 20, default))
            .ReturnsAsync(new List<ClerkDirectoryUser>
            {
                new ClerkDirectoryUser("clerk_active", "Active User"),
                new ClerkDirectoryUser("clerk_inactive", "Inactive User")
            });

        // Both users exist locally, but one is inactive
        userRepositoryMock
            .Setup(x => x.GetUsersByClerkIdsAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new List<User>
            {
                new User { Id = 1, ClerkUserId = "clerk_active", IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 2, ClerkUserId = "clerk_inactive", IsActive = false, CreatedAt = DateTime.UtcNow }
            });

        scoreRepositoryMock
            .Setup(x => x.GetScoresByUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Score>());

        var controller = new UsersController(
            clerkDirectoryMock.Object,
            userRepositoryMock.Object,
            scoreRepositoryMock.Object);

        // Act
        var result = await controller.Search("test", 20, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var users = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
        var userList = users.ToList();

        // Should only return active user
        Assert.Single(userList);
        Assert.Equal("Active User", userList[0].Username);
    }

    [Fact]
    public void Search_RequiresAuthorization()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.Search));
        var hasAuthorize = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true).Length > 0;
        Assert.True(hasAuthorize);
    }
}
