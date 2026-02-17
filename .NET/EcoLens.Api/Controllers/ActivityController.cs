using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Activity;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLens.Api.Services;
using Microsoft.Extensions.Logging;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IClimatiqService _climatiqService;
	private readonly ILogger<ActivityController>? _logger;
	private readonly IPointService _pointService;

	public ActivityController(ApplicationDbContext db, IClimatiqService climatiqService, IPointService pointService, ILogger<ActivityController>? logger = null)
	{
		_db = db;
		_climatiqService = climatiqService;
		_pointService = pointService;
		_logger = logger;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// Upload activity record: find reference by label, calculate emission and save.
	/// </summary>
	[HttpPost("upload")]
	public async Task<ActionResult<ActivityLogResponseDto>> Upload([FromForm] CreateActivityLogDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		// For Utility category match by Region first, otherwise by LabelName
		CarbonReference? carbonRef = null;
		if (dto.Category is CarbonCategory.Utility)
		{
			var userRegion = await _db.ApplicationUsers
				.Where(u => u.Id == userId.Value)
				.Select(u => u.Region)
				.FirstOrDefaultAsync(ct);

			// Prefer (Label, Utility, userRegion), else fallback to (Label, Utility, Region == null)
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

		// If no local carbon reference found, try fetching from Climatiq API
		if (carbonRef is null)
		{
			// Logic needed to map dto.Label and dto.Category to Climatiq activityId
			// Using sample activityId for now; in production map from your model output
			var climatiqActivityId = "consumer_goods-type_snack_foods"; // Sample Climatiq Activity ID
			var climatiqRegion = await _db.ApplicationUsers
				.Where(u => u.Id == userId.Value)
				.Select(u => u.Region)
				.FirstOrDefaultAsync(ct) ?? "US"; // Default to US region

			var climatiqEstimate = await _climatiqService.GetCarbonEmissionEstimateAsync(
				climatiqActivityId, dto.Quantity, dto.Unit, climatiqRegion);

			if (climatiqEstimate is not null && dto.Category.HasValue)
			{
				// Save Climatiq result to local CarbonReference DB for reuse
				carbonRef = new CarbonReference
				{
					LabelName = dto.Label,
					Category = dto.Category.Value, // Use .Value since HasValue was checked
					Co2Factor = climatiqEstimate.Co2e / dto.Quantity, // Climatiq returns total emission; here we compute factor
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

		// Update user total carbon emission
		await UpdateUserTotalCarbonEmissionAsync(userId.Value, ct);

		// Recalculate total carbon saved (based on daily net value)
		try
		{
			await _pointService.RecalculateTotalCarbonSavedAsync(userId.Value);
		}
		catch
		{
			// Do not affect main flow
		}

		var result = new ActivityLogResponseDto
		{
			Id = log.Id,
			Label = carbonRef.LabelName,
			Quantity = log.Quantity,
			TotalEmission = log.TotalEmission,
			ImageUrl = log.ImageUrl,
			CreatedAt = log.CreatedAt,
			Category = carbonRef.Category,
			EmissionUnit = "kg CO2e",
			FactorUnit = carbonRef.Unit,
			Description = log.DetectedLabel ?? carbonRef.LabelName
		};

		return Ok(result);
	}

	/// <summary>
	/// Update user total carbon emission (aggregate from ActivityLogs, TravelLogs, UtilityBills).
	/// </summary>
	private async Task UpdateUserTotalCarbonEmissionAsync(int userId, CancellationToken ct)
	{
		try
		{
			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
			if (user == null) return;

			var activityEmission = await _db.ActivityLogs
				.Where(a => a.UserId == userId)
				.SumAsync(a => (decimal?)a.TotalEmission, ct) ?? 0m;

			var travelEmission = await _db.TravelLogs
				.Where(t => t.UserId == userId)
				.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

			var utilityEmission = await _db.UtilityBills
				.Where(u => u.UserId == userId)
				.SumAsync(u => (decimal?)u.TotalCarbonEmission, ct) ?? 0m;

			user.TotalCarbonEmission = activityEmission + travelEmission + utilityEmission;
			await _db.SaveChangesAsync(ct);
		}
		catch (Exception ex)
		{
			// Log error but do not affect main flow
			// Note: if logger is not injected, this fails silently without affecting main flow
			try
			{
				_logger?.LogError(ex, "Failed to update TotalCarbonEmission for user {UserId}", userId);
			}
			catch
			{
				// Ignore logger error
			}
		}
	}

	/// <summary>
	/// Dashboard data: last 7 days emission trend, today's carbon neutrality gap (target 10kg/day), equivalent trees (TotalCarbonSaved/20).
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

		// Last 7 days, build list in date order (fill missing dates with 0)
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
	/// Get current user's activity log list.
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
				CreatedAt = l.CreatedAt,
				Category = l.CarbonReference!.Category,
				EmissionUnit = "kg CO2e",
				FactorUnit = l.CarbonReference!.Unit,
				Description = l.DetectedLabel ?? l.CarbonReference!.LabelName
			})
			.ToListAsync(ct);

		return Ok(logs);
	}

	/// <summary>
	/// Get current user's stats (total emission, record count, equivalent trees, global rank).
	/// </summary>
	[HttpGet("stats")]
	public async Task<ActionResult<ActivityStatsDto>> Stats(CancellationToken ct)
	{

		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var query = _db.ActivityLogs.Where(l => l.UserId == userId.Value);
		var totalItems = await query.CountAsync(ct);
		var totalEmission = await query.SumAsync(l => (decimal?)l.TotalEmission, ct) ?? 0m;

		// Equivalent trees: 1 tree/year ≈ 20kg CO2
		var treesSaved = totalEmission / 20m;

		// Compute user global rank (by TotalCarbonSaved descending)
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
	/// Daily net value: if today lacks any of (food record, travel record, steps &gt; 0), return 0.
	/// Formula: DailyNetValue = (Steps * 0.0001) + (Benchmark - DailyTotalEmission).
	/// </summary>
	[HttpGet("daily-net-value")]
	public async Task<ActionResult<DailyNetValueResponseDto>> DailyNetValue(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var today = DateTime.UtcNow.Date;
		var dayStart = today;
		var dayEnd = today.AddDays(1);

		// Check if all three items for today are present
		var hasFood = await _db.FoodRecords.AnyAsync(r => r.UserId == userId.Value && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd, ct);
		var hasTravel = await _db.TravelLogs.AnyAsync(r => r.UserId == userId.Value && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd, ct);
		var stepRecord = await _db.StepRecords.FirstOrDefaultAsync(r => r.UserId == userId.Value && r.RecordDate == dayStart, ct);
		var steps = stepRecord?.StepCount ?? 0;
		var isQualified = hasFood && hasTravel && steps > 0;

		// Build breakdown data
		var foodEmission = await _db.FoodRecords
			.Where(r => r.UserId == userId.Value && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
			.SumAsync(r => (decimal?)r.Emission, ct) ?? 0m;
		var travelEmission = await _db.TravelLogs
			.Where(r => r.UserId == userId.Value && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
			.SumAsync(r => (decimal?)r.CarbonEmission, ct) ?? 0m;
		var emission = foodEmission + travelEmission;

		const decimal stepFactor = 0.0001m;
		var stepSaving = (decimal)steps * stepFactor;

		const decimal benchmark = 15.0m; // Keep in sync with PointService.DailyBenchmark

		// Value calculation (delegate to service)
		var value = await _pointService.CalculateDailyNetValueAsync(userId.Value, today);

		var dto = new DailyNetValueResponseDto
		{
			Value = value,
			IsQualified = isQualified,
			Breakdown = new DailyNetValueBreakdownDto
			{
				StepSaving = stepSaving,
				Benchmark = benchmark,
				Emission = emission
			}
		};

		return Ok(dto);
	}

	/// <summary>
	/// Get daily total carbon emission for the past N days (current user).
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

		// Aggregate current user's daily emission in time window
		var grouped = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= startDate && l.CreatedAt < endDate)
			.GroupBy(l => l.CreatedAt.Date)
			.Select(g => new { Date = g.Key, Emission = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		// Fill missing dates with 0
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
	/// Aggregate total carbon saved by Region (based on user TotalCarbonSaved).
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
