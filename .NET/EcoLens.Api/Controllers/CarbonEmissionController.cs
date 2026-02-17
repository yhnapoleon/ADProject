using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.CarbonEmission;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

/// <summary>
/// 碳排放记录批量操作控制器
/// </summary>
[ApiController]
[Route("api/carbon-emission")]
[Authorize]
public class CarbonEmissionController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IPointService _pointService;
	private readonly ILogger<CarbonEmissionController> _logger;

	public CarbonEmissionController(
		ApplicationDbContext db,
		IPointService pointService,
		ILogger<CarbonEmissionController> logger)
	{
		_db = db;
		_pointService = pointService;
		_logger = logger;
	}

	/// <summary>
	/// 从 JWT Token 中获取用户ID
	/// </summary>
	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 批量删除（按类型）当前用户的碳排放记录。
	/// </summary>
	/// <remarks>
	/// 请求体为数组形式的条目，每条包含 type 与 id：
	/// 
	/// - type: 1 = food（食物记录）
	/// - type: 2 = travel（出行记录）
	/// - type: 3 = utility（水电账单）
	/// 
	/// 示例：
	/// [
	///   { "type": 1, "id": 101 },
	///   { "type": 2, "id": 202 },
	///   { "type": 3, "id": 303 }
	/// ]
	/// </remarks>
	[HttpPost("batch-delete-typed")]
	[ProducesResponseType(typeof(BatchDeleteTypedResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<BatchDeleteTypedResponseDto>> BatchDeleteTyped(
		[FromBody] List<BatchDeleteItemDto> items,
		CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		if (items == null || items.Count == 0)
		{
			return BadRequest(new { error = "Request items cannot be empty" });
		}

		try
		{
			var foodIds = items.Where(i => i.Type == 1).Select(i => i.Id).Distinct().ToList();
			var travelIds = items.Where(i => i.Type == 2).Select(i => i.Id).Distinct().ToList();
			var utilityIds = items.Where(i => i.Type == 3).Select(i => i.Id).Distinct().ToList();

			if (foodIds.Count == 0 && travelIds.Count == 0 && utilityIds.Count == 0)
			{
				return BadRequest(new { error = "No valid items to delete (supported types are 1, 2, 3)" });
			}

			var result = new BatchDeleteTypedResponseDto();

			// 删除食物记录（FoodRecord）
			if (foodIds.Count > 0)
			{
				var foodRecords = await _db.FoodRecords
					.Where(f => f.UserId == userId.Value && foodIds.Contains(f.Id))
					.ToListAsync(ct);
				if (foodRecords.Count > 0)
				{
					_db.FoodRecords.RemoveRange(foodRecords);
					result.FoodRecordsDeleted = foodRecords.Count;
					_logger.LogInformation("Deleting {Count} food records for UserId={UserId}", foodRecords.Count, userId);
				}
			}

			// 删除出行记录（TravelLog）
			if (travelIds.Count > 0)
			{
				var travelLogs = await _db.TravelLogs
					.Where(t => t.UserId == userId.Value && travelIds.Contains(t.Id))
					.ToListAsync(ct);
				if (travelLogs.Count > 0)
				{
					_db.TravelLogs.RemoveRange(travelLogs);
					result.TravelLogsDeleted = travelLogs.Count;
					_logger.LogInformation("Deleting {Count} travel logs for UserId={UserId}", travelLogs.Count, userId);
				}
			}

			// 删除水电账单（UtilityBill）
			if (utilityIds.Count > 0)
			{
				var utilityBills = await _db.UtilityBills
					.Where(u => u.UserId == userId.Value && utilityIds.Contains(u.Id))
					.ToListAsync(ct);
				if (utilityBills.Count > 0)
				{
					_db.UtilityBills.RemoveRange(utilityBills);
					result.UtilityBillsDeleted = utilityBills.Count;
					_logger.LogInformation("Deleting {Count} utility bills for UserId={UserId}", utilityBills.Count, userId);
				}
			}

			await _db.SaveChangesAsync(ct);

			// 更新用户总碳排放（按现有定义：Activity + Travel + Utility，不包含 FoodRecord）
			await UpdateUserTotalCarbonEmissionAsync(userId.Value, ct);

			// 重新计算总碳减排（忽略异常）
			try
			{
				await _pointService.RecalculateTotalCarbonSavedAsync(userId.Value);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to recalculate TotalCarbonSaved for UserId={UserId}", userId);
			}

			result.TotalDeleted = result.FoodRecordsDeleted + result.TravelLogsDeleted + result.UtilityBillsDeleted;

			_logger.LogInformation(
				"Batch delete (typed) completed for UserId={UserId}: Food={Food}, Travel={Travel}, Utility={Utility}, Total={Total}",
				userId, result.FoodRecordsDeleted, result.TravelLogsDeleted, result.UtilityBillsDeleted, result.TotalDeleted);

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during batch delete (typed) for UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 更新用户总碳排放（从 ActivityLogs、TravelLogs、UtilityBills 汇总）
	/// </summary>
	private async Task UpdateUserTotalCarbonEmissionAsync(int userId, CancellationToken ct)
	{
		try
		{
			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
			if (user == null) return;

			var activityEmission = await _db.ActivityLogs
				.Where(a => a.UserId == userId)
				.SumAsync(a => (decimal?)a.TotalEmission, ct) ?? 0m;

			var travelEmission = await _db.TravelLogs
				.Where(t => t.UserId == userId)
				.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

			var utilityEmission = await _db.UtilityBills
				.Where(u => u.UserId == userId)
				.SumAsync(u => (decimal?)u.TotalCarbonEmission, ct) ?? 0m;

			user.TotalCarbonEmission = activityEmission + travelEmission + utilityEmission;
			await _db.SaveChangesAsync(ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to update TotalCarbonEmission for user {UserId}", userId);
		}
	}
}
