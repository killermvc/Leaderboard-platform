using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Services;
using Leaderboard.Dtos;

namespace Leaderboard.Controllers;

/// <summary>
/// Controller for managing API keys. Provides endpoints to generate, retrieve, revoke, and regenerate API keys for games.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeyController(IApiKeyRepository apiKeyRepository, IApiKeyService apiKeyService, IGameRepository gameRepository, IApiKeyAuthorizationService apiKeyAuthorizationService) : ControllerBase
{
	private readonly IApiKeyRepository _apiKeyRepository = apiKeyRepository;
	private readonly IApiKeyService _apiKeyService = apiKeyService;
	private readonly IGameRepository _gameRepository = gameRepository;
	private readonly IApiKeyAuthorizationService _apiKeyAuthorizationService = apiKeyAuthorizationService;

	/// <summary>
	/// Generates a new API key for a specific game. The user must have permission to manage API keys for the game.
	/// </summary>
	/// <param name="request">The request containing the game ID and other details.</param>
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

		if (!await _apiKeyAuthorizationService.CanManageKeysAsync(User, request.GameId))
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

	/// <summary>
	/// Retrieves all API keys for a specific game. The user must have permission to manage API keys for the game.
	/// </summary>
	/// <param name="gameId">The ID of the game for which to retrieve API keys.</param>
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

		if (!await _apiKeyAuthorizationService.CanManageKeysAsync(User, gameId))
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

	/// <summary>
	/// Revokes an API key by its ID. The user must have permission to manage API keys for the game associated with the key.
	/// If the key is already revoked, a BadRequest response is returned.
	/// </summary>
	/// <param name="id">The ID of the API key to revoke.</param>
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

		if (!await _apiKeyAuthorizationService.CanManageKeysAsync(User, key.GameId))
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

	/// <summary>
	/// Regenerates an API key by its ID. The user must have permission to manage API keys for the game associated with the key.
	/// If the key is revoked, a BadRequest response is returned. The new key will have the same name unless another key in the game already uses it, in which case " - Copy
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	// POST: api/ApiKey/{id}/regenerate
	[HttpPost("{id}/regenerate")]
	public async Task<IActionResult> RegenerateKey(int id)
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

		if (!await _apiKeyAuthorizationService.CanManageKeysAsync(User, key.GameId))
		{
			return Forbid();
		}

		if (key.RevokedAt.HasValue)
		{
			return BadRequest(new { Message = "Api key is already revoked" });
		}

		// Keep the same name unless another key in the game already uses it
		string newName = key.Name;
		if (await _apiKeyRepository.HasKeyWithNameAsync(key.GameId, key.Name, key.Id))
		{
			newName = $"{key.Name} - Copy";
		}

		// Regenerate an expired key carries its expiry over, preserving the expired status
		string newApiKey = _apiKeyService.GenerateApiKey();
		var newKey = new ApiKey
		{
			Name = newName,
			KeyHash = _apiKeyService.HashApiKey(newApiKey),
			GameId = key.GameId,
			UserId = userId,
			Permissions = key.Permissions,
			CreatedAt = DateTime.UtcNow,
			ExpiresAt = key.ExpiresAt
		};

		await _apiKeyRepository.RegenerateAsync(key, newKey);

		return CreatedAtAction(nameof(GetKeysForGame), new { gameId = newKey.GameId }, new CreatedApiKeyDto
		{
			Id = newKey.Id,
			Name = newKey.Name,
			ApiKey = newApiKey,
			CreatedAt = newKey.CreatedAt,
			ExpiresAt = newKey.ExpiresAt
		});
	}

	/// <summary>
	/// Checks if the current user has permission to manage API keys for a specific game.
	/// </summary>
	/// <param name="gameId">The ID of the game to check permissions for.</param>
	// GET: api/ApiKey/game/{gameId}/can-manage
	[HttpGet("game/{gameId}/can-manage")]
	public async Task<IActionResult> CanManageKeys(int gameId)
	{
		var canManage = await _apiKeyAuthorizationService.CanManageKeysAsync(User, gameId);
		return Ok(new { CanManage = canManage });
	}

	private bool TryGetUserId(out int userId)
	{
		var userIdClaim = User.FindFirst(ClaimTypes.Name)?.Value;
		return int.TryParse(userIdClaim, out userId);
	}
}