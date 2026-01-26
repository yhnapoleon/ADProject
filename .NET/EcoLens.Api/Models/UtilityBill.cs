using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Models;

/// <summary>
/// 水电账单模型
/// </summary>
[Table("UtilityBills")]
public class UtilityBill : BaseEntity
{
	/// <summary>
	/// 用户ID（外键）
	/// </summary>
	[Required]
	public int UserId { get; set; }

	/// <summary>
	/// 账单类型
	/// </summary>
	[Required]
	public UtilityBillType BillType { get; set; }

	/// <summary>
	/// 账单周期开始日期
	/// </summary>
	[Required]
	public DateTime BillPeriodStart { get; set; }

	/// <summary>
	/// 账单周期结束日期
	/// </summary>
	[Required]
	public DateTime BillPeriodEnd { get; set; }

	/// <summary>
	/// 用电量（kWh）
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal? ElectricityUsage { get; set; }

	/// <summary>
	/// 用水量（m³）
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal? WaterUsage { get; set; }

	/// <summary>
	/// 用气量（kWh 或 m³）
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal? GasUsage { get; set; }

	/// <summary>
	/// 电力碳排放（kg CO2）
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal ElectricityCarbonEmission { get; set; }

	/// <summary>
	/// 水碳排放（kg CO2）
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal WaterCarbonEmission { get; set; }

	/// <summary>
	/// 燃气碳排放（kg CO2）
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal GasCarbonEmission { get; set; }

	/// <summary>
	/// 总碳排放（kg CO2）
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal TotalCarbonEmission { get; set; }

	/// <summary>
	/// OCR识别的原始文本
	/// </summary>
	[Column(TypeName = "nvarchar(max)")]
	public string? OcrRawText { get; set; }

	/// <summary>
	/// OCR识别置信度（0-1）
	/// </summary>
	[Column(TypeName = "decimal(5,4)")]
	public decimal? OcrConfidence { get; set; }

	/// <summary>
	/// 输入方式
	/// </summary>
	[Required]
	public InputMethod InputMethod { get; set; }

	// 导航属性
	/// <summary>
	/// 关联的用户
	/// </summary>
	public ApplicationUser? User { get; set; }
}
