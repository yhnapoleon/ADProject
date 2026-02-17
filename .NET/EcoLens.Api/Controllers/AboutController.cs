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

		// Fetch records for the current user in these 12 months (inclusive of boundaries)
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

		// Utility aggregated by bill end month to avoid double-counting with ActivityLog generated for display
		var userUtilityBills = await _db.UtilityBills
			.Where(b => b.UserId == userId.Value && b.BillPeriodEnd >= startMonth && b.BillPeriodEnd < endMonth.AddMonths(1))
			.Select(b => new { b.TotalCarbonEmission, b.BillPeriodEnd })
			.ToListAsync(ct);

		// All users' records in this time window (for calculating average carbon emission per user per month)
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

			// Current user: Food = Activity(Food) + FoodRecords
			var userFoodFromActivities = userActivityLogs
				.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd && l.Category == CarbonCategory.Food)
				.Sum(l => l.TotalEmission);
			var userFoodFromRecords = userFoodRecords
				.Where(f => f.CreatedAt >= mStart && f.CreatedAt < mEnd)
				.Sum(f => f.Emission);
			decimal food = userFoodFromActivities + userFoodFromRecords;

			// Current user: Transport = Activity(Transport) + TravelLogs
			var userTransportFromActivities = userActivityLogs
				.Where(l => l.CreatedAt >= mStart && l.CreatedAt < mEnd && l.Category == CarbonCategory.Transport)
				.Sum(l => l.TotalEmission);
			var userTransportFromTravels = userTravelLogs
				.Where(t => t.CreatedAt >= mStart && t.CreatedAt < mEnd)
				.Sum(t => t.CarbonEmission);
			decimal transport = userTransportFromActivities + userTransportFromTravels;

			// Current user: Utility = UtilityBills (avoid double-counting with ActivityLog generated for display)
			decimal utility = userUtilityBills
				.Where(b => b.BillPeriodEnd >= mStart && b.BillPeriodEnd < mEnd)
				.Sum(b => b.TotalCarbonEmission);

			// Calculate average carbon emission for all users this month: aggregate total emission per user (Activity counts Food/Transport only), then average across users
			var perUser = new Dictionary<int, decimal>();
			// Activity (Food/Transport only)
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
			// UtilityBills (counted by bill end month)
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


