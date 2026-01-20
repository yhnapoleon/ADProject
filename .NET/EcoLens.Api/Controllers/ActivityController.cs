using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public ActivityController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 上传活动记录：根据标签查找参考，计算排放并保存。
	/// </summary>
	[HttpPost("upload")]
	public async Task<ActionResult<ActivityLogResponseDto>> Upload([FromForm] CreateActivityLogDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == dto.Label, ct);
		if (carbonRef is null)
		{
			return NotFound("Carbon reference not found for the given label.");
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

		var result = new ActivityLogResponseDto
		{
			Id = log.Id,
			Label = carbonRef.LabelName,
			Quantity = log.Quantity,
			TotalEmission = log.TotalEmission,
			ImageUrl = log.ImageUrl,
			CreatedAt = log.CreatedAt
		};

		return Ok(result);
	}

	/// <summary>
	/// 获取当前用户的活动日志列表。
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
				CreatedAt = l.CreatedAt
			})
			.ToListAsync(ct);

		return Ok(logs);
	}

	/// <summary>
	/// 获取当前用户的统计信息（总排放与记录数量）。
	/// </summary>
	[HttpGet("stats")]
	public async Task<ActionResult<object>> Stats(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var query = _db.ActivityLogs.Where(l => l.UserId == userId.Value);
		var totalItems = await query.CountAsync(ct);
		var totalEmission = await query.SumAsync(l => (decimal?)l.TotalEmission, ct) ?? 0m;

		return Ok(new { totalEmission, totalItems });
	}
}

