using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class SimpleFoodController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public SimpleFoodController(ApplicationDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// 计算食物排放（不落库）。
	/// - 入参：name, amount（kg）
	/// - 出参：name, amount, emission_factor, emission
	/// </summary>
	[HttpPost("calculateFood")]
	public async Task<ActionResult<FoodCalcResponse>> CalculateFood([FromBody] CalculateFoodRequest req, CancellationToken ct)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);

		var food = await FindFoodByNameAsync(req.Name, ct);
		if (food is null)
		{
			return NotFound($"未找到食物：{req.Name} 的排放因子。");
		}

		// 假设 Food 的因子为按 kg 计（kgCO2/kg），amount 单位为 kg
		var emissionFactor = food.Co2Factor;
		var emission = (decimal)req.Amount * emissionFactor;

		return Ok(new FoodCalcResponse
		{
			Name = food.LabelName,
			Amount = req.Amount,
			EmissionFactor = decimal.Round(emissionFactor, 6),
			Emission = decimal.Round(emission, 6)
		});
	}

	/// <summary>
	/// 更新食物（计算逻辑与 calculateFood 一致，不落库）。
	/// - 入参：name, amount（kg）
	/// - 出参：name, amount, emission_factor, emission
	/// </summary>
	[HttpPost("updateFood")]
	public Task<ActionResult<FoodCalcResponse>> UpdateFood([FromBody] CalculateFoodRequest req, CancellationToken ct)
		=> CalculateFood(req, ct);

	/// <summary>
	/// 添加食物记录（落库）。
	/// - 入参：name, amount（kg）, emission_factor, emission, note
	/// - 出参：是否成功
	/// </summary>
	[HttpPost("addFood")]
	public async Task<ActionResult<AddFoodResponse>> AddFood([FromBody] AddFoodRequest req, CancellationToken ct)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);

		// 可选校验：校验 name 是否在 CarbonReferences 中存在
		var exists = await _db.CarbonReferences.AsNoTracking()
			.AnyAsync(c => c.Category == CarbonCategory.Food && c.LabelName == req.Name, ct);

		// 允许前端传自定义项；若不存在，依然保存
		var record = new FoodRecord
		{
			Name = req.Name,
			Amount = req.Amount,
			EmissionFactor = req.EmissionFactor,
			Emission = req.Emission,
			Note = req.Note
		};

		await _db.FoodRecords.AddAsync(record, ct);
		await _db.SaveChangesAsync(ct);

		return Ok(new AddFoodResponse { Success = true });
	}

	private async Task<Models.CarbonReference?> FindFoodByNameAsync(string name, CancellationToken ct)
	{
		var query = _db.CarbonReferences.AsNoTracking().Where(c => c.Category == CarbonCategory.Food);
		var exact = await query.FirstOrDefaultAsync(c => c.LabelName.Equals(name, StringComparison.OrdinalIgnoreCase), ct);
		if (exact != null) return exact;

		return await query.OrderBy(c => c.LabelName.Length)
			.FirstOrDefaultAsync(c => c.LabelName.ToLower().Contains(name.ToLower()), ct);
	}
}


