using System.Globalization;
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

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoodController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IVisionService _visionService;

	public FoodController(ApplicationDbContext db, IVisionService visionService)
	{
		_db = db;
		_visionService = visionService;
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

		var food = await FindFoodCarbonReferenceAsync(name, ct);
		if (food is null)
		{
			return NotFound($"未找到食物：{name} 的碳因子。");
		}

		var inputUnit = NormalizeUnit(request.Unit);
		var factorUnit = NormalizeUnit(food.Unit);

		var normalizedQuantity = ConvertToFactorUnit(name, request.Quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0)
		{
			return BadRequest("份量换算失败，请检查单位与数值。");
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
	/// 图片识别获取食物名称。
	/// </summary>
	[HttpPost("recognize")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<FoodRecognizeResponseDto>> Recognize([FromForm] IFormFile image, CancellationToken ct)
	{
		if (image == null || image.Length == 0)
		{
			return BadRequest("未上传图片。");
		}

		var vision = await _visionService.PredictAsync(image, ct);

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
	public async Task<ActionResult<FoodCalculationResultDto>> CalculateFromImage([FromForm] IFormFile image, [FromForm] double quantity, [FromForm] string unit, CancellationToken ct)
	{
		if (image == null || image.Length == 0)
		{
			return BadRequest("未上传图片。");
		}
		if (quantity < 0)
		{
			return BadRequest("Quantity 必须为非负数。");
		}
		unit ??= "g";

		var vision = await _visionService.PredictAsync(image, ct);
		var food = await FindFoodCarbonReferenceAsync(vision.Label, ct);
		if (food is null)
		{
			return NotFound($"未找到食物：{vision.Label} 的碳因子。");
		}

		var inputUnit = NormalizeUnit(unit);
		var factorUnit = NormalizeUnit(food.Unit);
		var normalizedQuantity = ConvertToFactorUnit(vision.Label, quantity, inputUnit, factorUnit);
		if (normalizedQuantity < 0)
		{
			return BadRequest("份量换算失败，请检查单位与数值。");
		}

		var total = (decimal)normalizedQuantity * food.Co2Factor;

		var result = new FoodCalculationResultDto
		{
			FoodName = food.LabelName,
			Co2Factor = food.Co2Factor,
			FactorUnit = food.Unit,
			Quantity = quantity,
			QuantityUnit = unit,
			NormalizedQuantityInFactorUnit = Math.Round(normalizedQuantity, 6),
			TotalEmission = Math.Round(total, 6)
		};
		return Ok(result);
	}

	private async Task<CarbonReference?> FindFoodCarbonReferenceAsync(string foodName, CancellationToken ct)
	{
		// 优先精确匹配（不区分大小写），其次包含匹配
		var query = _db.CarbonReferences
			.AsNoTracking()
			.Where(c => c.Category == CarbonCategory.Food);

		var exact = await query.FirstOrDefaultAsync(c => c.LabelName.Equals(foodName, StringComparison.OrdinalIgnoreCase), ct);
		if (exact != null) return exact;

		return await query.OrderBy(c => c.LabelName.Length)
			.FirstOrDefaultAsync(c => c.LabelName.ToLower().Contains(foodName.ToLower()), ct);
	}

	private static string NormalizeUnit(string unit)
	{
		var u = (unit ?? string.Empty).Trim().ToLowerInvariant();
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


