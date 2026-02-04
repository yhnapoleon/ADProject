using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.DTOs.User;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserProfileController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IMemoryCache _cache;

	public UserProfileController(ApplicationDbContext db, IMemoryCache cache)
	{
		_db = db;
		_cache = cache;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	/// <summary>
	/// 将 AvatarUrl 转换为 API URL（如果是 Base64，返回 /api/user/{userId}/avatar）
	/// </summary>
	private string? ConvertAvatarUrlToApiUrl(string? avatarUrl, int userId)
	{
		if (string.IsNullOrWhiteSpace(avatarUrl))
		{
			return null;
		}

		if (avatarUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
		{
			return Url.Action("GetAvatar", "UserProfile", new { userId }, Request.Scheme, Request.Host.Value);
		}

		if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
		{
			return avatarUrl;
		}

		return null;
	}

	public class UserProfileResponseDto
	{
		public string Id { get; set; } = string.Empty;          // string
		public string Name { get; set; } = string.Empty;        // name
		public string Nickname { get; set; } = string.Empty;    // nickname
		public string Email { get; set; } = string.Empty;       // email
		public string Location { get; set; } = string.Empty;    // LocationEnum -> 先返回字符串
		public string BirthDate { get; set; } = string.Empty;   // yyyy-MM-dd
		public string? Avatar { get; set; }                     // avatar url
		public int JoinDays { get; set; }                       // joinDays
		public int PointsTotal { get; set; }                    // pointsTotal
	}

	/// <summary>
	/// 获取当前登录用户的资料（含 Rank）。
	/// </summary>
	[HttpGet("profile")]
	public async Task<ActionResult<UserProfileResponseDto>> GetProfile(CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		var joinDays = (int)Math.Max(0, (DateTime.UtcNow.Date - user.CreatedAt.Date).TotalDays);

		var dto = new UserProfileResponseDto
		{
			Id = user.Id.ToString(),
			Name = user.Username,
			Nickname = user.Nickname ?? user.Username,
			Email = user.Email,
			Location = user.Region,
			BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
			Avatar = ConvertAvatarUrlToApiUrl(user.AvatarUrl, user.Id),
			JoinDays = joinDays,
			PointsTotal = user.CurrentPoints
		};

		return Ok(dto);
	}

	/// <summary>
	/// 更新当前登录用户资料：nickname、email、location、birthDate、avatar。
	/// </summary>
	[HttpPut("profile")]
	public async Task<ActionResult<UserProfileResponseDto>> UpdateProfile([FromBody] UpdateUserProfileDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		// Nickname -> ApplicationUser.Nickname（展示昵称；允许重复）
		if (dto.Nickname is not null)
		{
			user.Nickname = string.IsNullOrWhiteSpace(dto.Nickname) ? null : dto.Nickname.Trim();
		}

		if (dto.Avatar is not null)
		{
			user.AvatarUrl = dto.Avatar;
		}

		if (dto.Location is not null)
		{
			user.Region = dto.Location;
		}

		if (dto.Email is not null && !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
		{
			var emailExists = await _db.ApplicationUsers.AnyAsync(u => u.Email == dto.Email && u.Id != user.Id, ct);
			if (emailExists) return Conflict("Email already in use.");
			user.Email = dto.Email;
		}

		if (!string.IsNullOrWhiteSpace(dto.BirthDate))
		{
			if (!DateTime.TryParse(dto.BirthDate, out var bd))
			{
				return BadRequest("Invalid BirthDate format. Expected yyyy-MM-dd.");
			}
			user.BirthDate = bd.Date;
		}

		await _db.SaveChangesAsync(ct);

		// 如果头像被更新，清理内存缓存
		if (dto.Avatar is not null)
		{
			_cache.Remove($"Avatar_{userId.Value}");
		}

		var joinDays = (int)Math.Max(0, (DateTime.UtcNow.Date - user.CreatedAt.Date).TotalDays);

		var result = new UserProfileResponseDto
		{
			Id = user.Id.ToString(),
			Name = user.Username,
			Nickname = user.Nickname ?? user.Username,
			Email = user.Email,
			Location = user.Region,
			BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
			Avatar = ConvertAvatarUrlToApiUrl(user.AvatarUrl, user.Id),
			JoinDays = joinDays,
			PointsTotal = user.CurrentPoints
		};

		return Ok(result);
	}

	/// <summary>
	/// 修改密码：验证旧密码后更新为新密码。
	/// </summary>
	[HttpPost("change-password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
		{
			return BadRequest("OldPassword and NewPassword are required.");
		}

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		var oldHash = PasswordHasher.Hash(dto.OldPassword);
		if (!string.Equals(oldHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
		{
			return Unauthorized("Old password is incorrect.");
		}

		user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
		await _db.SaveChangesAsync(ct);

		return Ok();
	}

	/// <summary>
	/// 验证旧密码是否正确（仅校验，不修改）。
	/// </summary>
	[HttpPost("verify-password")]
	public async Task<ActionResult<VerifyPasswordResponseDto>> VerifyPassword([FromBody] VerifyPasswordRequestDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		if (string.IsNullOrWhiteSpace(dto.OldPassword))
		{
			return BadRequest("OldPassword is required.");
		}

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		var oldHash = PasswordHasher.Hash(dto.OldPassword);
		var valid = string.Equals(oldHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase);
		return Ok(new VerifyPasswordResponseDto { Valid = valid });
	}

	/// <summary>
	/// 获取用户头像图片（支持 Base64 和 URL）
	/// </summary>
	[HttpGet("{userId}/avatar")]
	[AllowAnonymous]
	[ResponseCache(Duration = 31536000, VaryByQueryKeys = new[] { "v" })]
	public async Task<IActionResult> GetAvatar([FromRoute] int userId, [FromQuery] long? v, CancellationToken ct)
	{
		var cacheKey = $"Avatar_{userId}";

		// 先尝试命中内存缓存
		if (_cache.TryGetValue<(byte[] ImageBytes, string ContentType)>(cacheKey, out var cached))
		{
			return File(cached.ImageBytes, cached.ContentType);
		}

		// 仅投影查询 AvatarUrl 字段，避免加载整个实体
		var avatarUrl = await _db.ApplicationUsers
			.AsNoTracking()
			.Where(u => u.Id == userId)
			.Select(u => u.AvatarUrl)
			.FirstOrDefaultAsync(ct);

		if (string.IsNullOrWhiteSpace(avatarUrl))
		{
			return NotFound();
		}

		// Base64 -> 解析并缓存
		if (avatarUrl.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
		{
			var parts = avatarUrl.Split(',');
			if (parts.Length != 2)
			{
				return BadRequest("Invalid avatar format.");
			}

			try
			{
				var meta = parts[0]; // data:image/png;base64
				var contentType = meta.Split(';')[0].Split(':')[1];
				var imageBytes = Convert.FromBase64String(parts[1]);

				_cache.Set(cacheKey, (imageBytes, contentType), new MemoryCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
				});

				return File(imageBytes, contentType);
			}
			catch (FormatException)
			{
				return BadRequest("Invalid avatar base64 content.");
			}
			catch
			{
				return BadRequest("Failed to parse avatar content.");
			}
		}

		// 普通 URL -> 直接重定向（无需缓存）
		if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
		{
			return Redirect(avatarUrl);
		}

		return NotFound();
	}
}


