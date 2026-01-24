using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Activity;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLens.Api.Services;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IClimatiqService _climatiqService;

	public ActivityController(ApplicationDbContext db, IClimatiqService climatiqService)
	{
		_db = db;
		_climatiqService = climatiqService;
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

		// 针对 Utility 类别按 Region 优先匹配，否则按 LabelName 直接匹配
		CarbonReference? carbonRef = null;
		if (dto.Category is CarbonCategory.Utility)
		{
			var userRegion = await _db.ApplicationUsers
				.Where(u => u.Id == userId.Value)
				.Select(u => u.Region)
				.FirstOrDefaultAsync(ct);

			// 优先匹配 (Label, Utility, userRegion)，否则回退 (Label, Utility, Region == null)
			if (!string.IsNullOrWhiteSpace(userRegion))
			{
				carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
					c => c.LabelName == dto.Label && c.Category == CarbonCategory.Utility && c.Region == userRegion, ct);
			}

			if (carbonRef is null)
			{
				carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
					c => c.LabelName == dto.Label && c.Category == CarbonCategory.Utility && c.Region == null, ct);
			}
		}
		else
		{
			carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == dto.Label, ct);
		}

		// 如果本地未找到碳参考数据，尝试从 Climatiq API 获取
		if (carbonRef is null)
		{
			// 这里需要一个逻辑来将 dto.Label 和 dto.Category 映射到 Climatiq 的 activityId
			// 暂时使用一个示例 activityId，实际应用中需要根据您的深度学习模型输出进行映射
			var climatiqActivityId = "consumer_goods-type_snack_foods"; // 示例 Climatiq Activity ID
			var climatiqRegion = await _db.ApplicationUsers
				.Where(u => u.Id == userId.Value)
				.Select(u => u.Region)
				.FirstOrDefaultAsync(ct) ?? "US"; // 默认使用美国地区

			var climatiqEstimate = await _climatiqService.GetCarbonEmissionEstimateAsync(
				climatiqActivityId, dto.Quantity, dto.Unit, climatiqRegion);

			if (climatiqEstimate is not null && dto.Category.HasValue)
			{
				// 将 Climatiq 结果保存到本地 CarbonReference 数据库，方便下次使用
				carbonRef = new CarbonReference
				{
					LabelName = dto.Label,
					Category = dto.Category.Value, // 使用 .Value 因为已经检查了 HasValue
					Co2Factor = climatiqEstimate.Co2e / dto.Quantity, // Climatiq 返回总排放，这里计算因子
					Unit = dto.Unit,
					Region = climatiqRegion,
					Source = "Climatiq",
					ClimatiqActivityId = climatiqActivityId
				};
				await _db.CarbonReferences.AddAsync(carbonRef, ct);
				await _db.SaveChangesAsync(ct);
			}
		}

		if (carbonRef is null)
		{
			return NotFound("Carbon reference not found for the given label, neither locally nor via Climatiq.");
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
	/// 仪表盘数据：近 7 天排放趋势、当日碳中和差值（目标 10kg/天）、等效植树数（按 TotalCarbonSaved/20）。
	/// </summary>
	[HttpGet("dashboard")]
	public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var today = DateTime.UtcNow.Date;
		var from = today.AddDays(-6);

		var dayGroups = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt.Date >= from && l.CreatedAt.Date <= today)
			.GroupBy(l => l.CreatedAt.Date)
			.Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		// 过去 7 天，按日期顺序组装列表（缺失日期补 0）
		var weeklyTrend = new List<decimal>(7);
		for (var i = 0; i < 7; i++)
		{
			var d = from.AddDays(i);
			var total = dayGroups.FirstOrDefault(x => x.Day == d)?.Total ?? 0m;
			weeklyTrend.Add(total);
		}

		var todayEmission = dayGroups.FirstOrDefault(x => x.Day == today)?.Total ?? 0m;
		const decimal dailyTarget = 10m;
		var neutralityGap = dailyTarget - todayEmission;

		var totalSaved = await _db.ApplicationUsers
			.Where(u => u.Id == userId.Value)
			.Select(u => u.TotalCarbonSaved)
			.FirstOrDefaultAsync(ct);

		var treesPlanted = totalSaved / 20m;

		var dto = new DashboardDto
		{
			WeeklyTrend = weeklyTrend,
			NeutralityGap = neutralityGap,
			TreesPlanted = treesPlanted
		};

		return Ok(dto);
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
