using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/mainpage")]
[Authorize]
public class MainPageController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public MainPageController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	public sealed class MainPageStatsDto
	{
		public decimal Total { get; set; }
		public decimal Food { get; set; }
		public decimal Transport { get; set; }
		public decimal Utility { get; set; }
	}

	[HttpGet]
	public async Task<ActionResult<MainPageStatsDto>> Get(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var now = DateTime.UtcNow;
		var monthStart = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);
		var nextMonth = monthStart.AddMonths(1);

		// 查询 ActivityLogs（食物、水电等，按本月 CreatedAt）
		var activityLogs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= monthStart && l.CreatedAt < nextMonth)
			.Include(l => l.CarbonReference)
			.ToListAsync(ct);

		// 查询 FoodRecords（LogMeal 保存的食物记录；表可能不存在则忽略）
		decimal foodRecordsEmission = 0;
		try
		{
			foodRecordsEmission = await _db.FoodRecords
				.Where(r => r.UserId == userId.Value && r.CreatedAt >= monthStart && r.CreatedAt < nextMonth)
				.SumAsync(r => r.Emission, ct);
		}
		catch (Exception)
		{
			// FoodRecords 表可能不存在（未执行迁移），忽略
		}

		// 查询 TravelLogs（出行记录，按本月 CreatedAt）
		var travelLogs = await _db.TravelLogs
			.Where(t => t.UserId == userId.Value && t.CreatedAt >= monthStart && t.CreatedAt < nextMonth)
			.ToListAsync(ct);

		// 查询 UtilityBills（水电账单，按 YearMonth 匹配当前年月，与创建账单时一致，避免时区/DateTime 边界问题）
		var currentYearMonth = $"{now.Year:D4}-{now.Month:D2}";
		var utilityBillsEmission = await _db.UtilityBills
			.Where(b => b.UserId == userId.Value && b.YearMonth == currentYearMonth)
			.SumAsync(b => b.TotalCarbonEmission, ct);

		// 计算各类别的碳排放
		// Food：ActivityLogs 中的 Food + FoodRecords 表
		var foodFromActivities = activityLogs
			.Where(l => l.CarbonReference != null && l.CarbonReference.Category == CarbonCategory.Food)
			.Sum(l => l.TotalEmission);
		var food = foodFromActivities + foodRecordsEmission;

		// Transport：ActivityLogs 中的 Transport + TravelLogs
		var transportFromActivities = activityLogs
			.Where(l => l.CarbonReference != null && l.CarbonReference.Category == CarbonCategory.Transport)
			.Sum(l => l.TotalEmission);
		var transport = transportFromActivities + travelLogs.Sum(t => t.CarbonEmission);

		// Utility：仅从 UtilityBills 统计（按 BillPeriodEnd 月份），与 Records 一致，避免与 ActivityLogs 重复
		var utility = utilityBillsEmission;

		return Ok(new MainPageStatsDto
		{
			Total = food + transport + utility,
			Food = food,
			Transport = transport,
			Utility = utility
		});
	}
}


