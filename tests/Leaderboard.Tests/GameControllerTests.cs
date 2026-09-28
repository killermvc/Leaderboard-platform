using Leaderboard.Controllers;
using Leaderboard.Dtos;
using Leaderboard.Models;
using Leaderboard.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Leaderboard.Tests;

public class GameControllerTests
{
	[Fact]
	public async Task GetGame_WhenGameExists_ReturnsMappedDto()
	{
		var repository = new Mock<IGameRepository>();
		repository.Setup(item => item.GetGameByIdAsync(7)).ReturnsAsync(new Game
		{
			Id = 7,
			Name = "Arcade",
			Description = "Classic game",
			ImageUrl = "https://example.test/arcade.png"
		});
		var controller = new GameController(repository.Object);

		var result = await controller.GetGame(7);

		var dto = Assert.IsType<GameDto>(result.Value);
		Assert.Equal(7, dto.Id);
		Assert.Equal("Arcade", dto.Name);
		Assert.Equal("https://example.test/arcade.png", dto.ImageUrl);
	}

	[Fact]
	public async Task GetGame_WhenGameDoesNotExist_ReturnsNotFound()
	{
		var repository = new Mock<IGameRepository>();
		repository.Setup(item => item.GetGameByIdAsync(7)).ReturnsAsync((Game?)null);
		var controller = new GameController(repository.Object);

		var result = await controller.GetGame(7);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task PostGame_AddsGameAndReturnsCreatedDto()
	{
		var repository = new Mock<IGameRepository>();
		repository.Setup(item => item.AddAsync(It.IsAny<Game>()))
			.Callback<Game>(game => game.Id = 12)
			.Returns(Task.CompletedTask);
		var controller = new GameController(repository.Object);

		var result = await controller.PostGame(new GameDto { Name = "Arcade", Description = "Classic game" });

		var created = Assert.IsType<CreatedAtActionResult>(result.Result);
		var dto = Assert.IsType<GameDto>(created.Value);
		Assert.Equal(12, dto.Id);
		Assert.Equal("Arcade", dto.Name);
		repository.Verify(item => item.AddAsync(It.Is<Game>(game => game.Name == "Arcade")), Times.Once);
	}
}