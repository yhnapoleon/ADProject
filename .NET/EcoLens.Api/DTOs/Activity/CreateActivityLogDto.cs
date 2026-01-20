using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.DTOs.Activity;

public class CreateActivityLogDto
{
	// 二选一：Base64 字符串或 IFormFile 文件上传（控制器中进行逻辑校验）
	public string? Base64Image { get; set; }
	public IFormFile? ImageFile { get; set; }

	[Required]
	[MaxLength(200)]
	public string Label { get; set; } = string.Empty;

	[Required]
	public decimal Quantity { get; set; }
}

