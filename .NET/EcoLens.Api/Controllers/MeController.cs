using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
  private readonly ApplicationDbContext _db;
  private readonly IWebHostEnvironment _env;

  public MeController(ApplicationDbContext db, IWebHostEnvironment env)
  {
    _db = db;
    _env = env;
  }

  private int? GetUserId()
  {
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    return int.TryParse(id, out var uid) ? uid : null;
  }

  public class MeDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? BirthDate { get; set; } // YYYY-MM-DD
    public string? Avatar { get; set; }
    public int PointsWeek { get; set; }
    public int PointsMonth { get; set; }
    public int PointsTotal { get; set; }
    public int JoinDays { get; set; }
  }

  public class UpdateMeRequest
  {
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Location { get; set; }
    public string? BirthDate { get; set; } // YYYY-MM-DD
    public string? Password { get; set; }
  }

  [HttpGet]
  [HttpGet("/api/user/me")]
  public async Task<ActionResult<MeDto>> Get(CancellationToken ct)
  {
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var u = await _db.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
    if (u is null) return NotFound();

    var days = (int)Math.Max(0, (DateTime.UtcNow.Date - u.CreatedAt.Date).TotalDays);
    var dto = new MeDto
    {
      Id = u.Id.ToString(),
      Name = u.Username,
      Nickname = u.Nickname ?? u.Username,
      Email = u.Email,
      Location = u.Region,
      BirthDate = u.BirthDate.ToString("yyyy-MM-dd"),
      Avatar = u.AvatarUrl,
      PointsWeek = u.CurrentPoints, // 简化：复用 CurrentPoints
      PointsMonth = u.CurrentPoints,
      PointsTotal = u.CurrentPoints,
      JoinDays = days
    };
    return Ok(dto);
  }

  [HttpPut]
  [HttpPut("/api/user/me")]
  public async Task<ActionResult<MeDto>> Update([FromBody] UpdateMeRequest req, CancellationToken ct)
  {
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var u = await _db.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
    if (u is null) return NotFound();

    if (!string.IsNullOrWhiteSpace(req.Email) && !string.Equals(req.Email, u.Email, StringComparison.OrdinalIgnoreCase))
    {
      var exists = await _db.ApplicationUsers.AnyAsync(x => x.Email == req.Email && x.Id != u.Id, ct);
      if (exists) return Conflict("Email already in use.");
      u.Email = req.Email;
    }
    if (req.Nickname is not null)
    {
      u.Nickname = string.IsNullOrWhiteSpace(req.Nickname) ? null : req.Nickname.Trim();
    }
    if (req.Location is not null) u.Region = req.Location;
    if (!string.IsNullOrWhiteSpace(req.BirthDate) && DateTime.TryParse(req.BirthDate, out var bd))
    {
      u.BirthDate = bd.Date;
    }
    if (!string.IsNullOrWhiteSpace(req.Password))
    {
      u.PasswordHash = Utilities.PasswordHasher.Hash(req.Password);
    }
    await _db.SaveChangesAsync(ct);
    return await Get(ct);
  }

  public class ChangePasswordRequest
  {
    public string Password { get; set; } = string.Empty;
  }

  [HttpPut("password")]
  public async Task<ActionResult<object>> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
  {
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var u = await _db.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
    if (u is null) return NotFound();

    if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest("Password required.");
    u.PasswordHash = Utilities.PasswordHasher.Hash(req.Password);
    await _db.SaveChangesAsync(ct);
    return Ok(new { updated = true });
  }

  [HttpPut("avatar")]
  [HttpPut("/api/user/avatar")]
  [Consumes("multipart/form-data")]
  public async Task<ActionResult<object>> UpdateAvatar([FromForm] IFormFile file, CancellationToken ct)
  {
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var u = await _db.ApplicationUsers.FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
    if (u is null) return NotFound();
    if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

    var oldPublicUrl = u.AvatarUrl;

    // 简易落盘到 wwwroot/uploads/avatars
    var uploadsDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");
    Directory.CreateDirectory(uploadsDir);
    var ext = Path.GetExtension(file.FileName);
    var name = $"{Guid.NewGuid()}{ext}";
    var path = Path.Combine(uploadsDir, name);
    await using (var fs = System.IO.File.Create(path))
    {
      await file.CopyToAsync(fs, ct);
    }
    var publicUrl = $"/uploads/avatars/{name}";
    u.AvatarUrl = publicUrl;
    await _db.SaveChangesAsync(ct);

    // 尝试删除旧头像文件（仅限当前 avatars 目录内）
    if (!string.IsNullOrWhiteSpace(oldPublicUrl))
    {
      try
      {
        var oldFileName = Path.GetFileName(oldPublicUrl);
        if (!string.IsNullOrWhiteSpace(oldFileName))
        {
          var oldPath = Path.Combine(uploadsDir, oldFileName);
          if (System.IO.File.Exists(oldPath))
          {
            System.IO.File.Delete(oldPath);
          }
        }
      }
      catch
      {
        // 忽略删除失败（例如文件被占用或不存在）
      }
    }
    return Ok(new { avatar = publicUrl, avatarUrl = publicUrl });
  }
}

