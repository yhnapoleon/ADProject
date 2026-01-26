using System;

namespace EcoLens.Api.DTOs.Activity;

public class ActivityLogResponseDto
{
	public int Id { get; set; }
	public string Label { get; set; } = string.Empty;
	public decimal Quantity { get; set; }
	public decimal TotalEmission { get; set; }
	public string? ImageUrl { get; set; }
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// 记录类别（Food/Transport/Utility）
	/// </summary>
	public EcoLens.Api.Models.Enums.CarbonCategory Category { get; set; }

	/// <summary>
	/// 显示给用户的排放单位（固定为 kg CO2e）
	/// </summary>
	public string EmissionUnit { get; set; } = "kg CO2e";

	/// <summary>
	/// 因子单位（例如 kgCO2/kWh, kgCO2/km）
	/// </summary>
	public string FactorUnit { get; set; } = string.Empty;

	/// <summary>
	/// 展示给用户的描述（优先使用识别标签，其次使用参考标签）
	/// </summary>
	public string Description { get; set; } = string.Empty;
}

