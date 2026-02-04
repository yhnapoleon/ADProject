using System.Linq;
using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Services;
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
	private readonly IPointService _pointService;
	private const int StepsPerTree = 15000; // 与移动端交互契合：约 150 步=1% -> 15000 步=100%
		private const int StepsPerProgressPoint = StepsPerTree / 100; // 150 步 == 1 进度点

	public TreesController(ApplicationDbContext db, IPointService pointService)
	{
		_db = db;
		_pointService = pointService;
	}

		// 兼容别名：/api/getTree 与 /api/postTree（与前端约定）
		[HttpGet("/api/getTree")]
		public Task<ActionResult<object>> GetTreeAlias(CancellationToken ct) => GetState(ct);

		public sealed class PostTreeRequest
		{
			public int? TotalTrees { get; set; }
			public int? CurrentProgress { get; set; }
		}

		/// <summary>
		/// 获取或更新树状态（兼容 GET 和 POST）
		/// GET: 返回当前状态（包括步数信息）
		/// POST: 更新状态并返回完整信息（包括步数信息）
		/// </summary>
		[HttpPost("/api/postTree")]
		public async Task<ActionResult<object>> PostTreeAlias([FromBody] PostTreeRequest? req, CancellationToken ct)
		{
			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var today = DateTime.UtcNow.Date;

			// 读取用户信息和今日步数
			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
			if (user is null) return NotFound();

			// 读取今日步数记录
			var stepRecord = await _db.StepRecords.FirstOrDefaultAsync(r => r.UserId == userId.Value && r.RecordDate == today, ct);
			var todaySteps = stepRecord?.StepCount ?? 0;

			// 处理每日重置：若最后消耗日期不是今天，则清零已用步数并刷新日期
			if (!user.LastStepUsageDate.HasValue || user.LastStepUsageDate.Value.Date != today)
			{
				user.StepsUsedToday = 0;
				user.LastStepUsageDate = today;
				await _db.SaveChangesAsync(ct);
			}

			// 计算可投入的步数
			var availableSteps = Math.Max(0, todaySteps - user.StepsUsedToday);

			// 如果是 POST 请求且有更新数据，则更新状态
			if (req != null && (req.TotalTrees.HasValue || req.CurrentProgress.HasValue))
			{
				var dto = new UpdateTreeStateRequest
				{
					TotalTrees = req.TotalTrees,
					CurrentProgress = req.CurrentProgress
				};
				await UpdateState(dto, ct);
				
				// 重新读取用户信息以获取最新状态
				user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
				if (user is null) return NotFound();
			}

			// 返回完整状态信息（包括步数）
			return Ok(new
			{
				totalTrees = user.TreesTotalCount,
				currentProgress = user.CurrentTreeProgress,
				todaySteps = todaySteps,
				availableSteps = availableSteps,
				status = user.CurrentTreeProgress >= 100 ? "completed" : (user.CurrentTreeProgress > 0 ? "growing" : "idle"),
				pointsTotal = user.CurrentPoints
			});
		}

		private static object BuildState(int totalTrees, int currentProgress, int? pointsTotal = null)
		{
			var status = currentProgress >= 100 ? "completed" : (currentProgress > 0 ? "growing" : "idle");
			var payload = new
			{
				totalTrees,
				currentProgress, // 0-100
				status,
				pointsTotal
			};
			return payload;
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
				.Select(u => new { u.TreesTotalCount, u.CurrentTreeProgress, u.CurrentPoints })
				.FirstOrDefaultAsync(ct);

			if (user is null) return NotFound();

			return Ok(BuildState(user.TreesTotalCount, user.CurrentTreeProgress, user.CurrentPoints));
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

			var oldTotal = user.TreesTotalCount;

			if (req.TotalTrees.HasValue) user.TreesTotalCount = req.TotalTrees.Value;
			if (req.CurrentProgress.HasValue) user.CurrentTreeProgress = req.CurrentProgress.Value;

			await _db.SaveChangesAsync(ct);

			// 新增树奖励：当总树数增加时，发放积分（每棵树 +30）
			var plantedDelta = user.TreesTotalCount - oldTotal;
			if (plantedDelta > 0)
			{
				try
				{
					await _pointService.AwardTreePlantingPointsAsync(userId.Value, plantedDelta);
				}
				catch (Exception)
				{
					// 忽略积分发放异常，避免影响树状态更新主流程
				}
			}

			// 返回最新积分
			return Ok(BuildState(user.TreesTotalCount, user.CurrentTreeProgress, user.CurrentPoints));
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

		// 步数更新后，重算总碳减排（基于每日净碳值）
		try
		{
			await _pointService.RecalculateTotalCarbonSavedAsync(userId.Value);
		}
		catch
		{
			// 忽略重算异常，避免影响前端交互流程
		}

		var totalSteps = await _db.StepRecords
			.Where(r => r.UserId == userId.Value)
			.LongSumAsync(r => (long)r.StepCount, ct);

		return Ok(BuildStats(record.StepCount, totalSteps));
	}

	/// <summary>
	/// 消耗“可用步数”用于树木生长：按 150 步 = 1 进度点；累计到达或超过 100 时结算为种下一棵树。
	/// </summary>
	[HttpPost("grow")]
	public async Task<ActionResult<object>> Grow(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var today = DateTime.UtcNow.Date;

		// 读取用户与当日步数
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		var record = await _db.StepRecords.FirstOrDefaultAsync(r => r.UserId == userId.Value && r.RecordDate == today, ct);
		var totalStepsToday = record?.StepCount ?? 0;

		// 每日重置：若最后消耗日期不是今天，则清零已用步数并刷新日期
		if (!user.LastStepUsageDate.HasValue || user.LastStepUsageDate.Value.Date != today)
		{
			user.StepsUsedToday = 0;
			user.LastStepUsageDate = today;
		}

		var availableSteps = Math.Max(0, totalStepsToday - Math.Max(0, user.StepsUsedToday));
		if (availableSteps <= 0)
		{
			return BadRequest(new { message = "No steps available", availableSteps });
		}

		// 消耗全部可用步数
		user.StepsUsedToday += availableSteps;

		// 按步数换算树进度（向下取整）
		var progressDelta = availableSteps / StepsPerProgressPoint;
		if (progressDelta > 0)
		{
			user.CurrentTreeProgress += progressDelta;

			// 结算成树（可能一次性跨越多个 100 档位）
			if (user.CurrentTreeProgress >= 100)
			{
				var planted = user.CurrentTreeProgress / 100;
				user.TreesTotalCount += planted;
				user.CurrentTreeProgress = user.CurrentTreeProgress % 100;

				// 积分奖励：每棵树 +30
				try
				{
					await _pointService.AwardTreePlantingPointsAsync(userId.Value, planted);
				}
				catch (Exception)
				{
					// 忽略积分发放异常，避免影响主流程
				}
			}
		}

		await _db.SaveChangesAsync(ct);

		// 保存后，可用步数应为 0（针对今天）
		var availableAfter = Math.Max(0, (record?.StepCount ?? 0) - Math.Max(0, user.StepsUsedToday));
		return Ok(new
		{
			TreesTotalCount = user.TreesTotalCount,
			CurrentTreeProgress = user.CurrentTreeProgress,
			AvailableSteps = availableAfter
		});
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

