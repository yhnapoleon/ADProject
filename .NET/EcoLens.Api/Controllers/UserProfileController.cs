using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
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
		public string Email { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
		public string? Region { get; set; }
		public decimal TotalCarbonSaved { get; set; }
		public int CurrentPoints { get; set; }
		public int Rank { get; set; }
		public UserRole Role { get; set; }
	}

	public class UpdateUserProfileDto
	{
		public string? AvatarUrl { get; set; }
		public string? Username { get; set; }
		public string? Region { get; set; }
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

		// Username 唯一性检查（当修改时）
		if (!string.IsNullOrWhiteSpace(dto.Username) && !dto.Username.Equals(user.Username, StringComparison.Ordinal))
		{
			var exists = await _db.ApplicationUsers
				.AnyAsync(u => u.Username == dto.Username && u.Id != user.Id, ct);
			if (exists) return Conflict("Username already in use.");

			user.Username = dto.Username;
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
}


