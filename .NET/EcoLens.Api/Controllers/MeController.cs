using System;
using System.IO;
using System.Linq;
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

    // 验证文件大小（限制在 500KB 以内）
    const long maxFileSize = 500 * 1024; // 500KB
    if (file.Length > maxFileSize)
    {
      return BadRequest("File size exceeds the maximum limit of 500KB. Please upload a smaller image.");
    }

    // 验证文件类型
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
    {
      return BadRequest("Invalid file type. Only JPG, PNG, GIF, and WebP images are allowed.");
    }

    // 读取文件字节数组
    byte[] fileBytes;
    await using (var memoryStream = new MemoryStream())
    {
      await file.CopyToAsync(memoryStream, ct);
      fileBytes = memoryStream.ToArray();
    }

    // 转换为 Base64 字符串
    var base64String = Convert.ToBase64String(fileBytes);

    // 根据文件扩展名确定 MIME 类型
    var mimeType = ext switch
    {
      ".jpg" or ".jpeg" => "image/jpeg",
      ".png" => "image/png",
      ".gif" => "image/gif",
      ".webp" => "image/webp",
      _ => "image/jpeg"
    };

    // 拼接完整的 data URI 格式：data:image/{type};base64,{base64字符串}
    var dataUri = $"data:{mimeType};base64,{base64String}";

    // 保存到数据库
    u.AvatarUrl = dataUri;
    await _db.SaveChangesAsync(ct);

    return Ok(new { avatar = dataUri, avatarUrl = dataUri });
  }
}

