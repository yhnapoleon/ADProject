using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using EcoLens.Api.Services;
using EcoLens.Api.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UtilityController : ControllerBase
{
	private readonly IAiService _aiService;
	private readonly ApplicationDbContext _db;

	public UtilityController(IAiService aiService, ApplicationDbContext db)
	{
		_aiService = aiService;
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
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
			// decimal 类型不支持 NaN/Infinity，若出现异常值会在反序列化阶段抛出 JsonException。
			// 这里仅做下界裁剪，避免负数。
			return value < 0m ? 0m : value;
		}
	}

	/// <summary>
	/// 路由别名：/api/vision/utility-ocr（与前端文档对齐）。表单字段名：file
	/// </summary>
	[HttpPost("/api/vision/utility-ocr")]
	[Consumes("multipart/form-data")]
	public Task<ActionResult<object>> UtilityOcrAlias([FromForm] IFormFile file, CancellationToken ct)
		=> Ocr(file, ct);

	private sealed class UsageDto
	{
		public decimal ElectricityUsage { get; set; }
		public decimal WaterUsage { get; set; }
		public decimal GasUsage { get; set; }
	}

	#region Utility Record CRUD

	public sealed class CreateUtilitiesRecordRequest
	{
		public string? Month { get; set; } // YYYY-MM
		public decimal? ElectricityUsage { get; set; }
		public decimal? ElectricityCost { get; set; }
		public decimal? WaterUsage { get; set; }
		public decimal? WaterCost { get; set; }
		public decimal? GasUsage { get; set; }
		public decimal? GasCost { get; set; }
	}

	public sealed class UpsertUtilityRecordDto
	{
		public string YearMonth { get; set; } = string.Empty; // YYYY-MM
		public decimal ElectricityUsage { get; set; }
		public decimal ElectricityCost { get; set; }
		public decimal WaterUsage { get; set; }
		public decimal WaterCost { get; set; }
		public decimal GasUsage { get; set; }
		public decimal GasCost { get; set; }
	}

	public sealed class UtilityRecordResponseDto
	{
		public int Id { get; set; }
		public string YearMonth { get; set; } = string.Empty;
		public decimal ElectricityUsage { get; set; }
		public decimal ElectricityCost { get; set; }
		public decimal WaterUsage { get; set; }
		public decimal WaterCost { get; set; }
		public decimal GasUsage { get; set; }
		public decimal GasCost { get; set; }
		public decimal EstimatedEmission { get; set; } // 合计的估算碳排放（kg CO2e）
	}

	/// <summary>
	/// 保存一条 Utility 账单记录，并基于参考因子生成对应的 ActivityLog（每个非零用量各一条）。
	/// </summary>
	[HttpPost("record")]
	public async Task<ActionResult<UtilityRecordResponseDto>> SaveRecord([FromBody] UpsertUtilityRecordDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();
		if (string.IsNullOrWhiteSpace(dto.YearMonth) || dto.YearMonth.Length != 7)
		{
			return BadRequest("YearMonth must be formatted as YYYY-MM.");
		}

		var bill = new UtilityBill
		{
			UserId = userId.Value,
			YearMonth = dto.YearMonth,
			ElectricityUsage = dto.ElectricityUsage < 0 ? 0 : dto.ElectricityUsage,
			ElectricityCost = dto.ElectricityCost < 0 ? 0 : dto.ElectricityCost,
			WaterUsage = dto.WaterUsage < 0 ? 0 : dto.WaterUsage,
			WaterCost = dto.WaterCost < 0 ? 0 : dto.WaterCost,
			GasUsage = dto.GasUsage < 0 ? 0 : dto.GasUsage,
			GasCost = dto.GasCost < 0 ? 0 : dto.GasCost
		};

		await _db.UtilityBills.AddAsync(bill, ct);
		await _db.SaveChangesAsync(ct);

		// 生成 ActivityLog（若对应 usage > 0）
		var userRegion = await _db.ApplicationUsers
			.Where(u => u.Id == userId.Value)
			.Select(u => u.Region)
			.FirstOrDefaultAsync(ct);

		async Task<CarbonReference?> FindUtilityFactorAsync(string label)
		{
			CarbonReference? factor = null;
			if (!string.IsNullOrWhiteSpace(userRegion))
			{
				factor = await _db.CarbonReferences.FirstOrDefaultAsync(
					c => c.LabelName == label && c.Category == CarbonCategory.Utility && c.Region == userRegion, ct);
			}
			if (factor is null)
			{
				factor = await _db.CarbonReferences.FirstOrDefaultAsync(
					c => c.LabelName == label && c.Category == CarbonCategory.Utility && c.Region == null, ct);
			}
			return factor;
		}

		decimal totalEmission = 0m;
		async Task AddLogAsync(string label, decimal usage)
		{
			if (usage <= 0) return;
			var factor = await FindUtilityFactorAsync(label);
			if (factor is null) return;

			var emission = usage * factor.Co2Factor;
			totalEmission += emission;

			var log = new ActivityLog
			{
				UserId = userId.Value,
				CarbonReferenceId = factor.Id,
				Quantity = usage,
				TotalEmission = emission,
				ImageUrl = null,
				DetectedLabel = $"{label} ({bill.YearMonth})"
			};
			await _db.ActivityLogs.AddAsync(log, ct);
		}

		await AddLogAsync("Electricity", bill.ElectricityUsage ?? 0m);
		await AddLogAsync("Water", bill.WaterUsage ?? 0m);
		await AddLogAsync("Gas", bill.GasUsage ?? 0m);

		await _db.SaveChangesAsync(ct);

		var response = new UtilityRecordResponseDto
		{
			Id = bill.Id,
			YearMonth = bill.YearMonth,
			ElectricityUsage = bill.ElectricityUsage ?? 0m,
			ElectricityCost = bill.ElectricityCost,
			WaterUsage = bill.WaterUsage ?? 0m,
			WaterCost = bill.WaterCost,
			GasUsage = bill.GasUsage ?? 0m,
			GasCost = bill.GasCost,
			EstimatedEmission = totalEmission
		};

		return Ok(response);
	}

	/// <summary>
	/// 路由别名：/api/records/utilities（与前端文档对齐）。
	/// </summary>
	[HttpPost("/api/records/utilities")]
	public Task<ActionResult<UtilityRecordResponseDto>> CreateUtilitiesRecord([FromBody] CreateUtilitiesRecordRequest req, CancellationToken ct)
	{
		var dto = new UpsertUtilityRecordDto
		{
			YearMonth = string.IsNullOrWhiteSpace(req.Month) ? DateTime.UtcNow.ToString("yyyy-MM") : req.Month!,
			ElectricityUsage = req.ElectricityUsage ?? 0,
			ElectricityCost = req.ElectricityCost ?? 0,
			WaterUsage = req.WaterUsage ?? 0,
			WaterCost = req.WaterCost ?? 0,
			GasUsage = req.GasUsage ?? 0,
			GasCost = req.GasCost ?? 0
		};
		return SaveRecord(dto, ct);
	}

	/// <summary>
	/// 获取当前用户的 Utility 账单记录（按月份倒序）。
	/// </summary>
	[HttpGet("my-records")]
	public async Task<ActionResult<IEnumerable<UtilityRecordResponseDto>>> MyRecords(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		// 估算排放：按当前参考因子临时计算（无需严格一致）
		var electricity = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == "Electricity" && c.Category == CarbonCategory.Utility, ct);
		var water = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == "Water" && c.Category == CarbonCategory.Utility, ct);
		var gas = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == "Gas" && c.Category == CarbonCategory.Utility, ct);

		var items = await _db.UtilityBills
			.Where(b => b.UserId == userId.Value)
			.OrderByDescending(b => b.YearMonth)
			.Select(b => new UtilityRecordResponseDto
			{
				Id = b.Id,
				YearMonth = b.YearMonth,
					ElectricityUsage = b.ElectricityUsage ?? 0m,
				ElectricityCost = b.ElectricityCost,
					WaterUsage = b.WaterUsage ?? 0m,
				WaterCost = b.WaterCost,
					GasUsage = b.GasUsage ?? 0m,
				GasCost = b.GasCost,
				EstimatedEmission =
					((b.ElectricityUsage ?? 0m) * (electricity != null ? electricity.Co2Factor : 0m)) +
					((b.WaterUsage ?? 0m) * (water != null ? water.Co2Factor : 0m)) +
					((b.GasUsage ?? 0m) * (gas != null ? gas.Co2Factor : 0m))
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	#endregion
}


