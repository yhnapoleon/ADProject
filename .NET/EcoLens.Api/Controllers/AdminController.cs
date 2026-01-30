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
			catch (DbUpdateException ex)
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
	/// 基于 ActivityLogs 在给定时间范围内（[from, to)）按 Region 聚合统计。
	/// - 若 to 传入的是当天日期，将自动按整天上限转为次日零点的开区间。
	/// - 若 from/to 任一为空，则按存在的条件过滤。
	/// </summary>
	private async Task<List<RegionStatItem>> ComputeRegionStatsByDateRange(DateTime? fromInclusiveUtc, DateTime? toExclusiveUtc, CancellationToken ct)
	{
		// 预先取出各 Region 的用户数（用于保持返回结构一致）
		var regionUsers = await _db.ApplicationUsers
			.Where(u => u.Region != null && u.Region != string.Empty)
			.GroupBy(u => u.Region!)
			.Select(g => new { Region = g.Key, UserCount = g.Count() })
			.ToListAsync(ct);

		// 在给定时间窗口内聚合 ActivityLogs，并关联用户以获取 Region
		var logs = _db.ActivityLogs.AsQueryable();
		if (fromInclusiveUtc.HasValue)
			logs = logs.Where(l => l.CreatedAt >= fromInclusiveUtc.Value);
		if (toExclusiveUtc.HasValue)
			logs = logs.Where(l => l.CreatedAt < toExclusiveUtc.Value);

		var aggregated = await (from l in logs
								join u in _db.ApplicationUsers on l.UserId equals u.Id
								where u.Region != null && u.Region != string.Empty
								group l by u.Region! into g
								select new { Region = g.Key, Emission = g.Sum(x => x.TotalEmission) })
							.ToListAsync(ct);

		var total = aggregated.Sum(x => x.Emission);
		var emissionByRegion = aggregated.ToDictionary(x => x.Region, x => x.Emission);

		var items = regionUsers.Select(g => new RegionStatItem
		{
			RegionCode = g.Region,
			RegionName = g.Region,
			CarbonReduced = emissionByRegion.TryGetValue(g.Region, out var v) ? v : 0m,
			UserCount = g.UserCount,
			ReductionRate = total > 0 ? Math.Round((emissionByRegion.TryGetValue(g.Region, out var vv) ? vv : 0m) * 100m / total, 2) : 0m
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

			var totalSaved = groups.Sum(x => x.TotalSaved);
			var items = groups.Select(g => new RegionStatItem
			{
				RegionCode = g.Region,
				RegionName = g.Region,
				CarbonReduced = g.TotalSaved,
				UserCount = g.UserCount,
				ReductionRate = totalSaved > 0 ? Math.Round(g.TotalSaved * 100m / totalSaved, 2) : 0m
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
	/// 类目占比（按 ActivityLog 的 CarbonReference.Category 汇总）。
	/// </summary>
	[HttpGet("analytics/category-share")]
	public async Task<ActionResult<IEnumerable<object>>> CategoryShare(CancellationToken ct)
	{
		// 在数据库端完成聚合，避免将整表加载到内存
		var grouped = await _db.ActivityLogs
			.Join(_db.CarbonReferences,
				l => l.CarbonReferenceId,
				c => c.Id,
				(l, c) => new { c.Category, l.TotalEmission })
			.GroupBy(x => x.Category)
			.Select(g => new { Category = g.Key, Total = g.Sum(x => x.TotalEmission) })
			.ToListAsync(ct);

		var total = grouped.Sum(x => x.Total);
		decimal AsPercent(decimal part) => total > 0 ? Math.Round(part * 100m / total, 2) : 0m;

		decimal GetTotal(CarbonCategory cat) => grouped.FirstOrDefault(x => x.Category == cat)?.Total ?? 0m;
		var food = GetTotal(CarbonCategory.Food);
		var transport = GetTotal(CarbonCategory.Transport);
		var utility = GetTotal(CarbonCategory.Utility);

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
}


