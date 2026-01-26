using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly Services.IAuthService _authService;

	public AdminAuthController(ApplicationDbContext db, Services.IAuthService authService)
	{
		_db = db;
		_authService = authService;
	}

	public class AdminLoginRequest
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class AdminLoginResponse
	{
		public string AccessToken { get; set; } = string.Empty;
		public int ExpiresIn { get; set; } = 3600;
		public object Admin { get; set; } = new();
	}

	[HttpPost("login")]
	[AllowAnonymous]
	public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest req, CancellationToken ct)
	{
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Username == req.Username, ct);
		if (user is null) return Unauthorized();
		if (!user.IsActive) return Unauthorized("User is banned.");
		if (user.Role != UserRole.Admin) return Unauthorized("Not an admin.");
		var hash = Utilities.PasswordHasher.Hash(req.Password);
		if (!string.Equals(hash, user.PasswordHash, StringComparison.OrdinalIgnoreCase)) return Unauthorized();

		var claims = new Dictionary<string, string>
		{
			{ ClaimTypes.NameIdentifier, user.Id.ToString() },
			{ ClaimTypes.Name, user.Username },
			{ ClaimTypes.Email, user.Email },
			{ ClaimTypes.Role, user.Role.ToString() }
		};
		var token = await _authService.GenerateTokenAsync(user.Id.ToString(), claims);

		return Ok(new AdminLoginResponse
		{
			AccessToken = token,
			ExpiresIn = 3600,
			Admin = new
			{
				id = user.Id.ToString(),
				username = user.Username,
				roles = new[] { "admin" }
			}
		});
	}

	/// <summary>
	/// 路由别名：/api/admin/login（与部分前端约定对齐）。
	/// </summary>
	[HttpPost("/api/admin/login")]
	[AllowAnonymous]
	public Task<ActionResult<AdminLoginResponse>> LoginAlias([FromBody] AdminLoginRequest req, CancellationToken ct)
		=> Login(req, ct);
}

