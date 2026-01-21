using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UtilityController : ControllerBase
{
	/// <summary>
	/// 账单 OCR（Mock）：接收图片并返回模拟的水电气用量。
	/// </summary>
	[HttpPost("ocr")]
	public async Task<ActionResult<object>> Ocr([FromForm] IFormFile billImage, CancellationToken ct)
	{
		if (billImage == null || billImage.Length == 0)
		{
			return BadRequest("No bill image uploaded.");
		}

		// PB-013: 此处应接入 Azure Form Recognizer 或 Google ML Kit 进行真实的票据识别。
		// 现阶段返回模拟数据。
		await Task.CompletedTask;

		// 简单生成一组固定/随机的模拟值
		var rnd = new Random();
		var electricity = Math.Round(100 + rnd.NextDouble() * 200, 1); // 100 - 300 kWh
		var water = Math.Round(10 + rnd.NextDouble() * 30, 1);        // 10 - 40 m3
		var gas = Math.Round(rnd.NextDouble() * 5, 1);                // 0 - 5 m3

		return Ok(new
		{
			electricityUsage = electricity,
			waterUsage = water,
			gasUsage = gas
		});
	}
}


