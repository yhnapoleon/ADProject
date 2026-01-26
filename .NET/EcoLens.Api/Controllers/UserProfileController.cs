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
		public int Id { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Nickname { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
		public string? Region { get; set; }
		public decimal TotalCarbonSaved { get; set; }
		public int CurrentPoints { get; set; }
		public int Rank { get; set; }
		public UserRole Role { get; set; }
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

		var rank = await _db.ApplicationUsers
			.CountAsync(u => u.TotalCarbonSaved > user.TotalCarbonSaved, ct) + 1;

		var dto = new UserProfileResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			Nickname = user.Username,
			Email = user.Email,
			AvatarUrl = user.AvatarUrl,
			Region = user.Region,
			TotalCarbonSaved = user.TotalCarbonSaved,
			CurrentPoints = user.CurrentPoints,
			Rank = rank,
			Role = user.Role
		};

		return Ok(dto);
	}

	/// <summary>
	/// 更新当前登录用户的头像、用户名与地区。
	/// </summary>
	[HttpPut("profile")]
	public async Task<ActionResult<UserProfileResponseDto>> UpdateProfile([FromBody] UpdateUserProfileDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		if (user is null) return NotFound();

		// Nickname -> Username，唯一性检查（当修改时）
		if (!string.IsNullOrWhiteSpace(dto.Nickname) && !dto.Nickname.Equals(user.Username, StringComparison.Ordinal))
		{
			var exists = await _db.ApplicationUsers
				.AnyAsync(u => u.Username == dto.Nickname && u.Id != user.Id, ct);
			if (exists) return Conflict("Username already in use.");

			user.Username = dto.Nickname;
		}

		if (dto.AvatarUrl is not null)
		{
			user.AvatarUrl = dto.AvatarUrl;
		}

		if (dto.Region is not null)
		{
			user.Region = dto.Region;
		}

		await _db.SaveChangesAsync(ct);

		var rank = await _db.ApplicationUsers
			.CountAsync(u => u.TotalCarbonSaved > user.TotalCarbonSaved, ct) + 1;

		var result = new UserProfileResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			Nickname = user.Username,
			Email = user.Email,
			AvatarUrl = user.AvatarUrl,
			Region = user.Region,
			TotalCarbonSaved = user.TotalCarbonSaved,
			CurrentPoints = user.CurrentPoints,
			Rank = rank,
			Role = user.Role
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


