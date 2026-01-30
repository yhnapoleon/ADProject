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

		// 取出这 12 个月内的所有记录（含边界）
		var logs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= startMonth && l.CreatedAt < endMonth.AddMonths(1))
			.Include(l => l.CarbonReference)
			.ToListAsync(ct);

		// 所有用户在该时间窗的记录（用于计算“所有用户的平均碳排放（按月、按人均）”）
		var allLogs = await _db.ActivityLogs
			.Where(l => l.CreatedAt >= startMonth && l.CreatedAt < endMonth.AddMonths(1))
			.Select(l => new { l.UserId, l.TotalEmission, l.CreatedAt })
			.ToListAsync(ct);

		var result = new List<MonthlyEmissionDto>(12);
		for (int i = 0; i < 12; i++)
		{
			var mStart = startMonth.AddMonths(i);
			var mEnd = mStart.AddMonths(1);
			var monthLogs = logs.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd).ToList();

			decimal food = monthLogs.Where(l => l.CarbonReference!.Category == CarbonCategory.Food).Sum(l => l.TotalEmission);
			decimal transport = monthLogs.Where(l => l.CarbonReference!.Category == CarbonCategory.Transport).Sum(l => l.TotalEmission);
			decimal utility = monthLogs.Where(l => l.CarbonReference!.Category == CarbonCategory.Utility).Sum(l => l.TotalEmission);

			// 计算该月“所有用户的平均碳排放”：先按用户汇总当月总排放，再对用户总排放取平均
			var allMonthLogs = allLogs.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd).ToList();
			decimal avgAllUsers = 0m;
			if (allMonthLogs.Count > 0)
			{
				var perUserTotals = allMonthLogs
					.GroupBy(x => x.UserId)
					.Select(g => g.Sum(x => x.TotalEmission))
					.ToList();
				if (perUserTotals.Count > 0)
				{
					avgAllUsers = perUserTotals.Average();
				}
			}

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


