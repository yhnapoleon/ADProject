using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Admin;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public AdminController(ApplicationDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// 获取碳排放因子列表（可按 Category/Region/Label 过滤）。
	/// </summary>
	[HttpGet("carbon-reference")]
	public async Task<ActionResult<IEnumerable<CarbonReferenceDto>>> GetCarbonReferences(
		[FromQuery] string? category,
		[FromQuery] string? region,
		[FromQuery] string? label,
		CancellationToken ct)
	{
		IQueryable<CarbonReference> query = _db.CarbonReferences.AsQueryable();

		if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<CarbonCategory>(category, true, out var cat))
		{
			query = query.Where(c => c.Category == cat);
		}

		if (!string.IsNullOrWhiteSpace(region))
		{
			query = query.Where(c => c.Region == region);
		}

		if (!string.IsNullOrWhiteSpace(label))
		{
			query = query.Where(c => c.LabelName.Contains(label));
		}

		var items = await query
			.OrderBy(c => c.LabelName)
			.Select(c => new CarbonReferenceDto
			{
				Id = c.Id,
				LabelName = c.LabelName,
				Category = c.Category,
				Co2Factor = c.Co2Factor,
				Unit = c.Unit,
				Region = c.Region
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	/// <summary>
	/// 新增或更新碳排放因子（Region 可选）。
	/// </summary>
	[HttpPost("carbon-reference")]
	public async Task<ActionResult<CarbonReferenceDto>> UpsertCarbonReference([FromBody] UpsertCarbonReferenceDto dto, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(dto.LabelName) || string.IsNullOrWhiteSpace(dto.Unit))
		{
			return BadRequest("LabelName and Unit are required.");
		}

		CarbonReference entity;
		if (dto.Id is { } id && id > 0)
		{
			entity = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.Id == id, ct)
				?? throw new KeyNotFoundException("Carbon reference not found.");

			entity.LabelName = dto.LabelName;
			entity.Category = dto.Category;
			entity.Co2Factor = dto.Co2Factor;
			entity.Unit = dto.Unit;
			entity.Region = dto.Region;
		}
		else
		{
			entity = new CarbonReference
			{
				LabelName = dto.LabelName,
				Category = dto.Category,
				Co2Factor = dto.Co2Factor,
				Unit = dto.Unit,
				Region = dto.Region
			};
			await _db.CarbonReferences.AddAsync(entity, ct);
		}

		await _db.SaveChangesAsync(ct);

		var result = new CarbonReferenceDto
		{
			Id = entity.Id,
			LabelName = entity.LabelName,
			Category = entity.Category,
			Co2Factor = entity.Co2Factor,
			Unit = entity.Unit,
			Region = entity.Region
		};

		return Ok(result);
	}

	/// <summary>
	/// 删除碳排放因子。
	/// </summary>
	[HttpDelete("carbon-reference/{id:int}")]
	public async Task<IActionResult> DeleteCarbonReference([FromRoute] int id, CancellationToken ct)
	{
		var entity = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.Id == id, ct);
		if (entity is null) return NotFound();

		_db.CarbonReferences.Remove(entity);
		await _db.SaveChangesAsync(ct);

		return NoContent();
	}

	/// <summary>
	/// 封禁/解封用户。
	/// </summary>
	[HttpPost("users/{id:int}/ban")]
	public async Task<IActionResult> BanUser([FromRoute] int id, [FromBody] BanUserRequestDto dto, CancellationToken ct)
	{
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
		if (user is null) return NotFound();

		user.IsActive = !dto.Ban;
		await _db.SaveChangesAsync(ct);

		return Ok();
	}

	/// <summary>
	/// 删除帖子（软删除 IsDeleted=true）。
	/// </summary>
	[HttpDelete("posts/{id:int}")]
	public async Task<IActionResult> DeletePost([FromRoute] int id, CancellationToken ct)
	{
		var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
		if (post is null) return NotFound();

		post.IsDeleted = true;
		await _db.SaveChangesAsync(ct);
		return NoContent();
	}
}


