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
	/// 批量删除碳排放记录
	/// </summary>
	/// <remarks>
	/// 批量删除当前用户的碳排放记录，支持删除活动记录、出行记录和水电账单。
	/// 
	/// **请求体格式：**
	/// ```json
	/// {
	///   "activityLogIds": [1, 2, 3],      // 活动记录ID列表（可选）
	///   "travelLogIds": [4, 5, 6],       // 出行记录ID列表（可选）
	///   "utilityBillIds": [7, 8, 9]      // 水电账单ID列表（可选）
	/// }
	/// ```
	/// 
	/// **注意事项：**
	/// - 只能删除当前用户自己的记录
	/// - 删除操作不可恢复
	/// - 删除后会自动更新用户的总碳排放量
	/// - 至少需要提供一个非空的ID列表
	/// </remarks>
	/// <param name="dto">批量删除请求数据</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>删除结果统计</returns>
	/// <response code="200">删除成功，返回删除统计信息</response>
	/// <response code="400">请求参数错误（所有ID列表都为空）</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="500">服务器内部错误</response>
	[HttpPost("batch-delete")]
	[ProducesResponseType(typeof(BatchDeleteResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<BatchDeleteResponseDto>> BatchDelete(
		[FromBody] BatchDeleteCarbonEmissionDto dto,
		CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		// 验证至少有一个非空的ID列表
		var hasActivityLogs = dto.ActivityLogIds != null && dto.ActivityLogIds.Count > 0;
		var hasTravelLogs = dto.TravelLogIds != null && dto.TravelLogIds.Count > 0;
		var hasUtilityBills = dto.UtilityBillIds != null && dto.UtilityBillIds.Count > 0;

		if (!hasActivityLogs && !hasTravelLogs && !hasUtilityBills)
		{
			return BadRequest(new { error = "At least one non-empty ID list is required" });
		}

		try
		{
			var deletedCounts = new BatchDeleteResponseDto
			{
				ActivityLogsDeleted = 0,
				TravelLogsDeleted = 0,
				UtilityBillsDeleted = 0
			};

			// 删除活动记录
			if (hasActivityLogs)
			{
				var activityLogs = await _db.ActivityLogs
					.Where(a => a.UserId == userId.Value && dto.ActivityLogIds!.Contains(a.Id))
					.ToListAsync(ct);

				if (activityLogs.Count > 0)
				{
					_db.ActivityLogs.RemoveRange(activityLogs);
					deletedCounts.ActivityLogsDeleted = activityLogs.Count;
					_logger.LogInformation("Deleting {Count} activity logs for UserId={UserId}", activityLogs.Count, userId);
				}
			}

			// 删除出行记录
			if (hasTravelLogs)
			{
				var travelLogs = await _db.TravelLogs
					.Where(t => t.UserId == userId.Value && dto.TravelLogIds!.Contains(t.Id))
					.ToListAsync(ct);

				if (travelLogs.Count > 0)
				{
					_db.TravelLogs.RemoveRange(travelLogs);
					deletedCounts.TravelLogsDeleted = travelLogs.Count;
					_logger.LogInformation("Deleting {Count} travel logs for UserId={UserId}", travelLogs.Count, userId);
				}
			}

			// 删除水电账单
			if (hasUtilityBills)
			{
				var utilityBills = await _db.UtilityBills
					.Where(u => u.UserId == userId.Value && dto.UtilityBillIds!.Contains(u.Id))
					.ToListAsync(ct);

				if (utilityBills.Count > 0)
				{
					_db.UtilityBills.RemoveRange(utilityBills);
					deletedCounts.UtilityBillsDeleted = utilityBills.Count;
					_logger.LogInformation("Deleting {Count} utility bills for UserId={UserId}", utilityBills.Count, userId);
				}
			}

			// 保存所有更改
			await _db.SaveChangesAsync(ct);

			// 更新用户总碳排放
			await UpdateUserTotalCarbonEmissionAsync(userId.Value, ct);

			// 重新计算总碳减排
			try
			{
				await _pointService.RecalculateTotalCarbonSavedAsync(userId.Value);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to recalculate TotalCarbonSaved for UserId={UserId}", userId);
			}

			deletedCounts.TotalDeleted = deletedCounts.ActivityLogsDeleted + 
			                            deletedCounts.TravelLogsDeleted + 
			                            deletedCounts.UtilityBillsDeleted;

			_logger.LogInformation(
				"Batch delete completed for UserId={UserId}: ActivityLogs={ActivityLogs}, TravelLogs={TravelLogs}, UtilityBills={UtilityBills}, Total={Total}",
				userId, deletedCounts.ActivityLogsDeleted, deletedCounts.TravelLogsDeleted, 
				deletedCounts.UtilityBillsDeleted, deletedCounts.TotalDeleted);

			return Ok(deletedCounts);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during batch delete for UserId={UserId}", userId);
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
