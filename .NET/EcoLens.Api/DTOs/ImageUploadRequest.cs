using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.DTOs;

/// <summary>
/// 图片上传请求 DTO
/// </summary>
public class ImageUploadRequest
{
	/// <summary>
	/// 图片文件
	/// </summary>
	[Required(ErrorMessage = "Image is required")]
	public IFormFile Image { get; set; } = null!;
}

