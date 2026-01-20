using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsightController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public InsightController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 生成一条模拟周报洞见（最近 7 天）。
	/// TODO: Send logs to Gemini/OpenAI API and return response
	/// </summary>
	[HttpGet("weekly-report")]
	public async Task<ActionResult<AiInsightDto>> WeeklyReport(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var since = DateTime.UtcNow.AddDays(-7);
		var logs = await _db.ActivityLogs
			.Where(l => l.UserId == userId.Value && l.CreatedAt >= since)
			.ToListAsync(ct);

		var content = "Based on your eating habits over the last week, try substituting steak with plant-based options twice to reduce your emissions.";

		var dto = new AiInsightDto
		{
			Id = 0,
			Content = content,
			Type = Models.Enums.InsightType.WeeklyReport,
			IsRead = false,
			CreatedAt = DateTime.UtcNow
		};

		return Ok(dto);
	}
}

