using System;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.UtilityBill;

/// <summary>
/// 账单响应 DTO
/// </summary>
public class UtilityBillResponseDto
{
	/// <summary>
	/// 账单ID
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// 账单类型
	/// </summary>
	public UtilityBillType BillType { get; set; }

	/// <summary>
	/// 账单类型名称（中文）
	/// </summary>
	public string BillTypeName { get; set; } = string.Empty;

	/// <summary>
	/// 账单周期开始日期
	/// </summary>
	public DateTime BillPeriodStart { get; set; }

	/// <summary>
	/// 账单周期结束日期
	/// </summary>
	public DateTime BillPeriodEnd { get; set; }

	/// <summary>
	/// 用电量（kWh）
	/// </summary>
	public decimal? ElectricityUsage { get; set; }

	/// <summary>
	/// 用水量（m³）
	/// </summary>
	public decimal? WaterUsage { get; set; }

	/// <summary>
	/// 电力碳排放（kg CO2）
	/// </summary>
	public decimal ElectricityCarbonEmission { get; set; }

	/// <summary>
	/// 水碳排放（kg CO2）
	/// </summary>
	public decimal WaterCarbonEmission { get; set; }

	/// <summary>
	/// 总碳排放（kg CO2，仅电+水，不含煤气）
	/// </summary>
	public decimal TotalCarbonEmission { get; set; }

	/// <summary>
	/// 输入方式
	/// </summary>
	public InputMethod InputMethod { get; set; }

	/// <summary>
	/// 输入方式名称（中文）
	/// </summary>
	public string InputMethodName { get; set; } = string.Empty;

	/// <summary>
	/// OCR识别置信度（0-1，仅自动识别时有值）
	/// </summary>
	public decimal? OcrConfidence { get; set; }
	
	/// <summary>
	/// OCR识别的原始文本（仅用于调试，生产环境可移除）
	/// </summary>
	public string? OcrRawText { get; set; }

	/// <summary>
	/// 备注
	/// </summary>
	public string? Notes { get; set; }

	/// <summary>
	/// 创建时间
	/// </summary>
	public DateTime CreatedAt { get; set; }
}
