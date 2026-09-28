using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Leaderboard.Models;
using Leaderboard.Repositories;
using Leaderboard.Dtos;

namespace Leaderboard.Controllers;

/// <summary>
/// Controller for generating reports related to the leaderboard system.
/// </summary>
[ApiController]
[Route("reports")]
public class ReportsController(IScoreRepository scoreRepository, ILogger<ReportsController> logger) : ControllerBase
{
    private readonly IScoreRepository _scoreRepository = scoreRepository;
    private readonly ILogger _logger = logger;

	/// <summary>
	/// Generates a report of the top players within a specified date range, limited to a certain number of players. Accessible only by users with the "Admin" role.
	/// </summary>
    [HttpGet("top-players")]
	[Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTopPlayersReport([FromQuery] DateTime start_date, [FromQuery] DateTime end_date, [FromQuery] int limit = 10)
    {
        try
        {
            if (start_date > end_date)
            {
                return BadRequest(new { message = "Start date must be before end date." });
            }

            var topPlayers = await _scoreRepository.GetTopPlayersAsync(start_date, end_date, limit);
            var dto = topPlayers.Select(tp => new LeaderboardEntryDto
            {
                UserId = tp.UserId,
                UserName = tp.UserName,
                Score = tp.Score
            }).ToList();

            return Ok(new
            {
                start_date,
                end_date,
                top_players = dto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating top players report from {StartDate} to {EndDate}", start_date, end_date);
            return StatusCode(500, new { message = "An error occurred while generating the report." });
        }
    }
}