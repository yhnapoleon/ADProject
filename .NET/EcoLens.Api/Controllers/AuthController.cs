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
	private readonly Services.ISensitiveWordService _sensitiveWordService;

	public AuthController(ApplicationDbContext db, Services.IAuthService authService, Services.ISensitiveWordService sensitiveWordService)
	{
		_db = db;
		_authService = authService;
		_sensitiveWordService = sensitiveWordService;
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
			// 使用 Url.Action 生成 API 端点 URL
			return Url.Action("GetAvatar", "UserProfile", new { userId }, Request.Scheme, Request.Host.Value);
		}

		if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
		{
			return avatarUrl;
		}

		return null;
	}

	/// <summary>
	/// 用户注册并返回认证 Token。
	/// </summary>
	[HttpPost("register")]
	[AllowAnonymous]
	public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
	{
		// 检测用户名是否包含敏感词
		var sensitiveWord = _sensitiveWordService.ContainsSensitiveWord(dto.Username);
		if (sensitiveWord != null)
		{
			return BadRequest("Username contains inappropriate content. Registration denied.");
		}

		var exists = await _db.ApplicationUsers.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username, ct);
		if (exists)
		{
			return Conflict("User already exists.");
		}

		var user = new ApplicationUser
		{
			Username = dto.Username,
			Nickname = dto.Username, // 默认展示昵称 = username（可后续修改）
			Email = dto.Email,
			PasswordHash = PasswordHasher.Hash(dto.Password),
			Region = dto.Region,
			BirthDate = dto.BirthDate
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
		var avatarUrl = ConvertAvatarUrlToApiUrl(user.AvatarUrl, user.Id);
		var response = new AuthResponseDto
		{
			Token = token,
			User = new UserSummaryDto
			{
				Id = user.Id.ToString(),
				Username = user.Username,
				Name = user.Username,
				Nickname = user.Nickname ?? user.Username,
				Email = user.Email,
				Location = user.Region,
				BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
				Role = user.Role,
				Avatar = avatarUrl,
				AvatarUrl = avatarUrl,
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
		var email = dto.Email?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(email))
			return BadRequest("Email is required.");

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim() == email, ct);
		if (user is null)
		{
			return NotFound("No account found with this email.");
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
		var avatarUrl = ConvertAvatarUrlToApiUrl(user.AvatarUrl, user.Id);
		var response = new AuthResponseDto
		{
			Token = token,
			User = new UserSummaryDto
			{
				Id = user.Id.ToString(),
				Username = user.Username,
				Name = user.Username,
				Nickname = user.Nickname ?? user.Username,
				Email = user.Email,
				Location = user.Region,
				BirthDate = user.BirthDate.ToString("yyyy-MM-dd"),
				Role = user.Role,
				Avatar = avatarUrl,
				AvatarUrl = avatarUrl,
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

