using System;
using System.Security.Claims;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.DTOs.UtilityBill;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

/// <summary>
/// 水电账单管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UtilityBillController : ControllerBase
{
	private readonly IUtilityBillService _utilityBillService;
	private readonly ILogger<UtilityBillController> _logger;

	public UtilityBillController(
		IUtilityBillService utilityBillService,
		ILogger<UtilityBillController> logger)
	{
		_utilityBillService = utilityBillService;
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
	/// 上传账单文件并识别（OCR识别 → 数据提取 → 碳排放计算，不保存到数据库）
	/// </summary>
	/// <remarks>
	/// 上传账单文件（支持图片：JPG、PNG、BMP、WEBP，或PDF文件），系统会自动识别账单内容，
	/// 提取用量数据和账单周期，计算碳排放，但**不保存到数据库**。识别结果返回给用户确认后，
	/// 用户需调用 `/api/utilitybill/manual` 接口保存到数据库。
	/// 
	/// **处理流程：**
	/// 1. 文件验证（类型、大小）
	/// 2. OCR识别（Google Cloud Vision API）
	/// 3. 数据提取（从OCR文本中提取用量、日期等）
	/// 4. 碳排放计算（根据用量和排放因子）
	/// 5. 返回识别结果（Id = 0 表示还未保存）
	/// 
	/// **支持的文件格式：**
	/// - 图片：JPG、JPEG、PNG、BMP、WEBP
	/// - 文档：PDF（支持多页）
	/// 
	/// **文件大小限制：**
	/// - 最大 10MB
	/// 
	/// **示例请求：**
	/// ```
	/// POST /api/utilitybill/upload
	/// Content-Type: multipart/form-data
	/// 
	/// file: [账单图片或PDF文件]
	/// ```
	/// 
	/// **注意事项：**
	/// - 识别后不会自动保存，返回的响应中 Id = 0
	/// - 用户确认识别结果后，需调用 `/api/utilitybill/manual` 接口保存
	/// - 如果OCR识别失败，会返回错误，建议使用手动输入
	/// - 如果数据提取不完整，会返回错误，建议使用手动输入补充
	/// </remarks>
	/// <param name="dto">上传账单文件的请求数据</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>处理后的账单详情</returns>
	/// <response code="200">处理成功，返回账单详情（包含识别的数据和计算的碳排放）</response>
	/// <response code="400">文件验证失败、OCR识别失败或数据提取失败</response>
	/// <response code="401">未授权，需要登录（Token 无效或过期）</response>
	/// <response code="500">服务器内部错误</response>
	[HttpPost("upload")]
	[Consumes("multipart/form-data")]
	[RequestFormLimits(MultipartBodyLengthLimit = 10485760, ValueLengthLimit = 10485760)]
	[ProducesResponseType(typeof(UtilityBillResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<UtilityBillResponseDto>> Upload(
		[FromForm] UploadUtilityBillDto dto,
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
			var result = await _utilityBillService.UploadAndProcessBillAsync(userId.Value, dto.File, ct);
			return Ok(result);
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning(ex, "File validation failed: UserId={UserId}, Error={Error}", userId, ex.Message);
			return BadRequest(new { error = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning(ex, "Failed to process bill: UserId={UserId}, Error={Error}", userId, ex.Message);
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading and processing bill: UserId={UserId}", userId);
			var msg = ex.Message ?? ex.InnerException?.Message ?? "";
			var displayError = string.IsNullOrEmpty(msg) ? $"Internal server error ({ex.GetType().Name}). Please try again later." : msg;
			return StatusCode(500, new { error = displayError });
		}
	}

	/// <summary>
	/// 路由别名：/api/vision/utility-bill（与前端文档对齐，转到上传并处理）。
	/// </summary>
	[HttpPost("/api/vision/utility-bill")]
	[Consumes("multipart/form-data")]
	public Task<ActionResult<UtilityBillResponseDto>> UploadAlias([FromForm] UploadUtilityBillDto dto, CancellationToken ct = default)
		=> Upload(dto, ct);

	/// <summary>
	/// 手动创建账单或保存识别结果（碳排放计算 → 保存）
	/// </summary>
	/// <remarks>
	/// 手动输入账单数据，或保存通过 `/api/utilitybill/upload` 识别后的结果。
	/// 系统会自动计算碳排放并保存到数据库。
	/// 
	/// **处理流程：**
	/// 1. 输入验证（日期范围、用量等）
	/// 2. 碳排放计算（根据用量和排放因子）
	/// 3. 保存到数据库
	/// 
	/// **使用场景：**
	/// - 场景1：用户手动输入账单数据
	/// - 场景2：用户上传账单图片识别后，确认识别结果并保存
	/// 
	/// **账单类型说明：**
	/// - 0: 电费（Electricity）
	/// - 1: 水费（Water）
	/// - 2: 燃气费（Gas）
	/// - 3: 综合账单（Combined）
	/// 
	/// **用量单位：**
	/// - 用电量：kWh
	/// - 用水量：m³
	/// - 用气量：kWh 或 m³
	/// 
	/// **示例请求：**
	/// ```json
	/// {
	///   "billType": 0,
	///   "billPeriodStart": "2024-01-01",
	///   "billPeriodEnd": "2024-01-31",
	///   "electricityUsage": 150.5,
	///   "waterUsage": null,
	///   "gasUsage": null
	/// }
	/// ```
	/// </remarks>
	/// <param name="dto">手动输入账单数据</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>创建的账单详情</returns>
	/// <response code="200">创建成功，返回账单详情（包含计算的碳排放）</response>
	/// <response code="400">请求参数验证失败</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="500">服务器内部错误</response>
	[HttpPost("manual")]
	[ProducesResponseType(typeof(UtilityBillResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<UtilityBillResponseDto>> CreateManually(
		[FromBody] CreateUtilityBillManuallyDto dto,
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
			var result = await _utilityBillService.CreateBillManuallyAsync(userId.Value, dto, ct);
			return Ok(result);
		}
		catch (ArgumentException ex)
		{
			_logger.LogWarning(ex, "Invalid input data: UserId={UserId}, Error={Error}", userId, ex.Message);
			return BadRequest(new { error = ex.Message });
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning(ex, "Business rule violation: UserId={UserId}, Error={Error}", userId, ex.Message);
			return BadRequest(new { error = ex.Message });
		}
		catch (Exception ex)
		{
			var msg = ex.Message ?? "";
			var inner = ex.InnerException?.Message ?? "";
			// 收集完整异常链（含 DbUpdateException 内层），便于识别重复/约束错误
			var fullText = msg + " " + inner;
			var e = ex.InnerException;
			while (e != null)
			{
				if (!string.IsNullOrEmpty(e.Message)) fullText += " " + e.Message;
				e = e.InnerException;
			}

			// 重复账单：业务异常或数据库唯一约束/重复键
			if (fullText.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
			    fullText.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
			    fullText.Contains("UNIQUE", StringComparison.Ordinal) ||
			    fullText.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
			    fullText.Contains("cannot insert duplicate", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning(ex, "Duplicate bill or constraint: UserId={UserId}", userId);
				return BadRequest(new { error = "This bill already exists. Do not add duplicate." });
			}
			// 因子未找到等业务错误也返回 400
			if (msg.Contains("factor", StringComparison.OrdinalIgnoreCase) ||
			    msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogWarning(ex, "Config/business error: UserId={UserId}", userId);
				return BadRequest(new { error = msg });
			}
			_logger.LogError(ex, "Error creating bill manually: UserId={UserId}", userId);
			// 优先返回实际异常，fallback 用唯一文案便于确认是否跑的是新代码
			var errMsg = !string.IsNullOrEmpty(msg) ? msg : inner;
			var displayError = string.IsNullOrEmpty(errMsg)
				? $"Server error ({ex.GetType().Name}). Please try again."
				: errMsg;
			return StatusCode(500, new { error = displayError });
		}
	}

	// 注意：/api/records/utilities 已由 UtilityController 暴露，避免路由冲突，这里不再重复定义。

	/// <summary>
	/// 获取当前用户的账单列表（支持筛选和分页）
	/// </summary>
	/// <remarks>
	/// 获取当前登录用户的账单列表，支持按日期范围、账单类型筛选，并支持分页。
	/// 
	/// **查询参数：**
	/// - `startDate`（可选）：开始日期（格式：yyyy-MM-dd），基于账单周期结束日期筛选
	/// - `endDate`（可选）：结束日期（格式：yyyy-MM-dd），基于账单周期开始日期筛选
	/// - `billType`（可选）：账单类型筛选（0-3，见枚举说明）
	/// - `page`（可选）：页码，默认1，从1开始
	/// - `pageSize`（可选）：每页数量，默认20，最大100
	/// 
	/// **返回数据：**
	/// - 分页结果，包含数据列表、总数、页码等信息
	/// - 每条记录包含：账单类型、账单周期、用量、碳排放、输入方式、创建时间等
	/// 
	/// **示例请求：**
	/// ```
	/// GET /api/utility-bills/my-bills
	/// GET /api/utility-bills/my-bills?startDate=2024-01-01&amp;endDate=2024-01-31&amp;billType=0&amp;page=1&amp;pageSize=20
	/// ```
	/// </remarks>
	/// <param name="query">查询参数</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>分页的账单列表</returns>
	/// <response code="200">获取成功，返回分页的账单列表</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="500">服务器内部错误</response>
	[HttpGet("my-bills")]
	[ProducesResponseType(typeof(PagedResultDto<UtilityBillResponseDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<PagedResultDto<UtilityBillResponseDto>>> GetMyBills(
		[FromQuery] GetUtilityBillsQueryDto query,
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
			var result = await _utilityBillService.GetUserBillsAsync(userId.Value, query, ct);
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting bills list: UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 调试端点：获取OCR文本和日期解析信息（临时，用于调试）
	/// </summary>
	[HttpGet("{id}/debug")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> GetBillDebugInfo(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		try
		{
			var bill = await _utilityBillService.GetBillByIdAsync(id, userId.Value, ct);
			if (bill == null)
			{
				return NotFound(new { error = "Bill not found or access denied" });
			}

			return Ok(new
			{
				billId = bill.Id,
				billPeriodStart = bill.BillPeriodStart,
				billPeriodEnd = bill.BillPeriodEnd,
				startYear = bill.BillPeriodStart.Year,
				endYear = bill.BillPeriodEnd.Year,
				ocrRawText = bill.OcrRawText,
				ocrTextLength = bill.OcrRawText?.Length ?? 0,
				ocrConfidence = bill.OcrConfidence
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting bill debug info: UserId={UserId}, BillId={BillId}", userId, id);
			return StatusCode(500, new { error = "Internal server error" });
		}
	}

	[HttpGet("{id}")]
	[ProducesResponseType(typeof(UtilityBillResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<UtilityBillResponseDto>> GetById(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		try
		{
			var result = await _utilityBillService.GetBillByIdAsync(id, userId.Value, ct);
			if (result == null)
			{
				return NotFound(new { error = "Bill not found or access denied" });
			}

			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting bill details: UserId={UserId}, BillId={BillId}", userId, id);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 删除账单
	/// </summary>
	/// <remarks>
	/// 删除指定ID的账单。只能删除当前用户自己的记录。
	/// 
	/// **注意：**
	/// - 删除操作不可恢复
	/// - 只能删除自己的记录
	/// 
	/// **示例请求：**
	/// ```
	/// DELETE /api/utility-bills/1
	/// ```
	/// </remarks>
	/// <param name="id">账单ID</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>删除结果</returns>
	/// <response code="200">删除成功</response>
	/// <response code="401">未授权，需要登录</response>
	/// <response code="404">账单不存在或不属于当前用户</response>
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
			var success = await _utilityBillService.DeleteBillAsync(id, userId.Value, ct);
			if (!success)
			{
				return NotFound(new { error = "Bill not found or access denied" });
			}

			return Ok(new { message = "Deleted successfully" });
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting bill: UserId={UserId}, BillId={BillId}", userId, id);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}

	/// <summary>
	/// 获取当前用户的账单统计信息
	/// </summary>
	/// <remarks>
	/// 获取当前登录用户的账单统计信息，包括总记录数、总用量、总碳排放量，以及按账单类型的统计。
	/// 
	/// **查询参数：**
	/// - `startDate`（可选）：开始日期（格式：yyyy-MM-dd），基于账单周期结束日期筛选
	/// - `endDate`（可选）：结束日期（格式：yyyy-MM-dd），基于账单周期开始日期筛选
	/// 
	/// **返回数据：**
	/// - 总体统计：总记录数、总用电量、总用水量、总用气量、总碳排放
	/// - 按账单类型统计：每种账单类型的记录数、用量、碳排放
	/// 
	/// **示例请求：**
	/// ```
	/// GET /api/utility-bills/statistics
	/// GET /api/utility-bills/statistics?startDate=2024-01-01&amp;endDate=2024-01-31
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
	[ProducesResponseType(typeof(UtilityBillStatisticsDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<UtilityBillStatisticsDto>> GetStatistics(
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
			var result = await _utilityBillService.GetUserStatisticsAsync(userId.Value, startDate, endDate, ct);
			return Ok(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting statistics: UserId={UserId}", userId);
			return StatusCode(500, new { error = "Internal server error, please try again later" });
		}
	}
}
