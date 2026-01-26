using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.UtilityBill;

/// <summary>
/// 账单统计信息响应 DTO
/// </summary>
public class UtilityBillStatisticsDto
{
	/// <summary>
	/// 总记录数
	/// </summary>
	public int TotalRecords { get; set; }

	/// <summary>
	/// 总用电量（kWh）
	/// </summary>
	public decimal TotalElectricityUsage { get; set; }

	/// <summary>
	/// 总用水量（m³）
	/// </summary>
	public decimal TotalWaterUsage { get; set; }

	/// <summary>
	/// 总用气量（kWh 或 m³）
	/// </summary>
	public decimal TotalGasUsage { get; set; }

	/// <summary>
	/// 总碳排放（kg CO2）
	/// </summary>
	public decimal TotalCarbonEmission { get; set; }

	/// <summary>
	/// 按账单类型统计
	/// </summary>
	public List<BillTypeStatisticsDto> ByBillType { get; set; } = new();
}

/// <summary>
/// 按账单类型的统计信息
/// </summary>
public class BillTypeStatisticsDto
{
	/// <summary>
	/// 账单类型
	/// </summary>
	public UtilityBillType BillType { get; set; }

	/// <summary>
	/// 账单类型名称（中文）
	/// </summary>
	public string BillTypeName { get; set; } = string.Empty;

	/// <summary>
	/// 记录数
	/// </summary>
	public int RecordCount { get; set; }

	/// <summary>
	/// 总用电量（kWh）
	/// </summary>
	public decimal TotalElectricityUsage { get; set; }

	/// <summary>
	/// 总用水量（m³）
	/// </summary>
	public decimal TotalWaterUsage { get; set; }

	/// <summary>
	/// 总用气量（kWh 或 m³）
	/// </summary>
	public decimal TotalGasUsage { get; set; }

	/// <summary>
	/// 总碳排放（kg CO2）
	/// </summary>
	public decimal TotalCarbonEmission { get; set; }
}
