using EcoLens.Api.DTOs.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".jpg", ".jpeg", ".png"
	};

	/// <summary>
	/// 上传图片到 wwwroot/uploads/{yyyyMMdd}/{guid}.ext 并返回相对 URL。
	/// </summary>
	[HttpPost("upload")]
	public async Task<ActionResult<UploadResponseDto>> Upload([FromForm] IFormFile file, CancellationToken ct)
	{
		if (file == null || file.Length == 0)
		{
			return BadRequest("No file uploaded.");
		}

		var ext = Path.GetExtension(file.FileName);
		if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
		{
			return BadRequest("Only .jpg/.jpeg/.png files are allowed.");
		}

		var date = DateTime.UtcNow.ToString("yyyyMMdd");
		var fileName = $"{Guid.NewGuid():N}{ext}";
		var relativeDir = Path.Combine("uploads", date);
		var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
		var absoluteDir = Path.Combine(webRoot, relativeDir);

		Directory.CreateDirectory(absoluteDir);

		var absolutePath = Path.Combine(absoluteDir, fileName);
		await using (var stream = System.IO.File.Create(absolutePath))
		{
			await file.CopyToAsync(stream, ct);
		}

		var url = "/" + Path.Combine(relativeDir, fileName).Replace("\\", "/");
		return Ok(new UploadResponseDto { Url = url });
	}
}


