using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/records")]
[Authorize]
public class RecordsController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public RecordsController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	public sealed class RecordItemDto
	{
		public string Id { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty; // yyyy-MM-dd
		public CarbonCategory Type { get; set; } // EmissionType
		public decimal Amount { get; set; } // kg CO2e
		public string Unit { get; set; } = "kg CO2e";
		public string Description { get; set; } = string.Empty;
	}

	/// <summary>
	/// 获取当前用户的记录列表（仅 GET，需要的精简字段）。
	/// </summary>
	[HttpGet]
	public async Task<ActionResult<IEnumerable<RecordItemDto>>> List(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		// 获取 ActivityLogs（食物和水电记录）
		var activityItems = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value)
			.Include(l => l.CarbonReference)
			.OrderByDescending(l => l.CreatedAt)
			.Select(l => new RecordItemDto
			{
				Id = "activity_" + l.Id.ToString(),
				Date = l.CreatedAt.ToString("yyyy-MM-dd"),
				Type = l.CarbonReference!.Category,
				Amount = l.TotalEmission,
				Unit = "kg CO₂e",
				Description = l.DetectedLabel ?? l.CarbonReference!.LabelName
			})
			.ToListAsync(ct);

		// 获取 TravelLogs（出行记录）
		var travelItems = await _db.TravelLogs
			.Where(l => l.UserId == userId.Value)
			.OrderByDescending(l => l.CreatedAt)
			.Select(l => new RecordItemDto
			{
				Id = "travel_" + l.Id.ToString(),
				Date = l.CreatedAt.ToString("yyyy-MM-dd"),
				Type = CarbonCategory.Transport,
				Amount = l.CarbonEmission,
				Unit = "kg CO₂e",
				Description = l.OriginAddress + " → " + l.DestinationAddress + " (" + l.TransportMode.ToString() + ")"
			})
			.ToListAsync(ct);

		// 合并并按日期排序
		var allItems = activityItems.Concat(travelItems)
			.OrderByDescending(x => x.Date)
			.ToList();

		return Ok(allItems);
	}

	/// <summary>
	/// 删除指定记录（仅限当前用户的记录）。
	/// 支持删除 ActivityLog 和 TravelLog。
	/// ID格式：activity_123 或 travel_456
	/// </summary>
	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		// 解析ID前缀，确定记录类型
		if (id.StartsWith("activity_"))
		{
			var activityId = int.Parse(id.Replace("activity_", ""));
			var log = await _db.ActivityLogs.FirstOrDefaultAsync(l => l.Id == activityId && l.UserId == userId.Value, ct);
			if (log is null) return NotFound();

			_db.ActivityLogs.Remove(log);
			await _db.SaveChangesAsync(ct);
			return NoContent();
		}
		else if (id.StartsWith("travel_"))
		{
			var travelId = int.Parse(id.Replace("travel_", ""));
			var log = await _db.TravelLogs.FirstOrDefaultAsync(l => l.Id == travelId && l.UserId == userId.Value, ct);
			if (log is null) return NotFound();

			_db.TravelLogs.Remove(log);
			await _db.SaveChangesAsync(ct);
			return NoContent();
		}
		else
		{
			return BadRequest(new { error = "Invalid ID format. Expected format: activity_123 or travel_456" });
		}
	}
}
