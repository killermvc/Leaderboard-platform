using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Leaderboard.Dtos;

namespace Leaderboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeyController(IApiKeyRepository apiKeyRepository, IApiKeyService apiKeyService, IGameRepository gameRepository, IGameModeratorRepository gameModeratorRepository) : ControllerBase
{
	private readonly IApiKeyRepository _apiKeyRepository = apiKeyRepository;
	private readonly IApiKeyService _apiKeyService = apiKeyService;
	private readonly IGameRepository _gameRepository = gameRepository;
	private readonly IGameModeratorRepository _gameModeratorRepository = gameModeratorRepository;

	// POST: api/ApiKey
	[HttpPost]
	public async Task<IActionResult> GenerateKey([FromBody] CreateApiKeyRequest request)
	{
		if (!TryGetUserId(out int userId))
		{
			return Unauthorized(new { Message = "Invalid token" });
		}

		if (string.IsNullOrWhiteSpace(request.Name))
		{
			return BadRequest(new { Message = "Name is required" });
		}

		Game? game = await _gameRepository.GetGameByIdAsync(request.GameId);
		if (game == null)
		{
			return NotFound(new { Message = "Game not found" });
		}

		if (!await CanManageKeysAsync(request.GameId, userId))
		{
			return Forbid();
		}

		string apiKey = _apiKeyService.GenerateApiKey();
		var key = new ApiKey
		{
			Name = request.Name,
			KeyHash = _apiKeyService.HashApiKey(apiKey),
			GameId = request.GameId,
			UserId = userId,
			Permissions = request.Permissions ?? ApiKeyPermissions.None,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = request.ExpiresAt
		};

		await _apiKeyRepository.AddAsync(key);

		return CreatedAtAction(nameof(GetKeysForGame), new { gameId = key.GameId }, new CreatedApiKeyDto
		{
			Id = key.Id,
			Name = key.Name,
			ApiKey = apiKey,
			CreatedAt = key.CreatedAt,
			ExpiresAt = key.ExpiresAt
		});
	}

	// GET: api/ApiKey/game/{gameId}
	[HttpGet("game/{gameId}")]
	public async Task<IActionResult> GetKeysForGame(int gameId)
	{
		if (!TryGetUserId(out int userId))
		{
			return Unauthorized(new { Message = "Invalid token" });
		}

		Game? game = await _gameRepository.GetGameByIdAsync(gameId);
		if (game == null)
		{
			return NotFound(new { Message = "Game not found" });
		}

		if (!await CanManageKeysAsync(gameId, userId))
		{
			return Forbid();
		}

		var keys = await _apiKeyRepository.GetKeysForGameAsync(gameId);

		var keyDtos = keys.Select(k => new ApiKeyDto
		{
			Id = k.Id,
			Name = k.Name,
			CreatedAt = k.CreatedAt,
			LastUsedAt = k.LastUsedAt,
			ExpiresAt = k.ExpiresAt,
			RevokedAt = k.RevokedAt
		});

		return Ok(keyDtos);
	}

	// POST: api/ApiKey/{id}/revoke
	[HttpPost("{id}/revoke")]
	public async Task<IActionResult> RevokeKey(int id)
	{
		if (!TryGetUserId(out int userId))
		{
			return Unauthorized(new { Message = "Invalid token" });
		}

		ApiKey? key = await _apiKeyRepository.GetByIdAsync(id);
		if (key == null)
		{
			return NotFound(new { Message = "Api key not found" });
		}

		if (!await CanManageKeysAsync(key.GameId, userId))
		{
			return Forbid();
		}

		if (key.RevokedAt.HasValue)
		{
			return BadRequest(new { Message = "Api key is already revoked" });
		}

		key.RevokedAt = DateTime.UtcNow;
		await _apiKeyRepository.UpdateAsync(key);

		return Ok(new ApiKeyDto
		{
			Id = key.Id,
			Name = key.Name,
			CreatedAt = key.CreatedAt,
			LastUsedAt = key.LastUsedAt,
			ExpiresAt = key.ExpiresAt,
			RevokedAt = key.RevokedAt
		});
	}

	private bool TryGetUserId(out int userId)
	{
		var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
		return int.TryParse(userIdClaim, out userId);
	}

	private async Task<bool> CanManageKeysAsync(int gameId, int userId)
	{
		return User.IsInRole("Admin") || await _gameModeratorRepository.CanModerateGameAsync(gameId, userId);
	}
}