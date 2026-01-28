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

		// 兼容别名：/api/getTree 与 /api/postTree（与前端约定）
		[HttpGet("/api/getTree")]
		public Task<ActionResult<object>> GetTreeAlias(CancellationToken ct) => GetState(ct);

		public sealed class PostTreeRequest
		{
			public int? TotalTrees { get; set; }
			public int? CurrentProgress { get; set; }
		}

		[HttpPost("/api/postTree")]
		public Task<ActionResult<object>> PostTreeAlias([FromBody] PostTreeRequest req, CancellationToken ct)
		{
			var dto = new UpdateTreeStateRequest
			{
				TotalTrees = req.TotalTrees,
				CurrentProgress = req.CurrentProgress
			};
			return UpdateState(dto, ct);
		}

		private static object BuildState(int totalTrees, int currentProgress)
		{
			var status = currentProgress >= 100 ? "completed" : (currentProgress > 0 ? "growing" : "idle");
			return new
			{
				totalTrees,
				currentProgress, // 0-100
				status
			};
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

		/// <summary>
		/// 获取当前树的数量与状态（包含：totalTrees、currentProgress、status）。
		/// </summary>
		[HttpGet("state")]
		public async Task<ActionResult<object>> GetState(CancellationToken ct)
		{
			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _db.ApplicationUsers
				.Where(u => u.Id == userId.Value)
				.Select(u => new { u.TreesTotalCount, u.CurrentTreeProgress })
				.FirstOrDefaultAsync(ct);

			if (user is null) return NotFound();

			return Ok(BuildState(user.TreesTotalCount, user.CurrentTreeProgress));
		}

		public sealed class UpdateTreeStateRequest
		{
			public int? TotalTrees { get; set; }      // 可选：不传则不更新
			public int? CurrentProgress { get; set; } // 可选：不传则不更新（0-100）
		}

		/// <summary>
		/// 更新当前树的数量与状态（通过 currentProgress 表达状态）。
		/// </summary>
		[HttpPut("state")]
		public async Task<ActionResult<object>> UpdateState([FromBody] UpdateTreeStateRequest req, CancellationToken ct)
		{
			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			if (req.TotalTrees is null && req.CurrentProgress is null)
			{
				return BadRequest("At least one field is required: TotalTrees or CurrentProgress.");
			}

			if (req.TotalTrees is < 0)
			{
				return BadRequest("TotalTrees must be >= 0.");
			}

			if (req.CurrentProgress is < 0 or > 100)
			{
				return BadRequest("CurrentProgress must be in range [0, 100].");
			}

			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
			if (user is null) return NotFound();

			if (req.TotalTrees.HasValue) user.TreesTotalCount = req.TotalTrees.Value;
			if (req.CurrentProgress.HasValue) user.CurrentTreeProgress = req.CurrentProgress.Value;

			await _db.SaveChangesAsync(ct);

			return Ok(BuildState(user.TreesTotalCount, user.CurrentTreeProgress));
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

