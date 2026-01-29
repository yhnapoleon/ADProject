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

		var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
		var nextMonth = monthStart.AddMonths(1);

		// 查询 ActivityLogs（食物、工具等）
		var activityLogs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= monthStart && l.CreatedAt < nextMonth)
			.Include(l => l.CarbonReference)
			.ToListAsync(ct);

		// 查询 TravelLogs（出行记录）
		var travelLogs = await _db.TravelLogs
			.Where(t => t.UserId == userId.Value && t.CreatedAt >= monthStart && t.CreatedAt < nextMonth)
			.ToListAsync(ct);

		// 计算各类别的碳排放
		var food = activityLogs
			.Where(l => l.CarbonReference != null && l.CarbonReference.Category == CarbonCategory.Food)
			.Sum(l => l.TotalEmission);

		// Transport 包括 ActivityLogs 中的 Transport 和所有 TravelLogs
		var transportFromActivities = activityLogs
			.Where(l => l.CarbonReference != null && l.CarbonReference.Category == CarbonCategory.Transport)
			.Sum(l => l.TotalEmission);
		
		var transportFromTravel = travelLogs.Sum(t => t.CarbonEmission);
		var transport = transportFromActivities + transportFromTravel;

		var utility = activityLogs
			.Where(l => l.CarbonReference != null && l.CarbonReference.Category == CarbonCategory.Utility)
			.Sum(l => l.TotalEmission);

		return Ok(new MainPageStatsDto
		{
			Total = food + transport + utility,
			Food = food,
			Transport = transport,
			Utility = utility
		});
	}
}


