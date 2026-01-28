using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.DTOs.User;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserProfileController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public UserProfileController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
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
			Avatar = user.AvatarUrl,
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

		var joinDays = (int)Math.Max(0, (DateTime.UtcNow.Date - user.CreatedAt.Date).TotalDays);

		var result = new UserProfileResponseDto
		{
			Id = user.Id.ToString(),
			Name = user.Username,
			Nickname = user.Nickname ?? user.Username,
			Email = user.Email,
			Location = user.Region,
			BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
			Avatar = user.AvatarUrl,
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
}


