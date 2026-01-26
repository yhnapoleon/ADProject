using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("UtilityBills")]
public class UtilityBill : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	/// <summary>
	/// 账单月份，格式为 YYYY-MM（例如 2026-01）
	/// </summary>
	[Required]
	[MaxLength(7)]
	public string YearMonth { get; set; } = string.Empty;

	[Column(TypeName = "decimal(18,4)")]
	public decimal ElectricityUsage { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal ElectricityCost { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal WaterUsage { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal WaterCost { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal GasUsage { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal GasCost { get; set; }

	public ApplicationUser? User { get; set; }
}

