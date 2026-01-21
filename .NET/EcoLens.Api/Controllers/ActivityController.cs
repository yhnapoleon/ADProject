using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public ActivityController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 上传活动记录：根据标签查找参考，计算排放并保存。
	/// </summary>
	[HttpPost("upload")]
	public async Task<ActionResult<ActivityLogResponseDto>> Upload([FromForm] CreateActivityLogDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == dto.Label, ct);
		if (carbonRef is null)
		{
			return NotFound("Carbon reference not found for the given label.");
		}

		var total = dto.Quantity * carbonRef.Co2Factor;

		var log = new Models.ActivityLog
		{
			UserId = userId.Value,
			CarbonReferenceId = carbonRef.Id,
			Quantity = dto.Quantity,
			TotalEmission = total,
			ImageUrl = null,
			DetectedLabel = dto.Label
		};

		await _db.ActivityLogs.AddAsync(log, ct);
		await _db.SaveChangesAsync(ct);

		var result = new ActivityLogResponseDto
		{
			Id = log.Id,
			Label = carbonRef.LabelName,
			Quantity = log.Quantity,
			TotalEmission = log.TotalEmission,
			ImageUrl = log.ImageUrl,
			CreatedAt = log.CreatedAt
		};

		return Ok(result);
	}

	/// <summary>
	/// 获取当前用户的活动日志列表。
	/// </summary>
	[HttpGet("my-logs")]
	public async Task<ActionResult<IEnumerable<ActivityLogResponseDto>>> MyLogs(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var logs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value)
			.Include(l => l.CarbonReference)
			.OrderByDescending(l => l.CreatedAt)
			.Select(l => new ActivityLogResponseDto
			{
				Id = l.Id,
				Label = l.CarbonReference!.LabelName,
				Quantity = l.Quantity,
				TotalEmission = l.TotalEmission,
				ImageUrl = l.ImageUrl,
				CreatedAt = l.CreatedAt
			})
			.ToListAsync(ct);

		return Ok(logs);
	}

	/// <summary>
	/// 获取当前用户的统计信息（总排放/记录数量/折算树木/全服排名）。
	/// </summary>
	[HttpGet("stats")]
	public async Task<ActionResult<ActivityStatsDto>> Stats(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var query = _db.ActivityLogs.Where(l => l.UserId == userId.Value);
		var totalItems = await query.CountAsync(ct);
		var totalEmission = await query.SumAsync(l => (decimal?)l.TotalEmission, ct) ?? 0m;

		// 折算树木数量：1 棵树/年 ≈ 20kg CO2
		var treesSaved = totalEmission / 20m;

		// 计算用户全服排名（按 TotalCarbonSaved 倒序）
		var me = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (me is null) return NotFound();
		var rank = await _db.ApplicationUsers.CountAsync(u => u.TotalCarbonSaved > me.TotalCarbonSaved, ct) + 1;

		var dto = new ActivityStatsDto
		{
			TotalEmission = totalEmission,
			TotalItems = totalItems,
			TreesSaved = treesSaved,
			Rank = rank
		};

		return Ok(dto);
	}

	/// <summary>
	/// 获取过去 N 天的每日碳排放总量（当前用户）。
	/// </summary>
	[HttpGet("chart-data")]
	public async Task<ActionResult<IEnumerable<ChartDataPointDto>>> ChartData([FromQuery] int days = 7, CancellationToken ct = default)
	{
		if (days <= 0) days = 7;

		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var todayUtc = DateTime.UtcNow.Date;
		var startDate = todayUtc.AddDays(-(days - 1));
		var endDate = todayUtc.AddDays(1);

		// 聚合当前用户在时间窗口内的每日排放
		var grouped = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= startDate && l.CreatedAt < endDate)
			.GroupBy(l => l.CreatedAt.Date)
			.Select(g => new { Date = g.Key, Emission = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		// 填充缺失日期为 0
		var dict = grouped.ToDictionary(x => x.Date, x => x.Emission);
		var result = new List<ChartDataPointDto>(days);
		for (var d = 0; d < days; d++)
		{
			var date = startDate.AddDays(d);
			dict.TryGetValue(date, out var emission);
			result.Add(new ChartDataPointDto
			{
				Date = date.ToString("yyyy-MM-dd"),
				Emission = emission
			});
		}

		return Ok(result);
	}

	/// <summary>
	/// 按 Region 统计总碳减排量（基于用户的 TotalCarbonSaved）。
	/// </summary>
	[HttpGet("heatmap")]
	public async Task<ActionResult<IEnumerable<RegionHeatmapDto>>> Heatmap(CancellationToken ct)
	{
		var items = await _db.ApplicationUsers
			.Where(u => u.Region != null && u.Region != string.Empty)
			.GroupBy(u => u.Region!)
			.Select(g => new RegionHeatmapDto
			{
				Region = g.Key,
				TotalSaved = g.Sum(x => x.TotalCarbonSaved)
			})
			.OrderByDescending(x => x.TotalSaved)
			.ToListAsync(ct);

		return Ok(items);
	}
}

