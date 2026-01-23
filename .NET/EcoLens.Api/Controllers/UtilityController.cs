using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using EcoLens.Api.Services;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UtilityController : ControllerBase
{
	private readonly IAiService _aiService;

	public UtilityController(IAiService aiService)
	{
		_aiService = aiService;
	}

	/// <summary>
	/// 账单 OCR（Mock）：接收图片并返回模拟的水电气用量。
	/// </summary>
	[HttpPost("ocr")]
	[Consumes("multipart/form-data")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status502BadGateway)]
	public async Task<ActionResult<object>> Ocr([FromForm] IFormFile billImage, CancellationToken ct)
	{
		if (billImage == null || billImage.Length == 0)
		{
			return BadRequest("No bill image uploaded.");
		}

		string prompt =
			"You are a utility bill analyzer. Extract the monthly usage values for Electricity (kWh), Water (Cu M or m3), and Gas. " +
			"The bill is likely from Singapore providers (e.g., SP Group). Look for labels such as 'Electricity Usage', 'Water Consumption', 'Gas Usage', " +
			"and units 'kWh', 'm3', or 'Cu M'. If multiple periods exist, choose the latest monthly total. " +
			"Return ONLY a valid JSON object with keys: 'electricityUsage', 'waterUsage', 'gasUsage'. " +
			"Values should be numbers. If a field is not found, use 0. Do not wrap the JSON in code fences.";

		string aiText;
		try
		{
			aiText = await _aiService.AnalyzeImageAsync(prompt, billImage);
		}
		catch (Exception ex)
		{
			return StatusCode(StatusCodes.Status502BadGateway, $"AI 调用失败: {ex.Message}");
		}

		if (string.IsNullOrWhiteSpace(aiText))
		{
			return StatusCode(StatusCodes.Status502BadGateway, "AI 返回空结果。");
		}

		// 容错：剔除可能的 ```json/``` 包裹，并提取花括号中的 JSON 片段
		var sanitized = aiText.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
			.Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
			.Trim();

		int start = sanitized.IndexOf('{');
		int end = sanitized.LastIndexOf('}');
		if (start >= 0 && end > start)
		{
			sanitized = sanitized.Substring(start, end - start + 1);
		}

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		try
		{
			var dto = JsonSerializer.Deserialize<UsageDto>(sanitized, options) ?? new UsageDto();

			// 归一化数值（防止 NaN/Infinity）
			decimal e = Normalize(dto.ElectricityUsage);
			decimal w = Normalize(dto.WaterUsage);
			decimal g = Normalize(dto.GasUsage);

			return Ok(new
			{
				electricityUsage = e,
				waterUsage = w,
				gasUsage = g
			});
		}
		catch (JsonException)
		{
			return StatusCode(StatusCodes.Status502BadGateway, "AI 返回非 JSON 格式或解析失败。");
		}

		static decimal Normalize(decimal value)
		{
			if (decimal.IsNaN(value) || decimal.IsInfinity(value))
			{
				return 0m;
			}
			return value < 0 ? 0m : value;
		}
	}

	private sealed class UsageDto
	{
		public decimal ElectricityUsage { get; set; }
		public decimal WaterUsage { get; set; }
		public decimal GasUsage { get; set; }
	}
}


