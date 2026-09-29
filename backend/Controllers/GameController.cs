using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Dtos;

namespace Leaderboard.Controllers
{
	/// <summary>
	/// Controller for managing games in the leaderboard system.
	/// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GameController(IGameRepository gameRepository) : ControllerBase
    {
		private readonly IGameRepository _gameRepository = gameRepository;

		/// <summary>
		/// Creates a new game in the system. Only accessible by users with the "Admin" role.
		/// </summary>
		// POST: api/Games
		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<GameDto>> PostGame(GameDto gameDto)
		{
			var game = new Game { Name = gameDto.Name, Description = gameDto.Description, ImageUrl = gameDto.ImageUrl };
			await _gameRepository.AddAsync(game);
			var resultDto = new GameDto { Id = game.Id, Name = game.Name, Description = game.Description, ImageUrl = game.ImageUrl, IsSubmitAllowed = game.SubmitsAllowed };
			return CreatedAtAction("GetGame", new { id = game.Id }, resultDto);
		}

		/// <summary>
		/// Retrieves a game by its ID. Accessible by any authenticated user.
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<GameDto>> GetGame(int id)
		{
			var game = await _gameRepository.GetGameByIdAsync(id);
			if (game == null)
			{
				return NotFound();
			}
			return new GameDto { Id = game.Id, Name = game.Name, Description = game.Description, ImageUrl = game.ImageUrl, IsSubmitAllowed = game.SubmitsAllowed };
		}

		/// <summary>
		/// Retrieves a paginated list of all games. Accessible by any authenticated user.
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<IEnumerable<GameDto>>> GetAllGames(int limit, int offset)
		{
			try
			{
				List<Game> games = await _gameRepository.GetAllGamesAsync();
				var pagedGames = games.Skip(offset).Take(limit);
				var gameDtos = pagedGames.Select(g => new GameDto
				{
					Id = ((Game)g).Id,
					Name = ((Game)g).Name,
					Description = ((Game)g).Description,
					ImageUrl = ((Game)g).ImageUrl,
					IsSubmitAllowed = ((Game)g).SubmitsAllowed
				});
				return Ok(gameDtos);
			}
			catch (Exception)
			{
				return StatusCode(500, "An error occurred while retrieving games.");
			}
		}

		/// <summary>
		/// Retrieves all games that a specific player has participated in. Accessible by any authenticated user.
		/// </summary>
		[HttpGet("player/{playerId}")]
		public async Task<ActionResult<IEnumerable<GameDto>>> GetGamesByPlayer(int playerId)
		{
			var games = await _gameRepository.GetGamesByPlayerIdAsync(playerId);

			var gameDtos = games.Select(g => new GameDto
			{
				Id = g.Id,
				Name = g.Name,
				Description = g.Description,
				ImageUrl = g.ImageUrl,
				IsSubmitAllowed = g.SubmitsAllowed
			});

			return Ok(gameDtos);
		}

	}
}
