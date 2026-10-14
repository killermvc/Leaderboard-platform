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
    public async Task Search_ReturnsUsersFromClerkDirectoryEvenIfNotInLocalDb()
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

        // GetOrCreateUserByClerkIdAsync returns existing user for clerk_1, creates new for clerk_2
        userRepositoryMock
            .Setup(x => x.GetOrCreateUserByClerkIdAsync("clerk_1"))
            .ReturnsAsync(new User { Id = 1, ClerkUserId = "clerk_1", IsActive = true, CreatedAt = DateTime.UtcNow });

        userRepositoryMock
            .Setup(x => x.GetOrCreateUserByClerkIdAsync("clerk_2"))
            .ReturnsAsync(new User { Id = 2, ClerkUserId = "clerk_2", IsActive = true, CreatedAt = DateTime.UtcNow });

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

        // Should return BOTH users from Clerk, not just the one in local DB
        Assert.Equal(2, userList.Count);
        Assert.Contains(userList, u => u.Username == "Alice");
        Assert.Contains(userList, u => u.Username == "Bob");
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
            .Setup(x => x.GetOrCreateUserByClerkIdAsync("clerk_active"))
            .ReturnsAsync(new User { Id = 1, ClerkUserId = "clerk_active", IsActive = true, CreatedAt = DateTime.UtcNow });

        userRepositoryMock
            .Setup(x => x.GetOrCreateUserByClerkIdAsync("clerk_inactive"))
            .ReturnsAsync(new User { Id = 2, ClerkUserId = "clerk_inactive", IsActive = false, CreatedAt = DateTime.UtcNow });

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
}