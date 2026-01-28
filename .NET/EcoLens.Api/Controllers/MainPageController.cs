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

		var logs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= monthStart && l.CreatedAt < nextMonth)
			.Include(l => l.CarbonReference)
			.ToListAsync(ct);

		var food = logs.Where(l => l.CarbonReference!.Category == CarbonCategory.Food).Sum(l => l.TotalEmission);
		var transport = logs.Where(l => l.CarbonReference!.Category == CarbonCategory.Transport).Sum(l => l.TotalEmission);
		var utility = logs.Where(l => l.CarbonReference!.Category == CarbonCategory.Utility).Sum(l => l.TotalEmission);

		return Ok(new MainPageStatsDto
		{
			Total = food + transport + utility,
			Food = food,
			Transport = transport,
			Utility = utility
		});
	}
}


