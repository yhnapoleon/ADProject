using System.ComponentModel.DataAnnotations;

namespace EcoLens.Api.DTOs.Food;

public class FoodCalculateByNameRequest
{
	[Required]
	[MaxLength(200)]
	public string FoodName { get; set; } = string.Empty;

	/// <summary>
	/// 输入的份量数值，例如 200
	/// </summary>
	[Range(0, double.MaxValue)]
	public double Quantity { get; set; }

	/// <summary>
	/// 输入的份量单位，支持：g, kg, portion(份/份量/serving)
	/// </summary>
	[Required]
	[MaxLength(50)]
	public string Unit { get; set; } = "g";
}


