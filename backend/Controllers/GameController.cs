using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
		public class GameController(IGameRepository gameRepository, IUserRepository userRepository) : ControllerBase
    {
		private readonly IGameRepository _gameRepository = gameRepository;
		private readonly IUserRepository _userRepository = userRepository;

		/// <summary>
		/// Creates a new game in the system. Only accessible by users with the "Admin" role.
		/// </summary>
		// POST: api/Games
		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<GameDto>> PostGame(GameDto gameDto)
		{
			if (gameDto.OwnerId.HasValue && await _userRepository.GetUserByIdAsync(gameDto.OwnerId.Value) == null)
			{
				return BadRequest("Owner not found.");
			}

			var game = new Game
			{
				Name = gameDto.Name,
				Description = gameDto.Description,
				ImageUrl = gameDto.ImageUrl,
				OwnerId = gameDto.OwnerId,
				SubmitsAllowed = gameDto.IsSubmitAllowed
			};
			await _gameRepository.AddAsync(game);
			var resultDto = ToDto(game);
			return CreatedAtAction("GetGame", new { id = game.Id }, resultDto);
		}

		/// <summary>
		/// Updates a game. Administrators and the game's owner can manage it.
		/// </summary>
		[HttpPut("{id}")]
		[Authorize]
		public async Task<ActionResult<GameDto>> UpdateGame(int id, GameDto gameDto)
		{
			var game = await _gameRepository.GetGameByIdAsync(id);
			if (game == null)
			{
				return NotFound();
			}

			if (!CanManageGame(game))
			{
				return Forbid();
			}

			game.Name = gameDto.Name;
			game.Description = gameDto.Description;
			game.ImageUrl = gameDto.ImageUrl;
			game.SubmitsAllowed = gameDto.IsSubmitAllowed;
			if (User.IsInRole("Admin") && gameDto.OwnerId != game.OwnerId)
			{
				if (gameDto.OwnerId.HasValue && await _userRepository.GetUserByIdAsync(gameDto.OwnerId.Value) == null)
				{
					return BadRequest("Owner not found.");
				}

				game.OwnerId = gameDto.OwnerId;
			}
			await _gameRepository.UpdateAsync(game);
			return Ok(ToDto(game));
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
			return ToDto(game);
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
					OwnerId = ((Game)g).OwnerId,
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
				OwnerId = g.OwnerId,
				IsSubmitAllowed = g.SubmitsAllowed
			});

			return Ok(gameDtos);
		}

		private bool CanManageGame(Game game)
		{
			if (User.IsInRole("Admin"))
			{
				return true;
			}

			return int.TryParse(User.FindFirstValue(ClaimTypes.Name), out var userId) && game.OwnerId == userId;
		}

		private static GameDto ToDto(Game game) => new()
		{
			Id = game.Id,
			Name = game.Name,
			Description = game.Description,
			ImageUrl = game.ImageUrl,
			OwnerId = game.OwnerId,
			IsSubmitAllowed = game.SubmitsAllowed
		};
	}
}
