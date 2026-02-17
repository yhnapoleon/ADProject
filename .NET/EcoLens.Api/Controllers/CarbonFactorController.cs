using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api")]
public class CarbonFactorController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public CarbonFactorController(ApplicationDbContext db)
	{
		_db = db;
	}

	public class CarbonFactorDto
	{
		public int Id { get; set; }
		public string LabelName { get; set; } = string.Empty;
		public CarbonCategory Category { get; set; }
		public decimal Co2Factor { get; set; }
		public string Unit { get; set; } = string.Empty;
	}

	public class UpsertCarbonFactorDto
	{
		public int? Id { get; set; }
		public string LabelName { get; set; } = string.Empty;
		public CarbonCategory Category { get; set; }
		public decimal Co2Factor { get; set; }
		public string Unit { get; set; } = string.Empty;
	}

	/// <summary>
	/// 获取碳排放因子（支持按 Category 过滤）。
	/// </summary>
	[HttpGet("carbon/factors")]
	public async Task<ActionResult<IEnumerable<CarbonFactorDto>>> GetFactors([FromQuery] string? category, CancellationToken ct)
	{
		IQueryable<CarbonReference> query = _db.CarbonReferences.AsQueryable();

		if (!string.IsNullOrWhiteSpace(category))
		{
			if (!Enum.TryParse<CarbonCategory>(category, true, out var cat))
			{
				return BadRequest("Invalid category value.");
			}
			query = query.Where(c => c.Category == cat);
		}

		var items = await query
			.OrderBy(c => c.LabelName)
			.Select(c => new CarbonFactorDto
			{
				Id = c.Id,
				LabelName = c.LabelName,
				Category = c.Category,
				Co2Factor = c.Co2Factor,
				Unit = c.Unit
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	/// <summary>
	/// 新增或更新碳排放因子（仅登录用户，暂不限制角色）。
	/// </summary>
	[HttpPost("admin/factor")]
	[Authorize]
	public async Task<ActionResult<CarbonFactorDto>> UpsertFactor([FromBody] UpsertCarbonFactorDto dto, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(dto.LabelName) || string.IsNullOrWhiteSpace(dto.Unit))
		{
			return BadRequest("LabelName and Unit are required.");
		}

		CarbonReference entity;
		if (dto.Id is { } id && id > 0)
		{
			entity = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.Id == id, ct)
				?? throw new KeyNotFoundException("Carbon factor not found.");

			entity.LabelName = dto.LabelName;
			entity.Category = dto.Category;
			entity.Co2Factor = dto.Co2Factor;
			entity.Unit = dto.Unit;
		}
		else
		{
			entity = new CarbonReference
			{
				LabelName = dto.LabelName,
				Category = dto.Category,
				Co2Factor = dto.Co2Factor,
				Unit = dto.Unit
			};
			await _db.CarbonReferences.AddAsync(entity, ct);
		}

		await _db.SaveChangesAsync(ct);

		var result = new CarbonFactorDto
		{
			Id = entity.Id,
			LabelName = entity.LabelName,
			Category = entity.Category,
			Co2Factor = entity.Co2Factor,
			Unit = entity.Unit
		};

		return Ok(result);
	}

	/// <summary>
	/// 查询或合并查找碳排放因子（优先使用静态数据，再回退数据库）。
	/// </summary>
	[HttpGet("carbon/lookup")]
	public async Task<ActionResult<IEnumerable<CarbonFactorDto>>> LookupFactors([FromQuery] string? label, [FromQuery] string? category, CancellationToken ct)
	{
		// Scenario A: 指定 label（Lookup 模式）
		if (!string.IsNullOrWhiteSpace(label))
		{
			var input = label.Trim();

			// 1) 优先从静态数据匹配（大小写不敏感）
			var staticKey = CarbonEmissionData.Factors.Keys
				.FirstOrDefault(k => k.Equals(input, System.StringComparison.OrdinalIgnoreCase));
			if (staticKey != null)
			{
				var factor = CarbonEmissionData.Factors[staticKey];
				var dto = new CarbonFactorDto
				{
					Id = 0,
					LabelName = staticKey,
					Category = CarbonCategory.Food,
					Co2Factor = (decimal)factor,
					Unit = "kg CO2e/serving"
				};
				return Ok(new[] { dto });
			}

			// 2) 数据库回退（大小写不敏感匹配）
			var entity = await _db.CarbonReferences
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.LabelName.ToLower() == input.ToLower(), ct);

			if (entity != null)
			{
				return Ok(new[]
				{
					new CarbonFactorDto
					{
						Id = entity.Id,
						LabelName = entity.LabelName,
						Category = entity.Category,
						Co2Factor = entity.Co2Factor,
						Unit = entity.Unit
					}
				});
			}

			// 3) 双重未命中时的默认回退
			var fallback = new CarbonFactorDto
			{
				Id = 0,
				LabelName = input,
				Category = CarbonCategory.Food,
				Co2Factor = 1.0m,
				Unit = "kg CO2e/serving"
			};
			return Ok(new[] { fallback });
		}

		// Scenario B: 未指定 label（合并列表模式）
		var dbList = await _db.CarbonReferences
			.AsNoTracking()
			.ToListAsync(ct);

		// 合并并去重：静态数据优先
		var combined = new Dictionary<string, CarbonFactorDto>(System.StringComparer.OrdinalIgnoreCase);

		// 1) 先加入静态数据（优先级高）
		foreach (var kv in CarbonEmissionData.Factors)
		{
			combined[kv.Key] = new CarbonFactorDto
			{
				Id = 0,
				LabelName = kv.Key,
				Category = CarbonCategory.Food,
				Co2Factor = (decimal)kv.Value,
				Unit = "kg CO2e/serving"
			};
		}

		// 2) 再加入数据库的数据（若标签已存在则跳过）
		foreach (var c in dbList)
		{
			if (!combined.ContainsKey(c.LabelName))
			{
				combined[c.LabelName] = new CarbonFactorDto
				{
					Id = c.Id,
					LabelName = c.LabelName,
					Category = c.Category,
					Co2Factor = c.Co2Factor,
					Unit = c.Unit
				};
			}
		}

		IEnumerable<CarbonFactorDto> resultList = combined.Values;

		// 可选分类过滤
		if (!string.IsNullOrWhiteSpace(category))
		{
			if (!Enum.TryParse<CarbonCategory>(category, true, out var cat))
			{
				return BadRequest("Invalid category value.");
			}

			resultList = resultList.Where(x => x.Category == cat);
		}

		// 按名称排序以保证稳定输出
		return Ok(resultList.OrderBy(x => x.LabelName).ToList());
	}
}





