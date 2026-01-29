using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Diet;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DietController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly ILogger<DietController> _logger;

	public DietController(ApplicationDbContext db, ILogger<DietController> logger)
	{
		_db = db;
		_logger = logger;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 创建饮食记录（存储名称、重量、碳排放因子、碳排放值、记录时间）。
	/// </summary>
	[HttpPost]
	[ProducesResponseType(typeof(DietRecordResponseDto), StatusCodes.Status200OK)]
	public async Task<ActionResult<DietRecordResponseDto>> Create([FromBody] CreateDietRecordDto dto, CancellationToken ct = default)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);

		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var emission = (decimal)dto.Amount * dto.EmissionFactor;

		var record = new DietRecord
		{
			UserId = userId.Value,
			Name = dto.Name,
			Amount = dto.Amount,
			EmissionFactor = dto.EmissionFactor,
			Emission = emission
		};

		await _db.DietRecords.AddAsync(record, ct);
		await _db.SaveChangesAsync(ct);

		return Ok(ToDto(record));
	}

	/// <summary>
	/// 获取当前用户的饮食记录（分页）。
	/// </summary>
	[HttpGet("my-diets")]
	[ProducesResponseType(typeof(PagedResultDto<DietRecordResponseDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<PagedResultDto<DietRecordResponseDto>>> GetMyDiets([FromQuery] GetDietRecordsQueryDto? query, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		query ??= new GetDietRecordsQueryDto();

		var baseQuery = _db.DietRecords.AsNoTracking().Where(r => r.UserId == userId.Value);
		if (query.StartDate.HasValue)
		{
			baseQuery = baseQuery.Where(r => r.CreatedAt >= query.StartDate.Value);
		}
		if (query.EndDate.HasValue)
		{
			var endInc = query.EndDate.Value.Date.AddDays(1);
			baseQuery = baseQuery.Where(r => r.CreatedAt < endInc);
		}

		var total = await baseQuery.CountAsync(ct);
		var records = await baseQuery
			.OrderByDescending(r => r.CreatedAt)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.ToListAsync(ct);

		var items = records.Select(ToDto).ToList();
		return Ok(new PagedResultDto<DietRecordResponseDto>
		{
			Items = items,
			TotalCount = total,
			Page = query.Page,
			PageSize = query.PageSize
		});
	}

	/// <summary>
	/// 根据ID获取饮食记录详情。
	/// </summary>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(DietRecordResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<DietRecordResponseDto>> GetById(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var record = await _db.DietRecords.AsNoTracking()
			.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);
		if (record is null) return NotFound(new { error = "Diet record not found or access denied" });

		return Ok(ToDto(record));
	}

	/// <summary>
	/// 删除饮食记录。
	/// </summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> Delete(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var record = await _db.DietRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);
		if (record is null) return NotFound(new { error = "Diet record not found or access denied" });

		_db.DietRecords.Remove(record);
		await _db.SaveChangesAsync(ct);

		_logger.LogInformation("Diet record deleted: UserId={UserId}, RecordId={Id}", userId, id);
		return Ok(new { message = "Deleted successfully" });
	}

	private static DietRecordResponseDto ToDto(DietRecord r) => new()
	{
		Id = r.Id,
		CreatedAt = r.CreatedAt,
		Name = r.Name,
		Amount = r.Amount,
		EmissionFactor = r.EmissionFactor,
		Emission = r.Emission
	};
}
