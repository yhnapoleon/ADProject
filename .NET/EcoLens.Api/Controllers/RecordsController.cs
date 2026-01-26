using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
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

	public class RecordDto
	{
		public int Id { get; set; }
		public string Date { get; set; } = string.Empty; // YYYY-MM-DD
		public string Type { get; set; } = "Food"; // EmissionType
		public decimal Amount { get; set; } // kg CO2e
		public string Unit { get; set; } = "kg CO₂e";
		public string Description { get; set; } = string.Empty;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<RecordDto>>> List([FromQuery] string? type, [FromQuery] string? month, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		if (page <= 0) page = 1;
		if (pageSize <= 0 || pageSize > 200) pageSize = 50;

		IQueryable<ActivityLog> query = _db.ActivityLogs
			.AsNoTracking()
			.Include(l => l.CarbonReference)
			.Where(l => l.UserId == userId.Value);

		if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<CarbonCategory>(type, true, out var cat))
		{
			query = query.Where(l => l.CarbonReference != null && l.CarbonReference.Category == cat);
		}
		if (!string.IsNullOrWhiteSpace(month) && month.Length == 7)
		{
			query = query.Where(l => l.CreatedAt.ToString("yyyy-MM") == month);
		}

		var items = await query
			.OrderByDescending(l => l.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(l => new RecordDto
			{
				Id = l.Id,
				Date = l.CreatedAt.ToString("yyyy-MM-dd"),
				Type = l.CarbonReference != null ? l.CarbonReference.Category.ToString() : "Food",
				Amount = l.TotalEmission,
				Unit = "kg CO₂e",
				Description = l.DetectedLabel ?? (l.CarbonReference != null ? l.CarbonReference.LabelName : string.Empty)
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	/// <summary>
	/// 带 total 的分页列表（与前端文档期望对齐）。
	/// </summary>
	[HttpGet("list")]
	public async Task<ActionResult<object>> ListWithTotal([FromQuery] string? type, [FromQuery] string? month, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		if (page <= 0) page = 1;
		if (pageSize <= 0 || pageSize > 200) pageSize = 50;

		IQueryable<ActivityLog> query = _db.ActivityLogs
			.AsNoTracking()
			.Include(l => l.CarbonReference)
			.Where(l => l.UserId == userId.Value);

		if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<CarbonCategory>(type, true, out var cat))
		{
			query = query.Where(l => l.CarbonReference != null && l.CarbonReference.Category == cat);
		}
		if (!string.IsNullOrWhiteSpace(month) && month.Length == 7)
		{
			query = query.Where(l => l.CreatedAt.ToString("yyyy-MM") == month);
		}

		var total = await query.CountAsync(ct);
		var items = await query
			.OrderByDescending(l => l.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(l => new RecordDto
			{
				Id = l.Id,
				Date = l.CreatedAt.ToString("yyyy-MM-dd"),
				Type = l.CarbonReference != null ? l.CarbonReference.Category.ToString() : "Food",
				Amount = l.TotalEmission,
				Unit = "kg CO₂e",
				Description = l.DetectedLabel ?? (l.CarbonReference != null ? l.CarbonReference.LabelName : string.Empty)
			})
			.ToListAsync(ct);

		return Ok(new { items, total });
	}

	public class CreateRecordRequest
	{
		public string? Date { get; set; } // YYYY-MM-DD
		public string Type { get; set; } = "Food"; // EmissionType
		public decimal Amount { get; set; } // kg CO2e
		public string Unit { get; set; } = "kg CO₂e";
		public string? Description { get; set; }
		public object? Extra { get; set; }
	}

	[HttpPost]
	public async Task<ActionResult<RecordDto>> Create([FromBody] CreateRecordRequest req, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		if (!Enum.TryParse<CarbonCategory>(req.Type, true, out var cat))
		{
			return BadRequest("Invalid type.");
		}
		if (string.IsNullOrWhiteSpace(req.Unit) || !req.Unit.Contains("kg", StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest("Unit must be kg CO₂e.");
		}

		// 为通用 CO2e 记录提供 1:1 因子
		var label = $"Generic-{cat}";
		var factor = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == label && c.Category == cat && c.Unit == "kgCO2e", ct);
		if (factor is null)
		{
			factor = new Models.CarbonReference
			{
				LabelName = label,
				Category = cat,
				Co2Factor = 1m,
				Unit = "kgCO2e",
				Source = "Manual"
			};
			await _db.CarbonReferences.AddAsync(factor, ct);
			await _db.SaveChangesAsync(ct);
		}

		var date = DateTime.UtcNow;
		if (!string.IsNullOrWhiteSpace(req.Date) && DateTime.TryParse(req.Date, out var parsed))
		{
			date = parsed;
		}

		var log = new ActivityLog
		{
			UserId = userId.Value,
			CarbonReferenceId = factor.Id,
			Quantity = req.Amount, // 与 TotalEmission 一致（因子=1）
			TotalEmission = req.Amount,
			ImageUrl = null,
			DetectedLabel = string.IsNullOrWhiteSpace(req.Description) ? label : req.Description,
			CreatedAt = date,
			UpdatedAt = date
		};
		await _db.ActivityLogs.AddAsync(log, ct);
		await _db.SaveChangesAsync(ct);

		var dto = new RecordDto
		{
			Id = log.Id,
			Date = log.CreatedAt.ToString("yyyy-MM-dd"),
			Type = cat.ToString(),
			Amount = log.TotalEmission,
			Unit = "kg CO₂e",
			Description = log.DetectedLabel ?? label
		};
		return Ok(dto);
	}

	[HttpDelete("{id:int}")]
	public async Task<ActionResult<object>> Delete([FromRoute] int id, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		var log = await _db.ActivityLogs.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId.Value, ct);
		if (log is null) return NotFound();
		_db.ActivityLogs.Remove(log);
		await _db.SaveChangesAsync(ct);
		return Ok(new { deleted = true });
	}
}

