using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("UtilityBills")]
public class UtilityBill : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	// 可选：保留原有的月份标识
	[MaxLength(7)]
	public string YearMonth { get; set; } = string.Empty;

	// 新增：账单类型与周期
	public EcoLens.Api.Models.Enums.UtilityBillType BillType { get; set; }

	[Required]
	public DateTime BillPeriodStart { get; set; }

	[Required]
	public DateTime BillPeriodEnd { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal? ElectricityUsage { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal ElectricityCost { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal? WaterUsage { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal WaterCost { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal? GasUsage { get; set; }

	[Column(TypeName = "decimal(18,2)")]
	public decimal GasCost { get; set; }

	// 新增：碳排放聚合结果
	[Column(TypeName = "decimal(18,4)")]
	public decimal ElectricityCarbonEmission { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal WaterCarbonEmission { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal GasCarbonEmission { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal TotalCarbonEmission { get; set; }

	// 新增：输入方式与 OCR 元数据
	public EcoLens.Api.Models.Enums.InputMethod InputMethod { get; set; }

	[Column(TypeName = "decimal(18,4)")]
	public decimal? OcrConfidence { get; set; }

	[Column(TypeName = "nvarchar(max)")]
	public string? OcrRawText { get; set; }

	[MaxLength(1000)]
	public string? Notes { get; set; }

	public ApplicationUser? User { get; set; }
}

