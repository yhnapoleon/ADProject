using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.DTOs.Food;

/// <summary>
/// 组合流程：图片 + 分量
/// </summary>
public class FoodCalculateFromImageRequest
{
	/// <summary>
	/// 图片文件（字段名：file）
	/// </summary>
	[Required]
	public IFormFile File { get; set; } = null!;

	/// <summary>
	/// 分量
	/// </summary>
	[Required]
	public double Quantity { get; set; }

	/// <summary>
	/// 单位（g、kg、portion）
	/// </summary>
	[Required]
	public string Unit { get; set; } = "g";
}

