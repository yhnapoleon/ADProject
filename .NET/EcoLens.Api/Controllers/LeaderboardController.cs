using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Social;
using System.Security.Claims;
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

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
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

	/// <summary>
	/// 关注某个用户。
	/// </summary>
	[HttpPost("follow/{targetUserId:int}")]
	public async Task<IActionResult> Follow([FromRoute] int targetUserId, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		if (targetUserId == userId.Value) return BadRequest("Cannot follow yourself.");

		var existsUser = await _db.ApplicationUsers.AnyAsync(u => u.Id == targetUserId, ct);
		if (!existsUser) return NotFound("Target user not found.");

		var exists = await _db.UserFollows.AnyAsync(f => f.FollowerId == userId.Value && f.FolloweeId == targetUserId, ct);
		if (exists) return NoContent();

		await _db.UserFollows.AddAsync(new Models.UserFollow
		{
			FollowerId = userId.Value,
			FolloweeId = targetUserId
		}, ct);
		await _db.SaveChangesAsync(ct);

		return Ok();
	}

	/// <summary>
	/// 获取我关注的人的排行榜数据（按总减排量排序）。
	/// </summary>
	[HttpGet("friends")]
	public async Task<ActionResult<IEnumerable<LeaderboardItemDto>>> Friends(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var items = await _db.UserFollows
			.Where(f => f.FollowerId == userId.Value)
			.Join(_db.ApplicationUsers,
				f => f.FolloweeId,
				u => u.Id,
				(f, u) => u)
			.OrderByDescending(u => u.TotalCarbonSaved)
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

