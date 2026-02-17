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
	/// Convert AvatarUrl to API URL (if Base64, returns /api/user/{userId}/avatar)
	/// </summary>
	private string? ConvertAvatarUrlToApiUrl(string? avatarUrl, int userId, long version)
	{
		if (string.IsNullOrWhiteSpace(avatarUrl))
		{
			return null;
		}

		// If Base64 format, return API endpoint URL
		if (avatarUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
		{
			return Url.Action("GetAvatar", "UserProfile", new { userId, v = version }, Request.Scheme, Request.Host.Value);
		}

		// If already a URL, return as-is
		if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
		{
			return avatarUrl;
		}

		// Otherwise return null
		return null;
	}

	/// <summary>
	/// /leaderboard?period=today|week|month|all&amp;limit=50
	/// Aggregates ActivityLogs (excluding Utility to avoid duplicates) + TravelLogs + UtilityBills + FoodRecords
	/// </summary>
	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult<IEnumerable<object>>> Get([FromQuery] string period = "month", [FromQuery] int limit = 50, CancellationToken ct = default)
	{
		var users = await _db.ApplicationUsers.Where(u => u.IsActive).ToListAsync(ct);
		DateTime? from = null;
		DateTime? toDate = null;
		if (string.Equals(period, "today", StringComparison.OrdinalIgnoreCase))
		{
			from = DateTime.UtcNow.Date;
			toDate = from.Value.AddDays(1);
		}
		else if (string.Equals(period, "week", StringComparison.OrdinalIgnoreCase))
		{
			from = DateTime.UtcNow.Date.AddDays(-6);
			toDate = DateTime.UtcNow.Date.AddDays(1);
		}
		else if (string.Equals(period, "month", StringComparison.OrdinalIgnoreCase))
		{
			// Calendar month: from 1st day 00:00 to next month 1st day 00:00 (exclusive)
			from = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
			toDate = from.Value.AddMonths(1);
		}
		if (limit <= 0 || limit > 100) limit = 50;

		// Aggregate today/week/month points from PointAwardLogs
		var todayStart = DateTime.UtcNow.Date;
		var todayEnd = todayStart.AddDays(1);
		var weekStart = DateTime.UtcNow.Date.AddDays(-6);
		var weekEnd = DateTime.UtcNow.Date.AddDays(1);
		var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
		var monthEnd = monthStart.AddMonths(1);

		var pointsTodayList = await _db.PointAwardLogs
			.Where(p => p.AwardedAt >= todayStart && p.AwardedAt < todayEnd)
			.GroupBy(p => p.UserId)
			.Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Points) })
			.ToListAsync(ct);
		var pointsWeekList = await _db.PointAwardLogs
			.Where(p => p.AwardedAt >= weekStart && p.AwardedAt < weekEnd)
			.GroupBy(p => p.UserId)
			.Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Points) })
			.ToListAsync(ct);
		var pointsMonthList = await _db.PointAwardLogs
			.Where(p => p.AwardedAt >= monthStart && p.AwardedAt < monthEnd)
			.GroupBy(p => p.UserId)
			.Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Points) })
			.ToListAsync(ct);

		var pointsTodayByUser = pointsTodayList.ToDictionary(x => x.UserId, x => x.Points);
		var pointsWeekByUser = pointsWeekList.ToDictionary(x => x.UserId, x => x.Points);
		var pointsMonthByUser = pointsMonthList.ToDictionary(x => x.UserId, x => x.Points);

		// 1. ActivityLogs (exclude Utility to avoid duplicates with UtilityBills)
		var activityEmissions = await _db.ActivityLogs
			.Include(l => l.CarbonReference)
			.Where(l => (l.CarbonReference == null || l.CarbonReference.Category != CarbonCategory.Utility) &&
			            (!from.HasValue || (l.CreatedAt.Date >= from.Value && (!toDate.HasValue || l.CreatedAt.Date < toDate.Value))))
			.GroupBy(l => l.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		// 2. TravelLogs
		var travelEmissions = await _db.TravelLogs
			.Where(t => !from.HasValue || (t.CreatedAt.Date >= from.Value && (!toDate.HasValue || t.CreatedAt.Date < toDate.Value)))
			.GroupBy(t => t.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(t => t.CarbonEmission) })
			.ToListAsync(ct);

		// 3. UtilityBills (grouped by BillPeriodEnd month)
		var utilityEmissions = await _db.UtilityBills
			.Where(b => !from.HasValue || (b.BillPeriodEnd >= from.Value && (!toDate.HasValue || b.BillPeriodEnd < toDate.Value)))
			.GroupBy(b => b.UserId)
			.Select(g => new { UserId = g.Key, Emission = g.Sum(b => b.TotalCarbonEmission) })
			.ToListAsync(ct);

		// 4. FoodRecords (ignore if table doesn't exist)
		Dictionary<int, decimal> foodDict = new();
		try
		{
			var foodList = await _db.FoodRecords
				.Where(r => !from.HasValue || (r.CreatedAt.Date >= from.Value && (!toDate.HasValue || r.CreatedAt.Date < toDate.Value)))
				.GroupBy(r => r.UserId)
				.Select(g => new { UserId = g.Key, Emission = g.Sum(r => r.Emission) })
				.ToListAsync(ct);
			foodDict = foodList.ToDictionary(x => x.UserId, x => x.Emission);
		}
		catch { /* FoodRecords table may not exist */ }

		// When period is "all", use TotalCarbonEmission from user table (which already contains all historical data)
		// and add FoodRecords (since TotalCarbonEmission doesn't include FoodRecords)
		var emissionByUser = new Dictionary<int, decimal>();
		if (!from.HasValue && !toDate.HasValue)
		{
			// Period is "all" - use TotalCarbonEmission from user table + FoodRecords
			foreach (var u in users)
			{
				var foodEmission = foodDict.GetValueOrDefault(u.Id);
				emissionByUser[u.Id] = u.TotalCarbonEmission + foodEmission;
			}
		}
		else
		{
			// Period is "today", "week", or "month" - calculate from logs
			emissionByUser = users.ToDictionary(u => u.Id, _ => 0m);
			foreach (var e in activityEmissions) emissionByUser[e.UserId] = emissionByUser.GetValueOrDefault(e.UserId) + e.Emission;
			foreach (var e in travelEmissions) emissionByUser[e.UserId] = emissionByUser.GetValueOrDefault(e.UserId) + e.Emission;
			foreach (var e in utilityEmissions) emissionByUser[e.UserId] = emissionByUser.GetValueOrDefault(e.UserId) + e.Emission;
			foreach (var kv in foodDict) emissionByUser[kv.Key] = emissionByUser.GetValueOrDefault(kv.Key) + kv.Value;
		}

		// Sort: today by today's points, week by week's points, month by month's points, all by total points (all descending)
		var list = users
			.Select(u => new
			{
				userId = u.Id,
				username = u.Username,
				nickname = u.Nickname ?? u.Username,
				avatarUrl = u.AvatarUrl,
				updatedAt = u.UpdatedAt,
				emissions = emissionByUser.GetValueOrDefault(u.Id),
				pointsToday = pointsTodayByUser.GetValueOrDefault(u.Id),
				pointsWeek = pointsWeekByUser.GetValueOrDefault(u.Id),
				pointsMonth = pointsMonthByUser.GetValueOrDefault(u.Id),
				pointsTotal = u.CurrentPoints
			})
			.ToList();

		var ordered = string.Equals(period, "today", StringComparison.OrdinalIgnoreCase)
			? list.OrderByDescending(x => x.pointsToday).ThenByDescending(x => x.pointsTotal).ThenBy(x => x.username).Take(limit).ToList()
			: string.Equals(period, "week", StringComparison.OrdinalIgnoreCase)
				? list.OrderByDescending(x => x.pointsWeek).ThenByDescending(x => x.pointsTotal).ThenBy(x => x.username).Take(limit).ToList()
				: string.Equals(period, "month", StringComparison.OrdinalIgnoreCase)
					? list.OrderByDescending(x => x.pointsMonth).ThenByDescending(x => x.pointsTotal).ThenBy(x => x.username).Take(limit).ToList()
					: list.OrderByDescending(x => x.pointsTotal).ThenBy(x => x.username).Take(limit).ToList();

		var ranked = ordered.Select((x, i) => new
		{
			rank = i + 1,
			username = x.username,
			nickname = x.nickname,
			emissionsTotal = x.emissions,
			avatarUrl = ConvertAvatarUrlToApiUrl(x.avatarUrl, x.userId, x.updatedAt.Ticks),
			pointsToday = x.pointsToday,
			pointsWeek = x.pointsWeek,
			pointsMonth = x.pointsMonth,
			pointsTotal = x.pointsTotal
		});
		return Ok(ranked);
	}

	/// <summary>
	/// Today's leaderboard (equivalent to /api/Leaderboard?period=today)
	/// </summary>
	[HttpGet("today")]
	[AllowAnonymous]
	public async Task<ActionResult<IEnumerable<object>>> GetToday([FromQuery] int limit = 50, CancellationToken ct = default)
		=> await Get("today", limit, ct);

	/// <summary>
	/// Monthly leaderboard (equivalent to /api/Leaderboard?period=month)
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
	/// Get leaderboard entry for specified username (same fields as list).
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
	/// Get top 10 users by total carbon saved.
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
	/// Follow a user.
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
	/// Get leaderboard data for users I follow (sorted by total carbon saved).
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

