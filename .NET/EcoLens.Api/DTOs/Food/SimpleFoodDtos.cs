using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.Food;

public class CalculateFoodRequest
{
	[JsonPropertyName("name")]
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 数量（克，g）。后端 /1000 转为 kg 再计算排放。
	/// </summary>
	[JsonPropertyName("amount")]
	[Range(0, double.MaxValue)]
	public double Amount { get; set; }
}

public class FoodCalcResponse
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("amount")]
	public double Amount { get; set; }

	/// <summary>
	/// 排放因子（kgCO2/kg）
	/// </summary>
	[JsonPropertyName("emission_factor")]
	public decimal EmissionFactor { get; set; }

	/// <summary>
	/// 总排放量（kgCO2）
	/// </summary>
	[JsonPropertyName("emission")]
	public decimal Emission { get; set; }
}

public class AddFoodRequest
{
	[JsonPropertyName("name")]
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 数量（克，g）。后端统一 /1000 转为 kg 再计算碳排放并存储。
	/// </summary>
	[JsonPropertyName("amount")]
	[Range(0, double.MaxValue)]
	public double Amount { get; set; }

	[JsonPropertyName("emission_factor")]
	public decimal EmissionFactor { get; set; }

	[JsonPropertyName("emission")]
	public decimal Emission { get; set; }

	[JsonPropertyName("note")]
	[MaxLength(500)]
	public string? Note { get; set; }
}

public class AddFoodResponse
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }
}


