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

	/// <summary>
	/// /leaderboard?period=today|week|month|all&limit=50
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult<IEnumerable<object>>> Get([FromQuery] string period = "month", [FromQuery] int limit = 50, CancellationToken ct = default)
	{
		// 简化实现：按总减排（all），或最近7/30天排放求和排行
		var users = _db.ApplicationUsers.AsQueryable();
		var logs = _db.ActivityLogs.AsQueryable();
		DateTime? from = null;
		if (string.Equals(period, "today", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date;
		else if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date.AddDays(-6);
		else if (string.Equals(period, "month", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date.AddDays(-29);
		if (limit <= 0 || limit > 100) limit = 50;

		var emissions = logs
			.Where(l => !from.HasValue || l.CreatedAt.Date >= from.Value)
			.GroupBy(l => l.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(x => x.TotalEmission) });

		var query = from u in users
					join e in emissions on u.Id equals e.UserId into ue
					from e in ue.DefaultIfEmpty()
					select new
					{
						userId = u.Id,
						username = u.Username,
						nickname = u.Username,
						avatarUrl = u.AvatarUrl,
						emissions = (decimal?)e.Emission ?? 0m,
						pointsWeek = u.CurrentPoints,
						pointsMonth = u.CurrentPoints,
						pointsTotal = u.CurrentPoints
					};

		var list = await query.OrderByDescending(x => x.emissions).Take(limit).ToListAsync(ct);
		var ranked = list.Select((x, i) => new
		{
			rank = i + 1,
			username = x.username,
			nickname = x.nickname,
			emissionsTotal = x.emissions,
			avatarUrl = x.avatarUrl,
			pointsTotal = x.pointsTotal
		});
		return Ok(ranked);
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 获取指定用户名的排行榜条目（同列表字段）。
	/// </summary>
	[HttpGet("{username}")]
	[AllowAnonymous]
	public async Task<ActionResult<object>> GetByUsername([FromRoute] string username, [FromQuery] string period = "month", CancellationToken ct = default)
	{
		var users = _db.ApplicationUsers.AsQueryable();
		var logs = _db.ActivityLogs.AsQueryable();
		DateTime? from = null;
		if (string.Equals(period, "today", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date;
		else if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date.AddDays(-6);
		else if (string.Equals(period, "month", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date.AddDays(-29);

		var emissions = logs
			.Where(l => !from.HasValue || l.CreatedAt.Date >= from.Value)
			.GroupBy(l => l.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(x => x.TotalEmission) });

		var query = from u in users
					join e in emissions on u.Id equals e.UserId into ue
					from e in ue.DefaultIfEmpty()
					select new
					{
						userId = u.Id,
						username = u.Username,
						nickname = u.Username,
						avatarUrl = u.AvatarUrl,
						emissions = (decimal?)e.Emission ?? 0m,
						pointsWeek = u.CurrentPoints,
						pointsMonth = u.CurrentPoints,
						pointsTotal = u.CurrentPoints
					};

		var all = await query.OrderByDescending(x => x.emissions).ToListAsync(ct);
		var item = all.Select((x, i) => new
		{
			rank = i + 1,
			username = x.username,
			nickname = x.nickname,
			emissionsTotal = x.emissions,
			avatarUrl = x.avatarUrl,
			pointsTotal = x.pointsTotal
		}).FirstOrDefault(x => string.Equals(x.username, username, StringComparison.OrdinalIgnoreCase));

		if (item == null) return NotFound();
		return Ok(item);
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

