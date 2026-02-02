using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class SimpleFoodController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IHttpClientFactory _httpClientFactory;

	public SimpleFoodController(ApplicationDbContext db, IHttpClientFactory httpClientFactory)
	{
		_db = db;
		_httpClientFactory = httpClientFactory;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 计算食物排放（不落库）。前端 amount 为克（g），后端 /1000 转为 kg 再计算。
	/// - 入参：name, amount（g）
	/// - 出参：name, amount（kg）, emission_factor, emission
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

		// 前端 amount 为克，/1000 转为 kg 再按 kgCO2/kg 计算
		var amountKg = req.Amount / 1000.0;
		var emissionFactor = food.Co2Factor;
		var emission = (decimal)amountKg * emissionFactor;

		return Ok(new FoodCalcResponse
		{
			Name = food.LabelName,
			Amount = amountKg,
			EmissionFactor = decimal.Round(emissionFactor, 6),
			Emission = decimal.Round(emission, 6)
		});
	}

	/// <summary>
	/// 更新食物（计算逻辑与 calculateFood 一致，不落库）。amount 为克（g）。
	/// </summary>
	[HttpPost("updateFood")]
	public Task<ActionResult<FoodCalcResponse>> UpdateFood([FromBody] CalculateFoodRequest req, CancellationToken ct)
		=> CalculateFood(req, ct);

	/// <summary>
	/// 添加食物记录（落库）。前端 amount 统一为克（g），后端 /1000 转为 kg 再计算并存储。
	/// - 入参：name, amount（g）, emission_factor, emission, note
	/// - 出参：是否成功
	/// </summary>
	[HttpPost("addFood")]
	public async Task<ActionResult<AddFoodResponse>> AddFood([FromBody] AddFoodRequest req, CancellationToken ct)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);

		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized();
		}

		// 可选校验：校验 name 是否在 CarbonReferences 中存在
		var exists = await _db.CarbonReferences.AsNoTracking()
			.AnyAsync(c => c.Category == CarbonCategory.Food && c.LabelName == req.Name, ct);

		// 前端 amount 统一为克（g），/1000 转为 kg 再计算并存储
		var amountKg = req.Amount / 1000.0;
		var emission = (decimal)amountKg * req.EmissionFactor;

		// 允许前端传自定义项；若不存在，依然保存
		var record = new FoodRecord
		{
			UserId = userId.Value,
			Name = req.Name,
			Amount = amountKg,
			EmissionFactor = req.EmissionFactor,
			Emission = emission,
			Note = req.Note
		};

		try
		{
			await _db.FoodRecords.AddAsync(record, ct);
			await _db.SaveChangesAsync(ct);
		}
		catch (Exception ex)
		{
			var sqlEx = ex.InnerException as SqlException ?? ex as SqlException;
			if (sqlEx?.Number == 208 && sqlEx.Message.Contains("FoodRecords", StringComparison.OrdinalIgnoreCase))
			{
				// Invalid object name 'FoodRecords'：表不存在，自动创建后重试
				await EnsureFoodRecordsTableAsync(ct);
				// 清除 ChangeTracker 中失败的实体
				foreach (var entry in _db.ChangeTracker.Entries<FoodRecord>().ToList())
					entry.State = EntityState.Detached;
				record = new FoodRecord
				{
					UserId = userId.Value,
					Name = req.Name,
					Amount = amountKg,
					EmissionFactor = req.EmissionFactor,
					Emission = emission,
					Note = req.Note
				};
				await _db.FoodRecords.AddAsync(record, ct);
				await _db.SaveChangesAsync(ct);
			}
			else
				throw;
		}

		return Ok(new AddFoodResponse { Success = true });
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

	private async Task EnsureFoodRecordsTableAsync(CancellationToken ct)
	{
		await _db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'FoodRecords')
BEGIN
    CREATE TABLE [dbo].[FoodRecords] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Amount] float NOT NULL,
        [EmissionFactor] decimal(18,4) NOT NULL,
        [Emission] decimal(18,4) NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_FoodRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FoodRecords_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FoodRecords_UserId] ON [dbo].[FoodRecords] ([UserId]);
END
", ct);
	}

	private async Task<CarbonFactorSimple?> FindFoodByNameAsync(string name, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(name)) return null;

		// 调用统一查找 API：/api/carbon/lookup?label=...
		var baseUri = $"{Request.Scheme}://{Request.Host.Value}";
		var url = $"{baseUri}/api/carbon/lookup?label={Uri.EscapeDataString(name)}";
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
}


