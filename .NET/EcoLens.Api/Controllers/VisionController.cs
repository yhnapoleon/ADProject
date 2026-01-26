using EcoLens.Api.DTOs;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisionController : ControllerBase
{
	private readonly IVisionService _visionService;

	public VisionController(IVisionService visionService)
	{
		_visionService = visionService;
	}

	/// <summary>
	/// 调用 Python FastAPI 的图像识别服务，返回融合后的预测结果。
	/// </summary>
	[HttpPost("analyze")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<VisionPredictionResponseDto>> Analyze([FromForm] IFormFile image, CancellationToken ct)
	{
		if (image == null || image.Length == 0)
		{
			return BadRequest("No image uploaded.");
		}

		try
		{
			var result = await _visionService.PredictAsync(image, ct);
			return Ok(result);
		}
		catch (ArgumentException ex)
		{
			return BadRequest(ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
		}
		catch (HttpRequestException ex)
		{
			return StatusCode(StatusCodes.Status502BadGateway, $"Vision service error: {ex.Message}");
		}
		catch (Exception ex)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, $"Unexpected error: {ex.Message}");
		}
	}

	/// <summary>
	/// 路由别名：/api/vision/recognize（与前端文档对齐）。
	/// </summary>
	[HttpPost("/api/vision/recognize")]
	[Consumes("multipart/form-data")]
	public Task<ActionResult<VisionPredictionResponseDto>> Recognize([FromForm] IFormFile image, CancellationToken ct)
		=> Analyze(image, ct);

	/// <summary>
	/// 路由别名：/api/vision/meal-detect（与前端文档对齐）。
	/// </summary>
	[HttpPost("/api/vision/meal-detect")]
	[Consumes("multipart/form-data")]
	public Task<ActionResult<VisionPredictionResponseDto>> MealDetect([FromForm] IFormFile image, CancellationToken ct)
		=> Analyze(image, ct);
}

