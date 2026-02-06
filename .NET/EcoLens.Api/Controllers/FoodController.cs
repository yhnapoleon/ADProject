using System.Globalization;
using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoodController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IVisionService _visionService;
	private readonly IPointService _pointService;
	private readonly ILogger<FoodController> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	public FoodController(ApplicationDbContext db, IVisionService visionService, IPointService pointService, ILogger<FoodController> logger, IHttpClientFactory httpClientFactory)
	{
		_db = db;
		_visionService = visionService;
		_pointService = pointService;
		_logger = logger;
		_httpClientFactory = httpClientFactory;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 通过食物名称与份量计算总碳排放。
	/// - 支持单位：g, kg, portion(份/份量/serving)
	/// - 根据 CarbonReference(Category=Food) 的 Unit 做换算
	/// </summary>
	[HttpPost("calculate")]
	public async Task<ActionResult<FoodCalculationResultDto>> CalculateByName([FromBody] FoodCalculateByNameRequest request, CancellationToken ct)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}

		var name = (request.FoodName ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			return BadRequest("FoodName is required.");
		}

		var food = await FindFoodCarbonFactorAsync(name, ct);
		if (food is null)
		{
			return NotFound($"Carbon factor not found for food: {name}.");
		}

		var inputUnit = NormalizeUnit(request.Unit);
		var factorUnit = NormalizeUnit(food.Unit);

		var normalizedQuantity = ConvertToFactorUnit(name, request.Quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0)
		{
			return BadRequest("Quantity conversion failed. Please check unit and value.");
		}

		var total = (decimal)normalizedQuantity * food.Co2Factor;

		var result = new FoodCalculationResultDto
		{
			FoodName = food.LabelName,
			Co2Factor = food.Co2Factor,
			FactorUnit = food.Unit,
			Quantity = request.Quantity,
			QuantityUnit = request.Unit,
			NormalizedQuantityInFactorUnit = Math.Round(normalizedQuantity, 6),
			TotalEmission = Math.Round(total, 6)
		};

		return Ok(result);
	}

	/// <summary>
	/// 集成式流程：上传图片 + 分量，后端识别名称、计算总排放并入库，返回最终结果。
	/// - multipart/form-data: image, quantity, unit, note?
	/// - 返回：name, amount(kg), emissionFactor, emission, createdAt
	/// </summary>
	[HttpPost("ingest-from-image")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<FoodSimpleCalcResponse>> IngestFromImage([FromForm] FoodIngestFromImageRequest req, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		if (req.File == null || req.File.Length == 0) return BadRequest("Have not uploaded image.");
		if (req.Quantity < 0) return BadRequest("Quantity must be non-negative.");

		var vision = await _visionService.PredictAsync(req.File, ct);
		var food = await FindFoodCarbonFactorAsync(vision.Label, ct);
		if (food is null) return NotFound($"Carbon factor not found for food: {vision.Label}.");

		// 计算总排放
		var inputUnit = NormalizeUnit(req.Unit);
		var factorUnit = NormalizeUnit(food.Unit);
		var normalizedQuantity = ConvertToFactorUnit(vision.Label, req.Quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0) return BadRequest("Quantity conversion failed. Please check unit and value.");
		var total = (decimal)normalizedQuantity * food.Co2Factor;

		// 入库（amount 统一保存为 kg）
		var amountKg = ToKilograms(vision.Label, req.Quantity, inputUnit);
		if (amountKg < 0) return BadRequest("Quantity conversion to kg failed.");

		var record = new FoodRecord
		{
			UserId = userId.Value,
			Name = food.LabelName,
			Amount = amountKg,
			EmissionFactor = food.Co2Factor,
			Emission = Math.Round(total, 6),
			Note = req.Note
		};
		await _db.FoodRecords.AddAsync(record, ct);
		await _db.SaveChangesAsync(ct);

		// 积分奖励检查（忽略异常，避免影响主流程）
		try
		{
			await _pointService.CheckAndAwardPointsAsync(userId.Value, record.CreatedAt.Date);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "CheckAndAwardPoints failed after food ingest-from-image for UserId={UserId}", userId);
		}

		return Ok(new FoodSimpleCalcResponse
		{
			Name = record.Name,
			Quantity = req.Quantity,
			EmissionFactor = record.EmissionFactor,
			Emission = record.Emission
		});
	}

	/// <summary>
	/// 集成式流程：前端直接传名称 + 分量，后端计算并入库后仅返回最终结果。
	/// - JSON: { name, quantity, unit, note? }
	/// </summary>
	[HttpPost("ingest-by-name")]
	public async Task<ActionResult<FoodSimpleCalcResponse>> IngestByName([FromBody] FoodIngestByNameRequest req, CancellationToken ct)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);

		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var food = await FindFoodCarbonFactorAsync(req.Name, ct);
		if (food is null) return NotFound($"Carbon factor not found for food: {req.Name}.");

		var inputUnit = NormalizeUnit(req.Unit);
		var factorUnit = NormalizeUnit(food.Unit);
		var normalizedQuantity = ConvertToFactorUnit(req.Name, req.Quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0) return BadRequest("Quantity conversion failed. Please check unit and value.");
		var total = (decimal)normalizedQuantity * food.Co2Factor;

		var amountKg = ToKilograms(req.Name, req.Quantity, inputUnit);
		if (amountKg < 0) return BadRequest("Quantity conversion to kg failed.");

		var record = new FoodRecord
		{
			UserId = userId.Value,
			Name = food.LabelName,
			Amount = amountKg,
			EmissionFactor = food.Co2Factor,
			Emission = Math.Round(total, 6),
			Note = req.Note
		};
		await _db.FoodRecords.AddAsync(record, ct);
		await _db.SaveChangesAsync(ct);

		// 积分奖励检查（忽略异常，避免影响主流程）
		try
		{
			await _pointService.CheckAndAwardPointsAsync(userId.Value, record.CreatedAt.Date);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "CheckAndAwardPoints failed after food ingest-by-name for UserId={UserId}", userId);
		}

		return Ok(new FoodSimpleCalcResponse
		{
			Name = record.Name,
			Quantity = req.Quantity,
			EmissionFactor = record.EmissionFactor,
			Emission = record.Emission
		});
	}

	/// <summary>
	/// 前端传名称+分量，后端只计算（不入库），返回最小结果：name, quantity, emissionFactor, emission。
	/// </summary>
	[HttpPost("calculate-simple")]
	public async Task<ActionResult<FoodSimpleCalcResponse>> CalculateSimple([FromBody] FoodCalculateSimpleRequest req, CancellationToken ct)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);

		var food = await FindFoodCarbonFactorAsync(req.Name, ct);
		if (food is null) return NotFound($"Carbon factor not found for food: {req.Name}.");

		var inputUnit = NormalizeUnit(req.Unit);
		var factorUnit = NormalizeUnit(food.Unit);
		var normalizedQuantity = ConvertToFactorUnit(req.Name, req.Quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0) return BadRequest("Quantity conversion failed. Please check unit and value.");

		var total = (decimal)normalizedQuantity * food.Co2Factor;

		return Ok(new FoodSimpleCalcResponse
		{
			Name = food.LabelName,
			Quantity = req.Quantity,
			EmissionFactor = food.Co2Factor,
			Emission = Math.Round(total, 6)
		});
	}

	/// <summary>
	/// 图片识别获取食物名称。
	/// </summary>
	[HttpPost("recognize")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<FoodRecognizeResponseDto>> Recognize([FromForm] FoodImageUploadDto dto, CancellationToken ct)
	{
		if (dto.File == null || dto.File.Length == 0)
		{
			return BadRequest("No image uploaded.");
		}

		var vision = await _visionService.PredictAsync(dto.File, ct);

		return Ok(new FoodRecognizeResponseDto
		{
			FoodName = vision.Label,
			Confidence = vision.Confidence,
			SourceModel = vision.SourceModel
		});
	}

	/// <summary>
	/// 组合流程：上传图片 + 份量，后端识别名称并计算总排放。
	/// - 前端可直接调用本接口完成识别+计算的一站式处理
	/// - 支持单位：g, kg, portion(份/份量/serving)
	/// </summary>
	[HttpPost("calculate-from-image")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<FoodCalculationResultDto>> CalculateFromImage([FromForm] FoodCalculateFromImageRequest req, CancellationToken ct)
	{
		if (req.File == null || req.File.Length == 0)
		{
			return BadRequest("No image uploaded.");
		}
		if (req.Quantity < 0)
		{
			return BadRequest("Quantity must be non-negative.");
		}
		req.Unit ??= "g";

		var vision = await _visionService.PredictAsync(req.File, ct);
		var food = await FindFoodCarbonFactorAsync(vision.Label, ct);
		if (food is null)
		{
			return NotFound($"Carbon factor not found for food: {vision.Label}.");
		}

		var inputUnit = NormalizeUnit(req.Unit);
		var factorUnit = NormalizeUnit(food.Unit);
		var normalizedQuantity = ConvertToFactorUnit(vision.Label, req.Quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0)
		{
			return BadRequest("Quantity conversion failed. Please check unit and value.");
		}

		var total = (decimal)normalizedQuantity * food.Co2Factor;

		var result = new FoodCalculationResultDto
		{
			FoodName = food.LabelName,
			Co2Factor = food.Co2Factor,
			FactorUnit = food.Unit,
			Quantity = req.Quantity,
			QuantityUnit = req.Unit,
			NormalizedQuantityInFactorUnit = Math.Round(normalizedQuantity, 6),
			TotalEmission = Math.Round(total, 6)
		};
		return Ok(result);
	}

	private class CarbonFactorSimple
	{
		public string LabelName { get; set; } = string.Empty;
		public decimal Co2Factor { get; set; }
		public string Unit { get; set; } = string.Empty;
	}

	private class LookupDto
	{
		public int Id { get; set; }
		public string LabelName { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal Co2Factor { get; set; }
	}

	private async Task<CarbonFactorSimple?> FindFoodCarbonFactorAsync(string foodName, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(foodName)) return null;

		// 使用统一 API 进行查找（静态数据优先）
		var baseUri = $"{Request.Scheme}://{Request.Host.Value}";
		var url = $"{baseUri}/api/carbon/lookup?label={Uri.EscapeDataString(foodName)}";
		var client = _httpClientFactory.CreateClient();
		using var resp = await client.GetAsync(url, ct);
		if (!resp.IsSuccessStatusCode) return null;

		await using var stream = await resp.Content.ReadAsStreamAsync(ct);
		var items = await JsonSerializer.DeserializeAsync<List<LookupDto>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
		var item = items?.FirstOrDefault();
		if (item == null) return null;

		return new CarbonFactorSimple
		{
			LabelName = item.LabelName,
			Co2Factor = item.Co2Factor,
			Unit = item.Unit
		};
	}

	private static string NormalizeUnit(string unit)
	{
		var u = (unit ?? string.Empty).Trim().ToLowerInvariant();
		// 将带有 serving 的单位视为“份量”（portion）
		if (u.Contains("serving"))
		{
			return "portion";
		}
		return u switch
		{
			"gram" or "grams" or "克" => "g",
			"kilogram" or "kilograms" or "千克" or "公斤" => "kg",
			"portion" or "serving" or "份" or "份量" => "portion",
			_ => u // g / kg / portion / 其他原样
		};
	}

	/// <summary>
	/// 将输入份量换算为碳因子单位的数值。
	/// 规则：
	/// - g 与 kg 互转
	/// - portion(份/份量/serving) 将按固定映射转换为克（每种食物一一对应），再换算为目标单位
	/// - 若无法识别单位则返回 -1
	/// </summary>
	private static double ConvertToFactorUnit(string foodName, double quantity, string inputUnit, string factorUnit)
	{
		if (quantity < 0) return -1;

		// 1) 将输入数量转换到“克(g)”
		double grams = inputUnit switch
		{
			"g" => quantity,
			"kg" => quantity * 1000.0,
			"portion" => MapPortionToGrams(foodName, quantity),
			_ => -1
		};
		if (grams < 0) return -1;

		// 2) 将克转换为目标单位
		return factorUnit switch
		{
			"g" => grams,
			"kg" => grams / 1000.0,
			_ => -1
		};
	}

	/// <summary>
	/// 将输入数量转换为 kg（入库统一单位）。
	/// </summary>
	private static double ToKilograms(string foodName, double quantity, string inputUnit)
	{
		double grams = inputUnit switch
		{
			"g" => quantity,
			"kg" => quantity * 1000.0,
			"portion" => MapPortionToGrams(foodName, quantity),
			_ => -1
		};
		if (grams < 0) return -1;
		return grams / 1000.0;
	}

	/// <summary>
	/// 简单的“份量 -> 克数”映射（可按需扩展）。
	/// 未命中则使用通用默认值 100g/份。
	/// </summary>
	private static double MapPortionToGrams(string foodName, double portions)
	{
		if (portions < 0) return -1;

		// 规范化食物名称进行字典匹配
		var key = (foodName ?? string.Empty).Trim().ToLowerInvariant();

		// 可扩展的固定映射（示例）
		// 例如：一个苹果 ~182g；香蕉 ~118g；米饭一碗 ~150g；牛排一份 ~200g
		var map = new Dictionary<string, double>
		{
			{ "apple", 182 },
			{ "banana", 118 },
			{ "rice", 150 },
			{ "steak", 200 },
			{ "鸡蛋", 50 },
			{ "米饭", 150 },
			{ "牛排", 200 },
			{ "苹果", 182 },
			{ "香蕉", 118 }
		};

		double gramsPerPortion = map.TryGetValue(key, out var g) ? g : 100.0;
		return gramsPerPortion * portions;
	}
}


