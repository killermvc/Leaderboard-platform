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
using Leaderboard.Services;

namespace Leaderboard.Controllers
{
	/// <summary>
	/// Controller for managing games in the leaderboard system.
	/// </summary>
    [Route("api/[controller]")]
    [ApiController]
		public class GameController(IGameRepository gameRepository, IUserRepository userRepository, IClerkDirectoryService clerkDirectoryService) : ControllerBase
    {
		private readonly IGameRepository _gameRepository = gameRepository;
		private readonly IUserRepository _userRepository = userRepository;
		private readonly IClerkDirectoryService _clerkDirectory = clerkDirectoryService;

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
			var resultDto = await ToDtoAsync(game);
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
			return Ok(await ToDtoAsync(game));
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
			return await ToDtoAsync(game);
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
			var displayNames = await GetDisplayNamesAsync(pagedGames.Select(g => ((Game)g).Owner?.ClerkUserId));
			var gameDtos = pagedGames.Select(g => new GameDto
			{
				Id = ((Game)g).Id,
				Name = ((Game)g).Name,
				Description = ((Game)g).Description,
				ImageUrl = ((Game)g).ImageUrl,
				OwnerId = ((Game)g).OwnerId,
				OwnerUsername = DisplayOrId(((Game)g).Owner?.ClerkUserId, displayNames),
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

		var displayNames = await GetDisplayNamesAsync(games.Select(g => g.Owner?.ClerkUserId));
		var gameDtos = games.Select(g => new GameDto
		{
			Id = g.Id,
			Name = g.Name,
			Description = g.Description,
			ImageUrl = g.ImageUrl,
			OwnerId = g.OwnerId,
			OwnerUsername = DisplayOrId(g.Owner?.ClerkUserId, displayNames),
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

		private async Task<GameDto> ToDtoAsync(Game game, CancellationToken cancellationToken = default)
		{
			string? ownerUsername = null;
			if (!string.IsNullOrWhiteSpace(game.Owner?.ClerkUserId))
			{
				ownerUsername = await _clerkDirectory.GetDisplayNameAsync(game.Owner.ClerkUserId!, cancellationToken);
			}

			return new GameDto
			{
				Id = game.Id,
				Name = game.Name,
				Description = game.Description,
				ImageUrl = game.ImageUrl,
				OwnerId = game.OwnerId,
				OwnerUsername = ownerUsername,
				IsSubmitAllowed = game.SubmitsAllowed
			};
		}

		private async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
			IEnumerable<string?> clerkUserIds,
			CancellationToken cancellationToken = default)
		{
			var ids = clerkUserIds
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id!)
				.Distinct(StringComparer.Ordinal)
				.ToArray();
			if (ids.Length == 0)
			{
				return new Dictionary<string, string>(StringComparer.Ordinal);
			}

			return await _clerkDirectory.GetDisplayNamesAsync(ids, cancellationToken);
		}

		private static string? DisplayOrId(string? clerkUserId, IReadOnlyDictionary<string, string> displayNames) =>
			string.IsNullOrWhiteSpace(clerkUserId)
				? null
				: displayNames.TryGetValue(clerkUserId, out string? displayName) ? displayName : clerkUserId;
	}
}
