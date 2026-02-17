using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.DTOs.Food;

/// <summary>
/// 仅上传图片的表单请求
/// </summary>
public class FoodImageUploadDto
{
	/// <summary>
	/// 图片文件（字段名：file）
	/// </summary>
	[Required]
	public IFormFile File { get; set; } = null!;
}

