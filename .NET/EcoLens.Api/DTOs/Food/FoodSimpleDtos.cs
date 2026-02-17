using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.Food;

public class FoodCalculateSimpleRequest
{
	[JsonPropertyName("name")]
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 分量数值（配合 unit 使用）
	/// </summary>
	[JsonPropertyName("quantity")]
	[Range(0, double.MaxValue)]
	public double Quantity { get; set; }

	/// <summary>
	/// 单位：g / kg / portion
	/// </summary>
	[JsonPropertyName("unit")]
	[Required]
	[MaxLength(50)]
	public string Unit { get; set; } = "g";
}

public class FoodSimpleCalcResponse
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("quantity")]
	public double Quantity { get; set; }

	/// <summary>
	/// 碳排放因子（数值）
	/// </summary>
	[JsonPropertyName("emissionFactor")]
	public decimal EmissionFactor { get; set; }

	/// <summary>
	/// 碳排放总量（kgCO2）
	/// </summary>
	[JsonPropertyName("emission")]
	public decimal Emission { get; set; }
}
