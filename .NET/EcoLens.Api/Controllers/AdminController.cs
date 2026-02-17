using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
	/// 获取当前登录的管理员用户ID。
	/// </summary>
	private int? GetCurrentAdminId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
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

	#region Emission Factors (/admin/emission-factors)

	public class EmissionFactorListItem
	{
		public string Id { get; set; } = string.Empty;
		public string Category { get; set; } = string.Empty;
		public string ItemName { get; set; } = string.Empty;
		public decimal Factor { get; set; }
		public string Unit { get; set; } = string.Empty;
		public string? Source { get; set; }
		public string Status { get; set; } = "Published";
		public string LastUpdated { get; set; } = string.Empty;
	}

	[HttpGet("emission-factors")]
	public async Task<ActionResult<object>> GetEmissionFactors([FromQuery] string? q, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		if (page <= 0) page = 1;
		if (pageSize <= 0 || pageSize > 100) pageSize = 20;

		IQueryable<CarbonReference> query = _db.CarbonReferences.AsQueryable();
		if (!string.IsNullOrWhiteSpace(q))
			query = query.Where(c => c.LabelName.Contains(q));
		if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<CarbonCategory>(category, true, out var cat))
			query = query.Where(c => c.Category == cat);

		var total = await query.CountAsync(ct);
		var items = await query
			.OrderBy(c => c.LabelName)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(c => new EmissionFactorListItem
			{
				Id = c.Id.ToString(),
				Category = c.Category.ToString(),
				ItemName = c.LabelName,
				Factor = c.Co2Factor,
				Unit = c.Unit,
				Source = c.Source,
				Status = "Published",
				LastUpdated = c.UpdatedAt.ToString("yyyy-MM-dd")
			})
			.ToListAsync(ct);

		return Ok(new { items, total });
	}

	[HttpPost("emission-factors")]
	public async Task<ActionResult<EmissionFactorListItem>> CreateEmissionFactor([FromBody] EmissionFactorListItem dto, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(dto.ItemName) || string.IsNullOrWhiteSpace(dto.Unit) || string.IsNullOrWhiteSpace(dto.Category))
			return BadRequest("ItemName/Unit/Category required.");
		if (!Enum.TryParse<CarbonCategory>(dto.Category, true, out var cat)) return BadRequest("Invalid category.");

		var entity = new CarbonReference
		{
			LabelName = dto.ItemName,
			Category = cat,
			Co2Factor = dto.Factor,
			Unit = dto.Unit,
			Source = dto.Source ?? "Admin"
		};
		await _db.CarbonReferences.AddAsync(entity, ct);
		await _db.SaveChangesAsync(ct);

		return Ok(new EmissionFactorListItem
		{
			Id = entity.Id.ToString(),
			Category = entity.Category.ToString(),
			ItemName = entity.LabelName,
			Factor = entity.Co2Factor,
			Unit = entity.Unit,
			Source = entity.Source,
			Status = "Published",
			LastUpdated = entity.UpdatedAt.ToString("yyyy-MM-dd")
		});
	}

	[HttpPost("emission-factors/import")]
	[Consumes("multipart/form-data")]
	public async Task<ActionResult<object>> ImportEmissionFactors([FromForm] IFormFile file, CancellationToken ct)
	{
		if (file is null || file.Length == 0) return BadRequest("No file provided.");

		int imported = 0;
		var errors = new List<object>();
		using var reader = new StreamReader(file.OpenReadStream());
		var text = await reader.ReadToEndAsync();
		List<EmissionFactorListItem> items;
		try
		{
			// 仅支持 JSON 数组 [{itemName, category, factor, unit, source}]
			items = System.Text.Json.JsonSerializer.Deserialize<List<EmissionFactorListItem>>(text, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
		}
		catch (System.Text.Json.JsonException)
		{
			errors.Add(new { row = 0, message = "Failed to parse JSON." });
			return Ok(new { importedCount = 0, errors });
		}

		for (var i = 0; i < items.Count; i++)
		{
			var x = items[i];
			// 基础校验
			if (string.IsNullOrWhiteSpace(x.ItemName) || string.IsNullOrWhiteSpace(x.Unit) || string.IsNullOrWhiteSpace(x.Category))
			{
				errors.Add(new { row = i + 1, message = "ItemName/Unit/Category required." });
				continue;
			}
			if (!Enum.TryParse<CarbonCategory>(x.Category, true, out var cat))
			{
				errors.Add(new { row = i + 1, message = "Invalid category." });
				continue;
			}

			var entity = new CarbonReference
			{
				LabelName = x.ItemName,
				Category = cat,
				Co2Factor = x.Factor,
				Unit = x.Unit,
				Source = x.Source ?? "Import"
			};

			try
			{
				await _db.CarbonReferences.AddAsync(entity, ct);
				await _db.SaveChangesAsync(ct);
				imported++;
			}
			catch (DbUpdateException)
			{
				// 回滚当前实体的跟踪，避免影响后续条目
				_db.Entry(entity).State = EntityState.Detached;
				errors.Add(new { row = i + 1, message = "Failed to save (possibly duplicate or constraint violation)." });
			}
			catch (Exception ex)
			{
				_db.Entry(entity).State = EntityState.Detached;
				errors.Add(new { row = i + 1, message = $"Unexpected error: {ex.Message}" });
			}
		}

		return Ok(new { importedCount = imported, errors });
	}

	[HttpPut("emission-factors/{id}")]
	public async Task<ActionResult<EmissionFactorListItem>> UpdateEmissionFactor([FromRoute] string id, [FromBody] EmissionFactorListItem dto, CancellationToken ct)
	{
		if (!int.TryParse(id, out var fid)) return NotFound();
		var entity = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.Id == fid, ct);
		if (entity is null) return NotFound();

		if (!string.IsNullOrWhiteSpace(dto.ItemName)) entity.LabelName = dto.ItemName;
		if (!string.IsNullOrWhiteSpace(dto.Unit)) entity.Unit = dto.Unit;
		if (!string.IsNullOrWhiteSpace(dto.Category) && Enum.TryParse<CarbonCategory>(dto.Category, true, out var cat)) entity.Category = cat;
		if (dto.Factor >= 0) entity.Co2Factor = dto.Factor;
		if (!string.IsNullOrWhiteSpace(dto.Source)) entity.Source = dto.Source;
		await _db.SaveChangesAsync(ct);

		return Ok(new EmissionFactorListItem
		{
			Id = entity.Id.ToString(),
			Category = entity.Category.ToString(),
			ItemName = entity.LabelName,
			Factor = entity.Co2Factor,
			Unit = entity.Unit,
			Source = entity.Source,
			Status = "Published",
			LastUpdated = entity.UpdatedAt.ToString("yyyy-MM-dd")
		});
	}

	[HttpDelete("emission-factors/{id}")]
	public async Task<ActionResult<object>> DeleteEmissionFactor([FromRoute] string id, CancellationToken ct)
	{
		if (!int.TryParse(id, out var fid)) return NotFound();
		var entity = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.Id == fid, ct);
		if (entity is null) return NotFound();
		_db.CarbonReferences.Remove(entity);
		await _db.SaveChangesAsync(ct);
		return Ok(new { deleted = true });
	}

	#endregion

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
		var currentAdminId = GetCurrentAdminId();
		if (currentAdminId is null) return Unauthorized();

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
		if (user is null) return NotFound();

		// 保护机制1：防止管理员封禁自己
		if (user.Id == currentAdminId.Value)
		{
			return BadRequest("Cannot ban yourself. Please ask another admin to perform this action.");
		}

		// 保护机制2：如果目标用户是管理员，检查是否是最后一个活跃的管理员
		if (user.Role == UserRole.Admin && dto.Ban)
		{
			var activeAdminCount = await _db.ApplicationUsers
				.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id, ct);
			
			if (activeAdminCount == 0)
			{
				return BadRequest("Cannot ban the last active admin. At least one admin must remain active.");
			}
		}

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

	#region Regions & Impact

	public class RegionStatItem
	{
		public string RegionCode { get; set; } = string.Empty;
		public string RegionName { get; set; } = string.Empty;
		public decimal CarbonReduced { get; set; }
		public int UserCount { get; set; }
		public decimal ReductionRate { get; set; }
	}

	/// <summary>
	/// 将用户自由填写的 Region 文本归一化为 GeoJSON 中使用的 REGION_C 代码。
	/// - 返回的 Code 为 WR/NR/NER/ER/CR 之一（或空字符串表示无法识别）。
	/// - Name 为规范化后的展示名称（如 "West Region"）。
	/// </summary>
	private static (string Code, string Name) NormalizeRegion(string? regionRaw)
	{
		if (string.IsNullOrWhiteSpace(regionRaw))
			return (string.Empty, string.Empty);

		var original = regionRaw.Trim();
		var upper = original.ToUpperInvariant();
		var compact = upper.Replace(" ", string.Empty).Replace("-", string.Empty);

		// 已经是标准代码的情况
		if (compact == "WR") return ("WR", "West Region");
		if (compact == "NR") return ("NR", "North Region");
		if (compact == "NER") return ("NER", "North-East Region");
		if (compact == "ER") return ("ER", "East Region");
		if (compact == "CR") return ("CR", "Central Region");

		// 英文描述（允许大小写、空格、短横线差异）
		if (compact.Contains("WEST"))
			return ("WR", "West Region");
		if (compact.Contains("NORTHEAST") || compact.Contains("NORTHEASTREGION") || compact.Contains("NORTHEASTERN"))
			return ("NER", "North-East Region");
		if (compact.Contains("NORTH"))
			return ("NR", "North Region");
		if (compact.Contains("EAST"))
			return ("ER", "East Region");
		if (compact.Contains("CENTRAL"))
			return ("CR", "Central Region");
		if (compact.Contains("SINGAPORE"))
			// 用户只写 "Singapore" 时，默认归到 Central Region，避免整张图无数据。
			return ("CR", "Central Region");

		// 简单的中文关键字映射
		if (original.Contains("西"))
			return ("WR", "West Region");
		if (original.Contains("东北"))
			return ("NER", "North-East Region");
		if (original.Contains("北"))
			return ("NR", "North Region");
		if (original.Contains("东"))
			return ("ER", "East Region");
		if (original.Contains("中"))
			return ("CR", "Central Region");

		// 无法识别：保留原始名称，但不返回代码（前端看不到这部分区域）。
		return (string.Empty, original);
	}

	/// <summary>
	/// 按用户累计的 TotalCarbonSaved（总碳减排量）在各 Region 间进行聚合统计。
	/// 注意：当前实现不再按时间范围切分 from/to/days，而是始终使用用户表中的累计值，
	/// 以保证与顶部 Carbon Reduced 卡片语义一致。
	/// </summary>
	private async Task<List<RegionStatItem>> ComputeRegionStatsByDateRange(DateTime? fromInclusiveUtc, DateTime? toExclusiveUtc, CancellationToken ct)
	{
		// 先按用户原始 Region 进行分组，汇总每个 Region 的总减排量与用户数
		var groups = await _db.ApplicationUsers
			.Where(u => u.Region != null && u.Region != string.Empty)
			.GroupBy(u => u.Region!)
			.Select(g => new
			{
				Region = g.Key,
				TotalSaved = g.Sum(x => x.TotalCarbonSaved),
				UserCount = g.Count()
			})
			.ToListAsync(ct);

		// 将不同写法的 Region 合并到统一的 REGION_C 代码上
		var userCountByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var savedByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
		var nameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var g in groups)
		{
			var norm = NormalizeRegion(g.Region);
			if (string.IsNullOrEmpty(norm.Code)) continue;

			if (!userCountByCode.TryAdd(norm.Code, g.UserCount))
				userCountByCode[norm.Code] += g.UserCount;

			if (!savedByCode.TryAdd(norm.Code, g.TotalSaved))
				savedByCode[norm.Code] += g.TotalSaved;

			if (!string.IsNullOrWhiteSpace(norm.Name))
				nameByCode[norm.Code] = norm.Name;
		}

		var totalSaved = savedByCode.Values.Sum();

		var items = userCountByCode.Select(kvp =>
		{
			var code = kvp.Key;
			savedByCode.TryGetValue(code, out var saved);
			nameByCode.TryGetValue(code, out var name);

			return new RegionStatItem
			{
				RegionCode = code,
				RegionName = string.IsNullOrWhiteSpace(name) ? code : name,
				CarbonReduced = saved,
				UserCount = kvp.Value,
				ReductionRate = totalSaved > 0 ? Math.Round(saved * 100m / totalSaved, 2) : 0m
			};
		}).ToList();

		return items;
	}

	[HttpGet("regions/stats")]
	public async Task<ActionResult<IEnumerable<RegionStatItem>>> RegionStats(
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null,
		[FromQuery] int? days = null,
		CancellationToken ct = default)
	{
		// 当提供了时间范围（from/to/days）时，按 ActivityLogs 在时间窗口内聚合；
		// 否则维持原有逻辑：按用户累计的 TotalCarbonSaved 聚合。
		if (from.HasValue || to.HasValue || (days.HasValue && days.Value > 0))
		{
			DateTime? start = null;
			DateTime? endExclusive = null;

			if (days.HasValue && days.Value > 0)
			{
				var today = DateTime.UtcNow.Date;
				start = today.AddDays(-(days.Value - 1));
				endExclusive = today.AddDays(1);
			}

			if (from.HasValue) start = from.Value.Date;
			if (to.HasValue) endExclusive = to.Value.Date.AddDays(1);

			var ranged = await ComputeRegionStatsByDateRange(start, endExclusive, ct);
			return Ok(ranged);
		}
		else
		{
			var groups = await _db.ApplicationUsers
				.Where(u => u.Region != null && u.Region != string.Empty)
				.GroupBy(u => u.Region!)
				.Select(g => new
				{
					Region = g.Key,
					TotalSaved = g.Sum(x => x.TotalCarbonSaved),
					UserCount = g.Count()
				})
				.ToListAsync(ct);

			// 将不同写法的 Region 合并到统一的 REGION_C 代码上
			var userCountByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var savedByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
			var nameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var g in groups)
			{
				var norm = NormalizeRegion(g.Region);
				if (string.IsNullOrEmpty(norm.Code)) continue;

				if (!userCountByCode.TryAdd(norm.Code, g.UserCount))
					userCountByCode[norm.Code] += g.UserCount;

				if (!savedByCode.TryAdd(norm.Code, g.TotalSaved))
					savedByCode[norm.Code] += g.TotalSaved;

				if (!string.IsNullOrWhiteSpace(norm.Name))
					nameByCode[norm.Code] = norm.Name;
			}

			var totalSaved = savedByCode.Values.Sum();

			var items = userCountByCode.Select(kvp =>
			{
				var code = kvp.Key;
				savedByCode.TryGetValue(code, out var saved);
				nameByCode.TryGetValue(code, out var name);

				return new RegionStatItem
				{
					RegionCode = code,
					RegionName = string.IsNullOrWhiteSpace(name) ? code : name,
					CarbonReduced = saved,
					UserCount = kvp.Value,
					ReductionRate = totalSaved > 0 ? Math.Round(saved * 100m / totalSaved, 2) : 0m
				};
			}).ToList();

			return Ok(items);
		}
	}

	/// <summary>
	/// 路由别名：/api/regions/stats（与前端文档对齐，仍需 Admin 权限）
	/// </summary>
	[HttpGet("/api/regions/stats")]
	public Task<ActionResult<IEnumerable<RegionStatItem>>> RegionStatsAlias(CancellationToken ct) => RegionStats(null, null, null, ct);

	/// <summary>
	/// 便捷接口：近 7 天区域热力统计
	/// </summary>
	[HttpGet("regions/stats/weekly")]
	public Task<ActionResult<IEnumerable<RegionStatItem>>> RegionStatsWeekly(CancellationToken ct) => RegionStats(null, null, 7, ct);

	/// <summary>
	/// 便捷接口：近 30 天区域热力统计
	/// </summary>
	[HttpGet("regions/stats/monthly")]
	public Task<ActionResult<IEnumerable<RegionStatItem>>> RegionStatsMonthly(CancellationToken ct) => RegionStats(null, null, 30, ct);

	[HttpGet("impact/weekly")]
	public async Task<ActionResult<IEnumerable<object>>> WeeklyImpact(CancellationToken ct)
	{
		var today = DateTime.UtcNow.Date;
		var start = today.AddDays(-34); // 最近5周
		var logs = await _db.ActivityLogs.Where(l => l.CreatedAt.Date >= start && l.CreatedAt.Date <= today).ToListAsync(ct);
		var weeks = Enumerable.Range(0, 5).Select(i => new
		{
			week = $"Week {i + 1}",
			from = start.AddDays(i * 7),
			to = start.AddDays(i * 7 + 6)
		}).ToList();
		var result = weeks.Select(w => new
		{
			week = w.week,
			value = logs.Where(l => l.CreatedAt.Date >= w.from && l.CreatedAt.Date <= w.to).Sum(x => x.TotalEmission)
		});
		return Ok(result);
	}

	#endregion

	#region Admin Users

	[HttpGet("users")]
	public async Task<ActionResult<object>> AdminUsers([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
	{
		if (page <= 0) page = 1; if (pageSize <= 0 || pageSize > 100) pageSize = 20;
		IQueryable<ApplicationUser> query = _db.ApplicationUsers.AsQueryable();
		if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.Username.Contains(q) || u.Email.Contains(q));
		var total = await query.CountAsync(ct);
		var items = await query.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
			.Select(u => new
			{
				id = u.Id.ToString(),
				username = u.Username,
				email = u.Email,
				joinedDate = u.CreatedAt.ToString("yyyy-MM-dd"),
				totalReduction = u.TotalCarbonSaved,
				points = u.CurrentPoints,
				status = u.IsActive ? "Active" : "Banned"
			}).ToListAsync(ct);
		return Ok(new { items, total });
	}

	/// <summary>
	/// 获取指定用户在给定时间范围内的排放统计（聚合 ActivityLogs、FoodRecords、TravelLogs、UtilityBills）。
	/// - 支持三种时间选择：
	///   1) days（近 N 天，含今日）；
	///   2) from/to（闭区间的日期，内部按 [from, to+1) 处理）；
	///   3) 若均未提供，则统计用户全量记录。
	/// </summary>
	/// <param name="id">用户ID</param>
	/// <param name="from">开始日期（UTC，按日期计算）</param>
	/// <param name="to">结束日期（UTC，按日期计算）</param>
	/// <param name="days">近 N 天（N>0）</param>
	/// <param name="ct">取消令牌</param>
	[HttpGet("users/{id:int}/emissions/stats")]
	public async Task<ActionResult<object>> GetUserEmissionStats(
		[FromRoute] int id,
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null,
		[FromQuery] int? days = null,
		CancellationToken ct = default)
	{
		// 校验用户是否存在
		var userExists = await _db.ApplicationUsers.AnyAsync(u => u.Id == id, ct);
		if (!userExists) return NotFound(new { error = "User not found" });

		// 计算时间窗口（采用 [start, endExclusive)）
		DateTime? start = null;
		DateTime? endExclusive = null;

		if (days.HasValue && days.Value > 0)
		{
			var today = DateTime.UtcNow.Date;
			start = today.AddDays(-(days.Value - 1));
			endExclusive = today.AddDays(1);
		}

		if (from.HasValue) start = from.Value.Date;
		if (to.HasValue) endExclusive = to.Value.Date.AddDays(1);

		// 1) ActivityLogs
		var logsQuery = _db.ActivityLogs.Where(l => l.UserId == id);
		if (start.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt >= start.Value);
		if (endExclusive.HasValue) logsQuery = logsQuery.Where(l => l.CreatedAt < endExclusive.Value);
		var logsCount = await logsQuery.CountAsync(ct);
		var logsEmission = await logsQuery.SumAsync(l => (decimal?)l.TotalEmission, ct) ?? 0m;

		// 2) FoodRecords（表可能不存在则忽略）
		var foodCount = 0;
		var foodEmission = 0m;
		try
		{
			var foodQuery = _db.FoodRecords.Where(r => r.UserId == id);
			if (start.HasValue) foodQuery = foodQuery.Where(r => r.CreatedAt >= start.Value);
			if (endExclusive.HasValue) foodQuery = foodQuery.Where(r => r.CreatedAt < endExclusive.Value);
			foodCount = await foodQuery.CountAsync(ct);
			foodEmission = await foodQuery.SumAsync(r => (decimal?)r.Emission, ct) ?? 0m;
		}
		catch (Exception) { /* FoodRecords 表可能不存在 */ }

		// 3) TravelLogs
		var travelQuery = _db.TravelLogs.Where(t => t.UserId == id);
		if (start.HasValue) travelQuery = travelQuery.Where(t => t.CreatedAt >= start.Value);
		if (endExclusive.HasValue) travelQuery = travelQuery.Where(t => t.CreatedAt < endExclusive.Value);
		var travelCount = await travelQuery.CountAsync(ct);
		var travelEmission = await travelQuery.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

		// 4) UtilityBills
		var utilityQuery = _db.UtilityBills.Where(b => b.UserId == id);
		if (start.HasValue) utilityQuery = utilityQuery.Where(b => b.CreatedAt >= start.Value);
		if (endExclusive.HasValue) utilityQuery = utilityQuery.Where(b => b.CreatedAt < endExclusive.Value);
		var utilityCount = await utilityQuery.CountAsync(ct);
		var utilityEmission = await utilityQuery.SumAsync(b => (decimal?)b.TotalCarbonEmission, ct) ?? 0m;

		var totalItems = logsCount + foodCount + travelCount + utilityCount;
		var totalEmission = logsEmission + foodEmission + travelEmission + utilityEmission;

		return Ok(new
		{
			userId = id,
			totalItems,
			totalEmission,
			from = start?.ToString("yyyy-MM-dd"),
			to = endExclusive?.AddDays(-1).ToString("yyyy-MM-dd")
		});
	}

	public class BatchUserUpdateItem
	{
		public string Id { get; set; } = string.Empty;
		public int? Points { get; set; }
		public string? Status { get; set; } // Active/Banned
	}

	public class BatchUserUpdateRequest
	{
		public List<BatchUserUpdateItem> Updates { get; set; } = new();
	}

	[HttpPost("users/batch-update")]
	public async Task<ActionResult<object>> BatchUpdateUsers([FromBody] BatchUserUpdateRequest req, CancellationToken ct)
	{
		var currentAdminId = GetCurrentAdminId();
		if (currentAdminId is null) return Unauthorized();

		int updated = 0;
		var errors = new List<string>();

		foreach (var x in req.Updates)
		{
			if (!int.TryParse(x.Id, out var uid)) continue;
			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == uid, ct);
			if (user is null) continue;

			// 保护机制1：防止管理员封禁自己
			if (user.Id == currentAdminId.Value && !string.IsNullOrWhiteSpace(x.Status))
			{
				var isActive = string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase);
				if (!isActive)
				{
					errors.Add($"User {user.Username}: Cannot ban yourself.");
					continue;
				}
			}

			// 保护机制2：如果目标用户是管理员且要封禁，检查是否是最后一个活跃的管理员
			if (user.Role == UserRole.Admin && !string.IsNullOrWhiteSpace(x.Status))
			{
				var isActive = string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase);
				if (!isActive)
				{
					var activeAdminCount = await _db.ApplicationUsers
						.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != uid, ct);
					
					if (activeAdminCount == 0)
					{
						errors.Add($"User {user.Username}: Cannot ban the last active admin.");
						continue;
					}
				}
			}

			if (x.Points.HasValue) user.CurrentPoints = x.Points.Value;
			if (!string.IsNullOrWhiteSpace(x.Status)) user.IsActive = string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase);
			updated++;
		}

		await _db.SaveChangesAsync(ct);
		
		if (errors.Count > 0)
		{
			return Ok(new { updatedCount = updated, errors });
		}

		return Ok(new { updatedCount = updated });
	}

	#endregion

	#region Settings

	public class SettingsDto
	{
		public int ConfidenceThreshold { get; set; }
		public string VisionModel { get; set; } = "default";
		public bool WeeklyDigest { get; set; }
		public bool MaintenanceMode { get; set; }
	}

	[HttpGet("settings")]
	public async Task<ActionResult<SettingsDto>> GetSettings(CancellationToken ct)
	{
		var s = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Id == 1, ct) ?? new Models.SystemSettings();
		return Ok(new SettingsDto
		{
			ConfidenceThreshold = s.ConfidenceThreshold,
			VisionModel = s.VisionModel,
			WeeklyDigest = s.WeeklyDigest,
			MaintenanceMode = s.MaintenanceMode
		});
	}

	[HttpPut("settings")]
	public async Task<ActionResult<SettingsDto>> UpdateSettings([FromBody] SettingsDto dto, CancellationToken ct)
	{
		var s = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
		if (s is null)
		{
			s = new Models.SystemSettings { Id = 1 };
			await _db.SystemSettings.AddAsync(s, ct);
		}
		s.ConfidenceThreshold = dto.ConfidenceThreshold;
		s.VisionModel = dto.VisionModel;
		s.WeeklyDigest = dto.WeeklyDigest;
		s.MaintenanceMode = dto.MaintenanceMode;
		await _db.SaveChangesAsync(ct);
		return await GetSettings(ct);
	}

	#endregion

	#region Analytics

	/// <summary>
	/// 类目占比（按 ActivityLog 的 CarbonReference.Category 汇总，并包含 TravelLogs、UtilityBills、FoodRecords 的碳排放）。
	/// </summary>
	[HttpGet("analytics/category-share")]
	public async Task<ActionResult<IEnumerable<object>>> CategoryShare(CancellationToken ct)
	{
		// 在数据库端完成聚合，避免将整表加载到内存
		// 1. 从 ActivityLogs 按 Category 汇总
		var grouped = await _db.ActivityLogs
			.Join(_db.CarbonReferences,
				l => l.CarbonReferenceId,
				c => c.Id,
				(l, c) => new { c.Category, l.TotalEmission })
			.GroupBy(x => x.Category)
			.Select(g => new { Category = g.Key, Total = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		// 2. 从 TravelLogs 汇总所有出行碳排放（计入 Transport 类别）
		var travelEmission = await _db.TravelLogs
			.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

		// 3. 从 UtilityBills 汇总所有水电账单碳排放（计入 Utility 类别）
		var utilityBillsEmission = await _db.UtilityBills
			.SumAsync(b => (decimal?)b.TotalCarbonEmission, ct) ?? 0m;

		// 4. 从 FoodRecords 汇总所有食物记录碳排放（计入 Food 类别；表可能不存在则忽略）
		decimal foodRecordsEmission = 0m;
		try
		{
			foodRecordsEmission = await _db.FoodRecords.SumAsync(r => r.Emission, ct);
		}
		catch (Exception)
		{
			// FoodRecords 表可能不存在（未执行迁移），忽略
		}

		// 5. 计算各类别的总碳排放
		decimal GetTotal(CarbonCategory cat) => grouped.FirstOrDefault(x => x.Category == cat)?.Total ?? 0m;
		var food = GetTotal(CarbonCategory.Food) + foodRecordsEmission; // Food = ActivityLogs 中的 Food + FoodRecords
		var transport = GetTotal(CarbonCategory.Transport) + travelEmission; // Transport = ActivityLogs 中的 Transport + TravelLogs
		var utility = GetTotal(CarbonCategory.Utility) + utilityBillsEmission; // Utility = ActivityLogs 中的 Utility + UtilityBills

		// 6. 计算总碳排放和百分比
		var total = food + transport + utility;
		decimal AsPercent(decimal part) => total > 0 ? Math.Round(part * 100m / total, 2) : 0m;

		return Ok(new[]
		{
			new { name = "Food", value = AsPercent(food) },
			new { name = "Transport", value = AsPercent(transport) },
			new { name = "Utilities", value = AsPercent(utility) }
		});
	}

	/// <summary>
	/// 参与度（最近 6 个月）：返回每月 DAU/M AU（简化近似）。
	/// </summary>
	[HttpGet("analytics/engagement")]
	public async Task<ActionResult<IEnumerable<object>>> Engagement(CancellationToken ct)
	{
		var today = DateTime.UtcNow.Date;
		var startMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

		// 取最近 6 个月的 ActivityLog
		var logs = await _db.ActivityLogs
			.Where(l => l.CreatedAt >= startMonth)
			.ToListAsync(ct);

		var result = new List<object>();
		for (int i = 0; i < 6; i++)
		{
			var monthStart = new DateTime(startMonth.Year, startMonth.Month, 1).AddMonths(i);
			var monthEnd = monthStart.AddMonths(1);

			var monthLogs = logs.Where(l => l.CreatedAt >= monthStart && l.CreatedAt < monthEnd).ToList();
			var mau = monthLogs.Select(l => l.UserId).Distinct().Count();

			// 简化 DAU：用当月活跃用户均值的近似（mau / 5）
			var dau = (int)Math.Round(mau / 5.0, MidpointRounding.AwayFromZero);

			result.Add(new
			{
				month = monthStart.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture),
				dau,
				mau
			});
		}

		return Ok(result);
	}

	#endregion

	#region Aliases

	/// <summary>
	/// 路由别名：/api/admin/users/batch（PUT），与批量更新对齐。
	/// </summary>
	[HttpPut("users/batch")]
	public Task<ActionResult<object>> BatchUpdateUsersAlias([FromBody] BatchUserUpdateRequest req, CancellationToken ct)
		=> BatchUpdateUsers(req, ct);

	/// <summary>
	/// 路由别名：/api/admin/users/batch（PATCH），与前端文档对齐。
	/// </summary>
	[HttpPatch("users/batch")]
	public Task<ActionResult<object>> BatchUpdateUsersPatch([FromBody] BatchUserUpdateRequest req, CancellationToken ct)
		=> BatchUpdateUsers(req, ct);

	#endregion

	#region Database Management

	/// <summary>
	/// 获取数据库统计信息（各表的记录数）。
	/// </summary>
	[HttpGet("database/statistics")]
	public async Task<ActionResult<object>> GetDatabaseStatistics(CancellationToken ct)
	{
		var stats = new Dictionary<string, object>();

		// 基础表（肯定存在）
		stats["users"] = await _db.ApplicationUsers.CountAsync(ct);
		stats["activeUsers"] = await _db.ApplicationUsers.CountAsync(u => u.IsActive, ct);
		stats["adminUsers"] = await _db.ApplicationUsers.CountAsync(u => u.Role == UserRole.Admin, ct);
		stats["travelLogs"] = await _db.TravelLogs.CountAsync(ct);
		stats["utilityBills"] = await _db.UtilityBills.CountAsync(ct);
		stats["activityLogs"] = await _db.ActivityLogs.CountAsync(ct);
		stats["posts"] = await _db.Posts.CountAsync(ct);
		stats["comments"] = await _db.Comments.CountAsync(ct);
		stats["carbonReferences"] = await _db.CarbonReferences.CountAsync(ct);
		stats["userFollows"] = await _db.UserFollows.CountAsync(ct);

		// 可能不存在的表（使用 try-catch）
		try { stats["foodRecords"] = await _db.FoodRecords.CountAsync(ct); } catch { stats["foodRecords"] = 0; }
		try { stats["dietRecords"] = await _db.DietRecords.CountAsync(ct); } catch { stats["dietRecords"] = 0; }
		try { stats["barcodeReferences"] = await _db.BarcodeReferences.CountAsync(ct); } catch { stats["barcodeReferences"] = 0; }
		try { stats["aiInsights"] = await _db.AiInsights.CountAsync(ct); } catch { stats["aiInsights"] = 0; }
		try { stats["stepRecords"] = await _db.StepRecords.CountAsync(ct); } catch { stats["stepRecords"] = 0; }

		return Ok(stats);
	}

	/// <summary>
	/// 删除指定用户的所有数据（包括出行记录、账单、活动日志、帖子、评论等）。
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="deleteAccount">是否同时删除用户账号（默认false，只删除数据）</param>
	/// <param name="ct">取消令牌</param>
	[HttpDelete("users/{userId:int}/data")]
	public async Task<ActionResult<object>> DeleteUserData(
		[FromRoute] int userId,
		[FromQuery] bool deleteAccount = false,
		CancellationToken ct = default)
	{
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
		if (user is null)
		{
			return NotFound(new { error = "User not found" });
		}

		var currentAdminId = GetCurrentAdminId();
		if (currentAdminId is null) return Unauthorized();

		// 保护机制：防止管理员删除自己的数据
		if (user.Id == currentAdminId.Value)
		{
			return BadRequest(new { error = "Cannot delete your own data. Please ask another admin to perform this action." });
		}

		// 保护机制：如果目标用户是管理员，检查是否是最后一个活跃的管理员
		if (user.Role == UserRole.Admin && deleteAccount)
		{
			var activeAdminCount = await _db.ApplicationUsers
				.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != userId, ct);
			
			if (activeAdminCount == 0)
			{
				return BadRequest(new { error = "Cannot delete the last active admin account. At least one admin must remain active." });
			}
		}

		var deletedCounts = new Dictionary<string, int>();

		try
		{
			// 按正确顺序删除数据（考虑外键约束）
			// 1. 删除评论（依赖Posts和Users）
			var comments = await _db.Comments.Where(c => c.UserId == userId).ToListAsync(ct);
			deletedCounts["comments"] = comments.Count;
			_db.Comments.RemoveRange(comments);

			// 2. 删除帖子（依赖Users，Comments已删除）
			var posts = await _db.Posts.Where(p => p.UserId == userId).ToListAsync(ct);
			deletedCounts["posts"] = posts.Count;
			// 先删除这些帖子的所有评论
			var postIds = posts.Select(p => p.Id).ToList();
			var postComments = await _db.Comments.Where(c => postIds.Contains(c.PostId)).ToListAsync(ct);
			_db.Comments.RemoveRange(postComments);
			_db.Posts.RemoveRange(posts);

			// 3. 删除用户关注关系
			var follows = await _db.UserFollows
				.Where(f => f.FollowerId == userId || f.FolloweeId == userId)
				.ToListAsync(ct);
			deletedCounts["userFollows"] = follows.Count;
			_db.UserFollows.RemoveRange(follows);

			// 4. 删除出行记录
			var travelLogs = await _db.TravelLogs.Where(t => t.UserId == userId).ToListAsync(ct);
			deletedCounts["travelLogs"] = travelLogs.Count;
			_db.TravelLogs.RemoveRange(travelLogs);

			// 5. 删除账单
			var utilityBills = await _db.UtilityBills.Where(b => b.UserId == userId).ToListAsync(ct);
			deletedCounts["utilityBills"] = utilityBills.Count;
			_db.UtilityBills.RemoveRange(utilityBills);

			// 6. 删除活动日志
			var activityLogs = await _db.ActivityLogs.Where(a => a.UserId == userId).ToListAsync(ct);
			deletedCounts["activityLogs"] = activityLogs.Count;
			_db.ActivityLogs.RemoveRange(activityLogs);

			// 7. 删除食物记录（如果表存在）
			try
			{
				var foodRecords = await _db.FoodRecords.Where(f => f.UserId == userId).ToListAsync(ct);
				deletedCounts["foodRecords"] = foodRecords.Count;
				_db.FoodRecords.RemoveRange(foodRecords);
			}
			catch (Exception)
			{
				deletedCounts["foodRecords"] = 0;
				// 表不存在，跳过
			}

			// 8. 删除饮食记录（如果表存在）
			try
			{
				var dietRecords = await _db.DietRecords.Where(d => d.UserId == userId).ToListAsync(ct);
				deletedCounts["dietRecords"] = dietRecords.Count;
				_db.DietRecords.RemoveRange(dietRecords);
			}
			catch (Exception)
			{
				deletedCounts["dietRecords"] = 0;
				// 表不存在，跳过
			}

			// 9. 删除AI洞察（如果表存在）
			try
			{
				var aiInsights = await _db.AiInsights.Where(i => i.UserId == userId).ToListAsync(ct);
				deletedCounts["aiInsights"] = aiInsights.Count;
				_db.AiInsights.RemoveRange(aiInsights);
			}
			catch (Exception)
			{
				deletedCounts["aiInsights"] = 0;
				// 表不存在，跳过
			}

			// 10. 删除步数记录（如果表存在）
			try
			{
				var stepRecords = await _db.StepRecords.Where(s => s.UserId == userId).ToListAsync(ct);
				deletedCounts["stepRecords"] = stepRecords.Count;
				_db.StepRecords.RemoveRange(stepRecords);
			}
			catch (Exception)
			{
				deletedCounts["stepRecords"] = 0;
				// 表不存在，跳过
			}

			// 11. 可选：删除用户账号
			if (deleteAccount)
			{
				_db.ApplicationUsers.Remove(user);
				deletedCounts["user"] = 1;
			}
			else
			{
				// 重置用户统计数据
				user.TotalCarbonSaved = 0;
				user.CurrentPoints = 0;
			}

			await _db.SaveChangesAsync(ct);

			return Ok(new
			{
				success = true,
				message = deleteAccount ? "User account and all data deleted successfully" : "User data deleted successfully, account preserved",
				deletedCounts,
				totalDeleted = deletedCounts.Values.Sum()
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				error = "Failed to delete user data",
				message = ex.Message,
				deletedCounts
			});
		}
	}

	/// <summary>
	/// 清空所有数据（需要确认码 "CONFIRM"）。
	/// </summary>
	/// <param name="dto">包含确认码的请求</param>
	/// <param name="ct">取消令牌</param>
	[HttpPost("database/clear-all")]
	public async Task<ActionResult<object>> ClearAllData([FromBody] ClearAllDataRequestDto dto, CancellationToken ct = default)
	{
		// 验证确认码
		if (string.IsNullOrWhiteSpace(dto.ConfirmationCode) || 
			!dto.ConfirmationCode.Equals("CONFIRM", StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest(new { error = "Invalid confirmation code. Please provide 'CONFIRM' to proceed." });
		}

		var deletedCounts = new Dictionary<string, int>();

		try
		{
			// 按正确顺序删除所有数据（考虑外键约束）
			// 1. 删除评论（依赖Posts和Users）
			var comments = await _db.Comments.ToListAsync(ct);
			deletedCounts["comments"] = comments.Count;
			_db.Comments.RemoveRange(comments);
			await _db.SaveChangesAsync(ct);

			// 2. 删除帖子（依赖Users）
			var posts = await _db.Posts.ToListAsync(ct);
			deletedCounts["posts"] = posts.Count;
			_db.Posts.RemoveRange(posts);
			await _db.SaveChangesAsync(ct);

			// 3. 删除用户关注关系
			var follows = await _db.UserFollows.ToListAsync(ct);
			deletedCounts["userFollows"] = follows.Count;
			_db.UserFollows.RemoveRange(follows);
			await _db.SaveChangesAsync(ct);

			// 4. 删除出行记录
			var travelLogs = await _db.TravelLogs.ToListAsync(ct);
			deletedCounts["travelLogs"] = travelLogs.Count;
			_db.TravelLogs.RemoveRange(travelLogs);
			await _db.SaveChangesAsync(ct);

			// 5. 删除账单
			var utilityBills = await _db.UtilityBills.ToListAsync(ct);
			deletedCounts["utilityBills"] = utilityBills.Count;
			_db.UtilityBills.RemoveRange(utilityBills);
			await _db.SaveChangesAsync(ct);

			// 6. 删除活动日志
			var activityLogs = await _db.ActivityLogs.ToListAsync(ct);
			deletedCounts["activityLogs"] = activityLogs.Count;
			_db.ActivityLogs.RemoveRange(activityLogs);
			await _db.SaveChangesAsync(ct);

			// 7. 删除食物记录（如果表存在）
			try
			{
				var foodRecords = await _db.FoodRecords.ToListAsync(ct);
				deletedCounts["foodRecords"] = foodRecords.Count;
				_db.FoodRecords.RemoveRange(foodRecords);
				await _db.SaveChangesAsync(ct);
			}
			catch (Exception)
			{
				deletedCounts["foodRecords"] = 0;
				// 表不存在，跳过
			}

			// 8. 删除饮食记录（如果表存在）
			try
			{
				var dietRecords = await _db.DietRecords.ToListAsync(ct);
				deletedCounts["dietRecords"] = dietRecords.Count;
				_db.DietRecords.RemoveRange(dietRecords);
				await _db.SaveChangesAsync(ct);
			}
			catch (Exception)
			{
				deletedCounts["dietRecords"] = 0;
				// 表不存在，跳过
			}

			// 9. 删除AI洞察（如果表存在）
			try
			{
				var aiInsights = await _db.AiInsights.ToListAsync(ct);
				deletedCounts["aiInsights"] = aiInsights.Count;
				_db.AiInsights.RemoveRange(aiInsights);
				await _db.SaveChangesAsync(ct);
			}
			catch (Exception)
			{
				deletedCounts["aiInsights"] = 0;
				// 表不存在，跳过
			}

			// 10. 删除步数记录（如果表存在）
			try
			{
				var stepRecords = await _db.StepRecords.ToListAsync(ct);
				deletedCounts["stepRecords"] = stepRecords.Count;
				_db.StepRecords.RemoveRange(stepRecords);
				await _db.SaveChangesAsync(ct);
			}
			catch (Exception)
			{
				deletedCounts["stepRecords"] = 0;
				// 表不存在，跳过
			}

			// 11. 删除用户（保留管理员账号，如果dto.PreserveAdmins为true）
			var usersToDelete = _db.ApplicationUsers.AsQueryable();
			if (dto.PreserveAdmins)
			{
				usersToDelete = usersToDelete.Where(u => u.Role != UserRole.Admin);
			}
			var users = await usersToDelete.ToListAsync(ct);
			deletedCounts["users"] = users.Count;
			_db.ApplicationUsers.RemoveRange(users);
			await _db.SaveChangesAsync(ct);

			// 12. 可选：删除条形码引用（如果dto.IncludeReferences为true）
			if (dto.IncludeReferences)
			{
				var barcodeReferences = await _db.BarcodeReferences.ToListAsync(ct);
				deletedCounts["barcodeReferences"] = barcodeReferences.Count;
				_db.BarcodeReferences.RemoveRange(barcodeReferences);
				await _db.SaveChangesAsync(ct);
			}

			// 注意：不删除 CarbonReferences 和 SystemSettings，这些是系统配置数据

			return Ok(new
			{
				success = true,
				message = "All data cleared successfully",
				deletedCounts,
				totalDeleted = deletedCounts.Values.Sum(),
				preservedAdmins = dto.PreserveAdmins,
				preservedReferences = !dto.IncludeReferences
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new
			{
				error = "Failed to clear all data",
				message = ex.Message,
				deletedCounts
			});
		}
	}

	/// <summary>
	/// 清空所有数据的请求DTO。
	/// </summary>
	public class ClearAllDataRequestDto
	{
		/// <summary>
		/// 确认码，必须为 "CONFIRM" 才能执行删除操作。
		/// </summary>
		[Required]
		public string ConfirmationCode { get; set; } = string.Empty;

		/// <summary>
		/// 是否保留管理员账号（默认true）。
		/// </summary>
		public bool PreserveAdmins { get; set; } = true;

		/// <summary>
		/// 是否同时删除条形码引用等系统引用数据（默认false）。
		/// </summary>
		public bool IncludeReferences { get; set; } = false;
	}

	#endregion
}


