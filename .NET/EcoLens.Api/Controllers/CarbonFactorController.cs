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
}





