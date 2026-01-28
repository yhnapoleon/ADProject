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

		var items = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value)
			.Include(l => l.CarbonReference)
			.OrderByDescending(l => l.CreatedAt)
			.Select(l => new RecordItemDto
			{
				Id = l.Id.ToString(),
				Date = l.CreatedAt.ToString("yyyy-MM-dd"),
				Type = l.CarbonReference!.Category,
				Amount = l.TotalEmission,
				Unit = "kg CO2e",
				Description = l.DetectedLabel ?? l.CarbonReference!.LabelName
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	/// <summary>
	/// 删除指定记录（仅限当前用户的记录）。
	/// </summary>
	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var log = await _db.ActivityLogs.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId.Value, ct);
		if (log is null) return NotFound();

		_db.ActivityLogs.Remove(log);
		await _db.SaveChangesAsync(ct);
		return NoContent();
	}
}
