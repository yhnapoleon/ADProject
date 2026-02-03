using System.ComponentModel.DataAnnotations;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.UtilityBill;

/// <summary>
/// 手动创建账单的请求 DTO
/// </summary>
public class CreateUtilityBillManuallyDto
{
	/// <summary>
	/// 账单类型
	/// </summary>
	/// <remarks>
	/// 枚举值说明：
	/// - 0: 电费（Electricity）
	/// - 1: 水费（Water）
	/// - 2: 燃气费（Gas）
	/// - 3: 综合账单（Combined）
	/// </remarks>
	[Required(ErrorMessage = "Bill type is required")]
	public UtilityBillType BillType { get; set; }

	/// <summary>
	/// 账单周期开始日期
	/// </summary>
	[Required(ErrorMessage = "Bill period start date is required")]
	public DateTime BillPeriodStart { get; set; }

	/// <summary>
	/// 账单周期结束日期
	/// </summary>
	[Required(ErrorMessage = "Bill period end date is required")]
	public DateTime BillPeriodEnd { get; set; }

	/// <summary>
	/// 用电量（kWh），可选
	/// </summary>
	[Range(0, double.MaxValue, ErrorMessage = "Electricity usage must be greater than or equal to 0")]
	public decimal? ElectricityUsage { get; set; }

	/// <summary>
	/// 用水量（m³），可选
	/// </summary>
	[Range(0, double.MaxValue, ErrorMessage = "Water usage must be greater than or equal to 0")]
	public decimal? WaterUsage { get; set; }

	/// <summary>
	/// 用气量（kWh 或 m³），可选
	/// </summary>
	[Range(0, double.MaxValue, ErrorMessage = "Gas usage must be greater than or equal to 0")]
	public decimal? GasUsage { get; set; }

	/// <summary>
	/// 备注，可选
	/// </summary>
	[MaxLength(1000, ErrorMessage = "Notes must not exceed 1000 characters")]
	public string? Notes { get; set; }
}
