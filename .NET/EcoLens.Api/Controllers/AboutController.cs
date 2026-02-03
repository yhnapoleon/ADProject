using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/about-me")]
[Authorize]
public class AboutController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public AboutController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	public sealed class MonthlyEmissionDto
	{
		public string Month { get; set; } = string.Empty; // yyyy-MM
		public decimal EmissionsTotal { get; set; }
		public decimal Food { get; set; }
		public decimal Transport { get; set; }
		public decimal Utility { get; set; }
		public decimal AverageAllUsers { get; set; }
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<MonthlyEmissionDto>>> Get(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var endMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
		var startMonth = endMonth.AddMonths(-11);

		// 取出这 12 个月内与当前用户相关的各类记录（含边界）
		var userActivityLogs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= startMonth && l.CreatedAt < endMonth.AddMonths(1))
			.Select(l => new { l.TotalEmission, l.CreatedAt, Category = l.CarbonReference!.Category })
			.ToListAsync(ct);

		var userFoodRecords = await _db.FoodRecords
			.Where(f => f.UserId == userId.Value && f.CreatedAt >= startMonth && f.CreatedAt < endMonth.AddMonths(1))
			.Select(f => new { f.Emission, f.CreatedAt })
			.ToListAsync(ct);

		var userTravelLogs = await _db.TravelLogs
			.Where(t => t.UserId == userId.Value && t.CreatedAt >= startMonth && t.CreatedAt < endMonth.AddMonths(1))
			.Select(t => new { t.CarbonEmission, t.CreatedAt })
			.ToListAsync(ct);

		// Utility 按账单结束月份统计，避免与为展示而生成的 ActivityLog 双算
		var userUtilityBills = await _db.UtilityBills
			.Where(b => b.UserId == userId.Value && b.BillPeriodEnd >= startMonth && b.BillPeriodEnd < endMonth.AddMonths(1))
			.Select(b => new { b.TotalCarbonEmission, b.BillPeriodEnd })
			.ToListAsync(ct);

		// 所有用户在该时间窗的记录（用于计算“所有用户的平均碳排放（按月、按人均）”）
		var allActivityLogs = await _db.ActivityLogs
			.Where(l => l.CreatedAt >= startMonth && l.CreatedAt < endMonth.AddMonths(1))
			.Select(l => new { l.UserId, l.TotalEmission, l.CreatedAt, Category = l.CarbonReference!.Category })
			.ToListAsync(ct);
		var allFoodRecords = await _db.FoodRecords
			.Where(f => f.CreatedAt >= startMonth && f.CreatedAt < endMonth.AddMonths(1))
			.Select(f => new { f.UserId, f.Emission, f.CreatedAt })
			.ToListAsync(ct);
		var allTravelLogs = await _db.TravelLogs
			.Where(t => t.CreatedAt >= startMonth && t.CreatedAt < endMonth.AddMonths(1))
			.Select(t => new { t.UserId, t.CarbonEmission, t.CreatedAt })
			.ToListAsync(ct);
		var allUtilityBills = await _db.UtilityBills
			.Where(b => b.BillPeriodEnd >= startMonth && b.BillPeriodEnd < endMonth.AddMonths(1))
			.Select(b => new { b.UserId, b.TotalCarbonEmission, b.BillPeriodEnd })
			.ToListAsync(ct);

		var result = new List<MonthlyEmissionDto>(12);
		for (int i = 0; i < 12; i++)
		{
			var mStart = startMonth.AddMonths(i);
			var mEnd = mStart.AddMonths(1);

			// 当前用户：食物 = Activity(Food) + FoodRecords
			var userFoodFromActivities = userActivityLogs
				.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd && l.Category == CarbonCategory.Food)
				.Sum(l => l.TotalEmission);
			var userFoodFromRecords = userFoodRecords
				.Where(f => f.CreatedAt >= mStart && f.CreatedAt < mEnd)
				.Sum(f => f.Emission);
			decimal food = userFoodFromActivities + userFoodFromRecords;

			// 当前用户：出行 = Activity(Transport) + TravelLogs
			var userTransportFromActivities = userActivityLogs
				.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd && l.Category == CarbonCategory.Transport)
				.Sum(l => l.TotalEmission);
			var userTransportFromTravels = userTravelLogs
				.Where(t => t.CreatedAt >= mStart && t.CreatedAt < mEnd)
				.Sum(t => t.CarbonEmission);
			decimal transport = userTransportFromActivities + userTransportFromTravels;

			// 当前用户：公用事业 = UtilityBills（避免与为展示而生成的 ActivityLog 双算）
			decimal utility = userUtilityBills
				.Where(b => b.BillPeriodEnd >= mStart && b.BillPeriodEnd < mEnd)
				.Sum(b => b.TotalCarbonEmission);

			// 计算该月“所有用户的平均碳排放”：先按用户汇总当月总排放（Activity 仅计入 Food/Transport），再对用户总排放取平均
			var perUser = new Dictionary<int, decimal>();
			// Activity（仅 Food/Transport）
			foreach (var a in allActivityLogs.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd && l.Category != CarbonCategory.Utility))
			{
				perUser[a.UserId] = perUser.GetValueOrDefault(a.UserId) + a.TotalEmission;
			}
			// FoodRecords
			foreach (var f in allFoodRecords.Where(f => f.CreatedAt >= mStart && f.CreatedAt < mEnd))
			{
				perUser[f.UserId] = perUser.GetValueOrDefault(f.UserId) + f.Emission;
			}
			// TravelLogs
			foreach (var t in allTravelLogs.Where(t => t.CreatedAt >= mStart && t.CreatedAt < mEnd))
			{
				perUser[t.UserId] = perUser.GetValueOrDefault(t.UserId) + t.CarbonEmission;
			}
			// UtilityBills（按账单结束月计入）
			foreach (var u in allUtilityBills.Where(b => b.BillPeriodEnd >= mStart && b.BillPeriodEnd < mEnd))
			{
				perUser[u.UserId] = perUser.GetValueOrDefault(u.UserId) + u.TotalCarbonEmission;
			}
			decimal avgAllUsers = perUser.Count > 0 ? perUser.Values.Average() : 0m;

			result.Add(new MonthlyEmissionDto
			{
				Month = mStart.ToString("yyyy-MM"),
				EmissionsTotal = food + transport + utility,
				Food = food,
				Transport = transport,
				Utility = utility,
				AverageAllUsers = avgAllUsers
			});
		}

		return Ok(result);
	}
}


