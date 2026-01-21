using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisionController : ControllerBase
{
	/// <summary>
	/// 模拟分析图片并返回识别标签。
	/// TODO: Call Python Microservice via HttpClient here
	/// </summary>
	[HttpPost("analyze")]
	public async Task<ActionResult<object>> Analyze([FromForm] IFormFile image, CancellationToken ct)
	{
		if (image == null || image.Length == 0)
		{
			return BadRequest("No image uploaded.");
		}

		// Mock: filename contains "steak" -> "Steak" else "Apple"
		var label = image.FileName.Contains("steak", StringComparison.OrdinalIgnoreCase) ? "Steak" : "Apple";
		await Task.CompletedTask;
		return Ok(new { label });
	}
}

