using System.Linq;
using System.Security.Claims;
using EcoLens.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/trees")]
[Authorize]
public class TreesController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private const int StepsPerTree = 15000; // 与移动端交互契合：约 150 步=1% -> 15000 步=100%

	public TreesController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	private static object BuildStats(int todaySteps, long totalSteps)
	{
		var totalPlantedTrees = (int)(totalSteps / StepsPerTree);
		var currentTreeGrowth = todaySteps <= 0 ? 0 : (int)System.Math.Min(100, System.Math.Floor((todaySteps * 100.0) / StepsPerTree));
		return new
		{
			currentTreeGrowth, // 0-100
			totalPlantedTrees,
			todaySteps
		};
	}

	[HttpGet("stats")]
	public async Task<ActionResult<object>> Stats(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var today = DateTime.UtcNow.Date;
		var todaySteps = await _db.StepRecords
			.Where(r => r.UserId == userId.Value && r.RecordDate == today)
			.Select(r => r.StepCount)
			.FirstOrDefaultAsync(ct);

		var totalSteps = await _db.StepRecords
			.Where(r => r.UserId == userId.Value)
			.LongSumAsync(r => (long)r.StepCount, ct);

		return Ok(BuildStats(todaySteps, totalSteps));
	}

	public sealed class ConvertStepsRequest
	{
		public int Steps { get; set; }
	}

	[HttpPost("convert-steps")]
	public async Task<ActionResult<object>> ConvertSteps([FromBody] ConvertStepsRequest req, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		if (req.Steps < 0) return BadRequest("steps must be non-negative.");

		var today = DateTime.UtcNow.Date;
		var record = await _db.StepRecords.FirstOrDefaultAsync(r => r.UserId == userId.Value && r.RecordDate == today, ct);
		if (record is null)
		{
			record = new Models.StepRecord
			{
				UserId = userId.Value,
				RecordDate = today,
				StepCount = req.Steps,
				CarbonOffset = (decimal)req.Steps * 0.0001m // 与 StepController 同口径
			};
			await _db.StepRecords.AddAsync(record, ct);
		}
		else
		{
			record.StepCount = req.Steps;
			record.CarbonOffset = (decimal)req.Steps * 0.0001m;
		}

		await _db.SaveChangesAsync(ct);

		var totalSteps = await _db.StepRecords
			.Where(r => r.UserId == userId.Value)
			.LongSumAsync(r => (long)r.StepCount, ct);

		return Ok(BuildStats(record.StepCount, totalSteps));
	}
}

internal static class EfCoreExtensions
{
	public static async Task<long> LongSumAsync<TSource>(this IQueryable<TSource> source, Func<TSource, long> selector, CancellationToken ct)
	{
		long sum = 0;
		await foreach (var item in source.AsAsyncEnumerable().WithCancellation(ct))
		{
			sum += selector(item);
		}
		return sum;
	}
}

