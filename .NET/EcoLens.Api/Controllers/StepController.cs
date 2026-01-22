using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Steps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StepController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public StepController(ApplicationDbContext db)
	{
		_db = db;
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
	public async Task<IActionResult> Sync([FromBody] SyncStepsRequestDto dto, CancellationToken ct)
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
		return Ok();
	}
}




