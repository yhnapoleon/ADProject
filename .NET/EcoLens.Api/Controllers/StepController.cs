using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Steps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLens.Api.Services;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StepController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IPointService _pointService;

	public StepController(ApplicationDbContext db, IPointService pointService)
	{
		_db = db;
		_pointService = pointService;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 同步计步数据：若当日存在记录则更新，否则创建。按照每 1000 步 = 0.1 kg 减排计算 CarbonOffset。
	/// 自动累计到用户 TotalCarbonSaved 与 CurrentPoints。
	/// </summary>
	[HttpPost("sync")]
	public async Task<ActionResult<SyncStepsResponseDto>> Sync([FromBody] SyncStepsRequestDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		if (dto.StepCount < 0) return BadRequest("StepCount must be non-negative.");

		var date = dto.Date.Date;
		var record = await _db.StepRecords.FirstOrDefaultAsync(r => r.UserId == userId.Value && r.RecordDate == date, ct);

		var newOffset = (decimal)dto.StepCount * 0.0001m; // 1000 步 = 0.1 kg
		var pointsDelta = (int)decimal.Round(newOffset * 100m, 0, MidpointRounding.AwayFromZero);

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		// 日重置：以请求体中的 date（按其 date 部分）作为当日判定
		if (!user.LastStepUsageDate.HasValue || user.LastStepUsageDate.Value.Date != date)
		{
			user.StepsUsedToday = 0;
			user.LastStepUsageDate = date;
		}

		if (record is null)
		{
			record = new Models.StepRecord
			{
				UserId = userId.Value,
				StepCount = dto.StepCount,
				RecordDate = date,
				CarbonOffset = newOffset
			};
			await _db.StepRecords.AddAsync(record, ct);

			user.TotalCarbonSaved += newOffset;
			user.CurrentPoints += pointsDelta;
		}
		else
		{
			var oldOffset = record.CarbonOffset;
			var oldPoints = (int)decimal.Round(oldOffset * 100m, 0, MidpointRounding.AwayFromZero);

			record.StepCount = dto.StepCount;
			record.CarbonOffset = newOffset;

			var deltaOffset = newOffset - oldOffset;
			var deltaPoints = pointsDelta - oldPoints;

			user.TotalCarbonSaved += deltaOffset;
			user.CurrentPoints += deltaPoints;
		}

		await _db.SaveChangesAsync(ct);

		var total = record.StepCount;

		// 重算总碳减排（基于每日净碳值）
		try
		{
			await _pointService.RecalculateTotalCarbonSavedAsync(userId.Value);
		}
		catch
		{
			// 忽略错误，不影响步数同步主流程
		}
		var used = Math.Max(0, user.StepsUsedToday);
		var available = Math.Max(0, total - used);

		return Ok(new SyncStepsResponseDto
		{
			TotalSteps = total,
			UsedSteps = used,
			AvailableSteps = available
		});
	}
}





