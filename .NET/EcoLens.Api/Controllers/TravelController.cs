using System.Security.Claims;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

/// <summary>
/// 出行记录控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TravelController : ControllerBase
{
	private readonly ITravelService _travelService;
	private readonly ILogger<TravelController> _logger;

	public TravelController(ITravelService travelService, ILogger<TravelController> logger)
	{
		_travelService = travelService;
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
	/// 创建出行记录
	/// </summary>
	/// <remarks>
	/// 根据出发地和目的地地址，调用 Google Maps API 计算路线和距离，
	/// 根据出行方式计算碳排放量，并保存到数据库。
	/// 
	/// **处理流程：**
	/// 1. 地址转坐标（Geocoding API）
	/// 2. 获取路线信息（Directions API，包含距离、时间、polyline）
	/// 3. 根据出行方式查找碳排放因子
	/// 4. 计算碳排放（距离 × 碳排放因子）
	/// 5. 保存到数据库
	/// 
	/// **示例请求：**
	/// ```json
	/// {
	///   "originAddress": "北京市朝阳区",
	///   "destinationAddress": "北京市海淀区",
	///   "transportMode": 3,
	///   "notes": "上班通勤"
	/// }
	/// ```
	/// </remarks>
	/// <param name="dto">创建出行记录的请求数据</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>创建成功的出行记录详情</returns>
	/// <response code="200">创建成功，返回出行记录详情（包含路线 polyline）</response>
	/// <response code="400">请求参数错误、地址无法解析或路线获取失败</response>
	/// <response code="401">未授权，需要登录（Token 无效或过期）</response>
	/// <response code="500">服务器内部错误</response>
	[HttpPost]
	[ProducesResponseType(typeof(TravelLogResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<TravelLogResponseDto>> Create([FromBody] CreateTravelLogDto dto, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		if (!ModelState.IsValid)
		{
			return BadRequest(new { error = "Request validation failed", errors = ModelState });
		}

		try
		{
			var result = await _travelService.CreateTravelLogAsync(userId.Value, dto, ct);
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning(ex, "Failed to create travel log: UserId={UserId}, Error={Error}", userId, ex.Message);
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating travel log: UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 预览路线和碳排放（不保存到数据库）
	/// </summary>
	/// <remarks>
	/// 预览功能，用于在保存前查看路线信息、距离、时间和预估碳排放量。
	/// 不会创建数据库记录，适合在用户确认前预览结果。
	/// 
	/// **处理流程：**
	/// 1. 地址转坐标（Geocoding API）
	/// 2. 获取路线信息（Directions API）
	/// 3. 计算预估碳排放
	/// 4. 返回预览结果（不保存）
	/// 
	/// **示例请求：**
	/// ```json
	/// {
	///   "originAddress": "北京市朝阳区",
	///   "destinationAddress": "北京市海淀区",
	///   "transportMode": 3
	/// }
	/// ```
	/// </remarks>
	/// <param name="dto">创建出行记录的请求数据</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>路线预览信息（包含距离、时间、预估碳排放、polyline）</returns>
	/// <response code="200">预览成功，返回路线和碳排放信息</response>
	/// <response code="400">请求参数错误或地址无法解析</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="500">服务器内部错误</response>
	[HttpPost("preview")]
	[ProducesResponseType(typeof(RoutePreviewDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<RoutePreviewDto>> Preview([FromBody] CreateTravelLogDto dto, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		if (!ModelState.IsValid)
		{
			return BadRequest(new { error = "Request validation failed", errors = ModelState });
		}

		try
		{
			var result = await _travelService.PreviewRouteAsync(dto, ct);
			return Ok(result);
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning(ex, "Failed to preview route: UserId={UserId}, Error={Error}", userId, ex.Message);
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error previewing route: UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 获取当前用户的出行记录列表（支持筛选和分页）
	/// </summary>
	/// <remarks>
	/// 获取当前登录用户的出行记录，支持按日期范围、出行方式筛选，并支持分页。
	/// 
	/// **查询参数：**
	/// - startDate（可选）：开始日期（格式：yyyy-MM-dd）
	/// - endDate（可选）：结束日期（格式：yyyy-MM-dd）
	/// - transportMode（可选）：出行方式（0-9，见枚举说明）
	/// - page（可选）：页码，默认1
	/// - pageSize（可选）：每页数量，默认20，最大100
	/// 
	/// **返回数据：**
	/// - 分页结果，包含数据列表、总数、页码等信息
	/// - 每条记录包含：出发地、目的地、出行方式、距离、碳排放、创建时间等
	/// - 包含路线 polyline，可用于前端绘制地图
	/// 
	/// **示例请求：**
	/// ```
	/// GET /api/travel/my-travels
	/// GET /api/travel/my-travels?startDate=2024-01-01&endDate=2024-01-31&transportMode=3&page=1&pageSize=20
	/// ```
	/// </remarks>
	/// <param name="query">查询参数</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>分页的出行记录列表</returns>
	/// <response code="200">获取成功，返回分页的出行记录列表</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="500">服务器内部错误</response>
	[HttpGet("my-travels")]
	[ProducesResponseType(typeof(PagedResultDto<TravelLogResponseDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<PagedResultDto<TravelLogResponseDto>>> GetMyTravels(
		[FromQuery] GetTravelLogsQueryDto query,
		CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		if (!ModelState.IsValid)
		{
			return BadRequest(new { error = "Request validation failed", errors = ModelState });
		}

		try
		{
			var result = await _travelService.GetUserTravelLogsAsync(userId.Value, query, ct);
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting travel logs list: UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 根据ID获取单条出行记录详情
	/// </summary>
	/// <remarks>
	/// 获取指定ID的出行记录详细信息。只能获取当前用户自己的记录。
	/// 
	/// **返回数据：**
	/// - 完整的出行记录信息
	/// - 包含路线 polyline，可用于前端绘制地图
	/// - 包含出发地和目的地的坐标，可用于地图标记
	/// </remarks>
	/// <param name="id">出行记录ID</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>出行记录详情</returns>
	/// <response code="200">获取成功，返回出行记录详情</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="404">记录不存在或不属于当前用户</response>
	/// <response code="500">服务器内部错误</response>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(TravelLogResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<TravelLogResponseDto>> GetById(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		try
		{
			var result = await _travelService.GetTravelLogByIdAsync(id, userId.Value, ct);
			if (result == null)
			{
				return NotFound(new { error = "Travel log not found or access denied" });
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting travel log details: UserId={UserId}, TravelLogId={TravelLogId}", userId, id);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 删除出行记录
	/// </summary>
	/// <remarks>
	/// 删除指定ID的出行记录。只能删除当前用户自己的记录。
	/// 
	/// **注意：**
	/// - 删除操作不可恢复
	/// - 只能删除自己的记录
	/// </remarks>
	/// <param name="id">出行记录ID</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>删除结果</returns>
	/// <response code="200">删除成功</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="404">记录不存在或不属于当前用户</response>
	/// <response code="500">服务器内部错误</response>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult> Delete(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		try
		{
			var success = await _travelService.DeleteTravelLogAsync(id, userId.Value, ct);
			if (!success)
			{
				return NotFound(new { error = "Travel log not found or access denied" });
			}

			return Ok(new { message = "Deleted successfully" });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting travel log: UserId={UserId}, TravelLogId={TravelLogId}", userId, id);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 获取当前用户的出行记录统计信息
	/// </summary>
	/// <remarks>
	/// 获取当前登录用户的出行记录统计信息，包括总记录数、总距离、总碳排放量，以及按出行方式的统计。
	/// 
	/// **查询参数：**
	/// - `startDate`（可选）：开始日期（格式：yyyy-MM-dd）
	/// - `endDate`（可选）：结束日期（格式：yyyy-MM-dd）
	/// 
	/// **返回数据：**
	/// - 总体统计：总记录数、总距离、总碳排放
	/// - 按出行方式统计：每种出行方式的记录数、距离、碳排放
	/// 
	/// **示例请求：**
	/// ```
	/// GET /api/travel/statistics
	/// GET /api/travel/statistics?startDate=2024-01-01&endDate=2024-01-31
	/// ```
	/// </remarks>
	/// <param name="startDate">开始日期（可选）</param>
	/// <param name="endDate">结束日期（可选）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>统计信息</returns>
	/// <response code="200">获取成功，返回统计信息</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="500">服务器内部错误</response>
	[HttpGet("statistics")]
	[ProducesResponseType(typeof(TravelStatisticsDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<TravelStatisticsDto>> GetStatistics(
		[FromQuery] DateTime? startDate = null,
		[FromQuery] DateTime? endDate = null,
		CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		try
		{
			var result = await _travelService.GetUserTravelStatisticsAsync(userId.Value, startDate, endDate, ct);
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting travel statistics: UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}
}
