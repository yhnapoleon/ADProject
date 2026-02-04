using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Social;
using EcoLens.Api.Models.Enums;
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
	/// 将 AvatarUrl 转换为 API URL（如果是 Base64，返回 /api/user/{userId}/avatar）
	/// </summary>
	private string? ConvertAvatarUrlToApiUrl(string? avatarUrl, int userId, long version)
	{
		if (string.IsNullOrWhiteSpace(avatarUrl))
		{
			return null;
		}

		// 如果是 Base64 格式，返回 API 端点 URL
		if (avatarUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
		{
			return Url.Action("GetAvatar", "UserProfile", new { userId, v = version }, Request.Scheme, Request.Host.Value);
		}

		// 如果已经是 URL，直接返回
		if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
		{
			return avatarUrl;
		}

		// 其他情况返回 null
		return null;
	}

	/// <summary>
	/// /leaderboard?period=today|week|month|all&amp;limit=50
	/// 聚合 ActivityLogs(排除Utility避免重复) + TravelLogs + UtilityBills + FoodRecords
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult<IEnumerable<object>>> Get([FromQuery] string period = "month", [FromQuery] int limit = 50, CancellationToken ct = default)
	{
		var users = await _db.ApplicationUsers.Where(u => u.IsActive).ToListAsync(ct);
		DateTime? from = null;
		if (string.Equals(period, "today", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date;
		else if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date.AddDays(-6);
		else if (string.Equals(period, "month", StringComparison.OrdinalIgnoreCase)) from = DateTime.UtcNow.Date.AddDays(-29);
		if (limit <= 0 || limit > 100) limit = 50;

		var toDate = DateTime.UtcNow.Date.AddDays(1);

		// 1. ActivityLogs（排除 Utility，因 UtilityBills 单独统计避免重复）
		var activityEmissions = await _db.ActivityLogs
			.Include(l => l.CarbonReference)
			.Where(l => (l.CarbonReference == null || l.CarbonReference.Category != CarbonCategory.Utility) &&
			            (!from.HasValue || l.CreatedAt.Date >= from.Value))
			.GroupBy(l => l.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		// 2. TravelLogs
		var travelEmissions = await _db.TravelLogs
			.Where(t => !from.HasValue || (t.CreatedAt.Date >= from.Value && t.CreatedAt.Date < toDate))
			.GroupBy(t => t.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(t => t.CarbonEmission) })
			.ToListAsync(ct);

		// 3. UtilityBills（按 BillPeriodEnd 归属月份）
		var utilityEmissions = await _db.UtilityBills
			.Where(b => !from.HasValue || (b.BillPeriodEnd >= from.Value && b.BillPeriodEnd < toDate))
			.GroupBy(b => b.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(b => b.TotalCarbonEmission) })
			.ToListAsync(ct);

		// 4. FoodRecords（表可能不存在则忽略）
		Dictionary<int, decimal> foodDict = new();
		try
		{
			var foodList = await _db.FoodRecords
				.Where(r => !from.HasValue || (r.CreatedAt.Date >= from.Value && r.CreatedAt.Date < toDate))
				.GroupBy(r => r.UserId)
				.Select(g => new { UserId = g.Key, Emission = g.Sum(r => r.Emission) })
				.ToListAsync(ct);
			foodDict = foodList.ToDictionary(x => x.UserId, x => x.Emission);
		}
		catch { /* FoodRecords 表可能不存在 */ }

		var emissionByUser = users.ToDictionary(u => u.Id, _ => 0m);
		foreach (var e in activityEmissions) emissionByUser[e.UserId] = emissionByUser.GetValueOrDefault(e.UserId) + e.Emission;
		foreach (var e in travelEmissions) emissionByUser[e.UserId] = emissionByUser.GetValueOrDefault(e.UserId) + e.Emission;
		foreach (var e in utilityEmissions) emissionByUser[e.UserId] = emissionByUser.GetValueOrDefault(e.UserId) + e.Emission;
		foreach (var kv in foodDict) emissionByUser[kv.Key] = emissionByUser.GetValueOrDefault(kv.Key) + kv.Value;

		var list = users
			.Select(u => new
			{
				userId = u.Id,
				username = u.Username,
				nickname = u.Nickname ?? u.Username,
				avatarUrl = u.AvatarUrl,
				updatedAt = u.UpdatedAt,
				emissions = emissionByUser.GetValueOrDefault(u.Id),
				pointsWeek = u.CurrentPoints,
				pointsMonth = u.CurrentPoints,
				pointsTotal = u.CurrentPoints
			})
			.OrderByDescending(x => x.pointsTotal)
			.Take(limit)
			.ToList();

		var ranked = list.Select((x, i) => new
		{
			rank = i + 1,
			username = x.username,
			nickname = x.nickname,
			emissionsTotal = x.emissions,
			avatarUrl = ConvertAvatarUrlToApiUrl(x.avatarUrl, x.userId, x.updatedAt.Ticks),
			pointsTotal = x.pointsTotal
		});
		return Ok(ranked);
	}

	/// <summary>
	/// 今日排行榜（等价于 /api/Leaderboard?period=today）
	/// </summary>
	[HttpGet("today")]
	[AllowAnonymous]
	public async Task<ActionResult<IEnumerable<object>>> GetToday([FromQuery] int limit = 50, CancellationToken ct = default)
		=> await Get("today", limit, ct);

	/// <summary>
	/// 月度排行榜（等价于 /api/Leaderboard?period=month）
	/// </summary>
	[HttpGet("month")]
	[AllowAnonymous]
	public async Task<ActionResult<IEnumerable<object>>> GetMonth([FromQuery] int limit = 50, CancellationToken ct = default)
		=> await Get("month", limit, ct);

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
		var getResult = await Get(period, 1000, ct);
		if (getResult.Result is not OkObjectResult ok || ok.Value is not IEnumerable<object> enumerable)
			return NotFound();
		var all = enumerable.ToList();
		dynamic? item = null;
		foreach (dynamic x in all)
		{
			if (string.Equals((string?)x.username, username, StringComparison.OrdinalIgnoreCase))
			{
				item = x;
				break;
			}
		}
		if (item == null) return NotFound();
		return Ok((object)item);
	}

	/// <summary>
	/// 获取总减排量前 10 名用户。
	/// </summary>
	[HttpGet("top-users")]
	public async Task<ActionResult<IEnumerable<LeaderboardItemDto>>> TopUsers(CancellationToken ct)
	{
		var items = await _db.ApplicationUsers
			.Where(u => u.IsActive)
			.OrderByDescending(u => u.TotalCarbonSaved)
			.Take(10)
			.ToListAsync(ct);

		var itemsDto = items.Select(u => new LeaderboardItemDto
		{
			Username = u.Username,
			AvatarUrl = ConvertAvatarUrlToApiUrl(u.AvatarUrl, u.Id, u.UpdatedAt.Ticks),
			TotalCarbonSaved = u.TotalCarbonSaved
		}).ToList();

		return Ok(itemsDto);
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
			.Where(u => u.IsActive)
			.OrderByDescending(u => u.TotalCarbonSaved)
			.ToListAsync(ct);

		var itemsDto = items.Select(u => new LeaderboardItemDto
		{
			Username = u.Username,
			AvatarUrl = ConvertAvatarUrlToApiUrl(u.AvatarUrl, u.Id, u.UpdatedAt.Ticks),
			TotalCarbonSaved = u.TotalCarbonSaved
		}).ToList();

		return Ok(itemsDto);
	}
}

