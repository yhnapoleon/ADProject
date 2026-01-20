using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public LeaderboardController(ApplicationDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// 获取总减排量前 10 名用户。
	/// </summary>
	[HttpGet("top-users")]
	public async Task<ActionResult<IEnumerable<LeaderboardItemDto>>> TopUsers(CancellationToken ct)
	{
		var items = await _db.ApplicationUsers
			.OrderByDescending(u => u.TotalCarbonSaved)
			.Take(10)
			.Select(u => new LeaderboardItemDto
			{
				Username = u.Username,
				AvatarUrl = u.AvatarUrl,
				TotalCarbonSaved = u.TotalCarbonSaved
			})
			.ToListAsync(ct);

		return Ok(items);
	}
}

