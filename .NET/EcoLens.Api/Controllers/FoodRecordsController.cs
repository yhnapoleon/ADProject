using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoodRecordsController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly ILogger<FoodRecordsController> _logger;

	public FoodRecordsController(ApplicationDbContext db, ILogger<FoodRecordsController> logger)
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
	/// 获取当前用户的食物记录列表（支持日期与分页）。
	/// 返回字段：名称、重量、碳排放因子、总碳排放值、创建时间。
	/// </summary>
	[HttpGet("my-records")]
	[ProducesResponseType(typeof(PagedResultDto<FoodRecordResponseDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<PagedResultDto<FoodRecordResponseDto>>> GetMyRecords([FromQuery] GetFoodRecordsQueryDto? query, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null)
		{
			return Unauthorized(new { error = "Unable to get user information, please login again" });
		}

		query ??= new GetFoodRecordsQueryDto();

		var baseQuery = _db.FoodRecords
			.AsNoTracking()
			.Where(r => r.UserId == userId.Value);

		if (query.StartDate.HasValue)
		{
			baseQuery = baseQuery.Where(r => r.CreatedAt >= query.StartDate.Value);
		}
		if (query.EndDate.HasValue)
		{
			var endInc = query.EndDate.Value.Date.AddDays(1);
			baseQuery = baseQuery.Where(r => r.CreatedAt < endInc);
		}

		var totalCount = await baseQuery.CountAsync(ct);

		var records = await baseQuery
			.OrderByDescending(r => r.CreatedAt)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.ToListAsync(ct);

		var items = records.Select(ToDto).ToList();

		return Ok(new PagedResultDto<FoodRecordResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize
		});
	}

	/// <summary>
	/// 根据ID获取单条食物记录详情。
	/// </summary>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(FoodRecordResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<FoodRecordResponseDto>> GetById(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var record = await _db.FoodRecords
			.AsNoTracking()
			.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);

		if (record is null) return NotFound(new { error = "Food record not found or access denied" });

		return Ok(ToDto(record));
	}

	/// <summary>
	/// 删除食物记录（仅可删除当前用户自己的记录）。
	/// </summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult> Delete(int id, CancellationToken ct = default)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var record = await _db.FoodRecords
			.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);

		if (record is null) return NotFound(new { error = "Food record not found or access denied" });

		_db.FoodRecords.Remove(record);
		await _db.SaveChangesAsync(ct);

		_logger.LogInformation("Food record deleted: UserId={UserId}, RecordId={Id}", userId, id);
		return Ok(new { message = "Deleted successfully" });
	}

	private static FoodRecordResponseDto ToDto(FoodRecord r)
		=> new()
		{
			Id = r.Id,
			CreatedAt = r.CreatedAt,
			Name = r.Name,
			Amount = r.Amount,
			EmissionFactor = r.EmissionFactor,
			Emission = r.Emission
		};
}
*** End Patch ***!
