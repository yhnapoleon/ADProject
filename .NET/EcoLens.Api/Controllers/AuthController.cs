using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Auth;
using EcoLens.Api.Models;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly Services.IAuthService _authService;

	public AuthController(ApplicationDbContext db, Services.IAuthService authService)
	{
		_db = db;
		_authService = authService;
	}

	/// <summary>
	/// 用户注册并返回认证 Token。
	/// </summary>
	[HttpPost("register")]
	[AllowAnonymous]
	public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
	{
		var exists = await _db.ApplicationUsers.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username, ct);
		if (exists)
		{
			return Conflict("User already exists.");
		}

		var user = new ApplicationUser
		{
			Username = dto.Username,
			Email = dto.Email,
			PasswordHash = PasswordHasher.Hash(dto.Password)
		};

		await _db.ApplicationUsers.AddAsync(user, ct);
		await _db.SaveChangesAsync(ct);

		var claims = new Dictionary<string, string>
		{
			{ ClaimTypes.NameIdentifier, user.Id.ToString() },
			{ ClaimTypes.Name, user.Username },
			{ ClaimTypes.Email, user.Email },
			{ ClaimTypes.Role, user.Role.ToString() }
		};

		var token = await _authService.GenerateTokenAsync(user.Id.ToString(), claims);

		var joinDaysReg = (int)Math.Max(0, (DateTime.UtcNow.Date - user.CreatedAt.Date).TotalDays);
		var response = new AuthResponseDto
		{
			Token = token,
			User = new UserSummaryDto
			{
				Id = user.Id.ToString(),
				Username = user.Username,
				Name = user.Username,
				Nickname = user.Username,
				Email = user.Email,
				Location = user.Region,
				BirthDate = user.BirthDate?.ToString("yyyy-MM-dd"),
				Role = user.Role,
				Avatar = user.AvatarUrl,
				AvatarUrl = user.AvatarUrl,
				TotalCarbonSaved = user.TotalCarbonSaved,
				CurrentPoints = user.CurrentPoints,
				PointsWeek = user.CurrentPoints,
				PointsMonth = user.CurrentPoints,
				PointsTotal = user.CurrentPoints,
				JoinDays = joinDaysReg
			}
		};

		return Ok(response);
	}

	/// <summary>
	/// 用户登录并返回认证 Token。
	/// </summary>
	[HttpPost("login")]
	[AllowAnonymous]
	public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
	{
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
		if (user is null)
		{
			return Unauthorized("Invalid credentials.");
		}

		if (!user.IsActive)
		{
			return Unauthorized("User is banned.");
		}

		var hash = PasswordHasher.Hash(dto.Password);
		if (!string.Equals(hash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
		{
			return Unauthorized("Invalid credentials.");
		}

		var claims = new Dictionary<string, string>
		{
			{ ClaimTypes.NameIdentifier, user.Id.ToString() },
			{ ClaimTypes.Name, user.Username },
			{ ClaimTypes.Email, user.Email },
			{ ClaimTypes.Role, user.Role.ToString() }
		};

		var token = await _authService.GenerateTokenAsync(user.Id.ToString(), claims);

		var joinDays = (int)Math.Max(0, (DateTime.UtcNow.Date - user.CreatedAt.Date).TotalDays);
		var response = new AuthResponseDto
		{
			Token = token,
			User = new UserSummaryDto
			{
				Id = user.Id.ToString(),
				Username = user.Username,
				Name = user.Username,
				Nickname = user.Username,
				Email = user.Email,
				Location = user.Region,
				BirthDate = user.BirthDate?.ToString("yyyy-MM-dd"),
				Role = user.Role,
				Avatar = user.AvatarUrl,
				AvatarUrl = user.AvatarUrl,
				TotalCarbonSaved = user.TotalCarbonSaved,
				CurrentPoints = user.CurrentPoints,
				PointsWeek = user.CurrentPoints,
				PointsMonth = user.CurrentPoints,
				PointsTotal = user.CurrentPoints,
				JoinDays = joinDays
			}
		};

		return Ok(response);
	}
}

