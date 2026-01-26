using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using EcoLens.Api.Models.Enums;

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

	[Required]
	[MaxLength(50)]
	public string Unit { get; set; } = string.Empty;

	// 可选：当为 Utility 时，服务端将按用户 Region 优先匹配
	public CarbonCategory? Category { get; set; }
}

