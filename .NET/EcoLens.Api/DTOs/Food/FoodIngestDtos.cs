using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.Food;

public class FoodIngestByNameRequest
{
	[JsonPropertyName("name")]
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 数量（配合 unit，可为 g/kg/portion）
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

	[JsonPropertyName("note")]
	[MaxLength(500)]
	public string? Note { get; set; }
}

