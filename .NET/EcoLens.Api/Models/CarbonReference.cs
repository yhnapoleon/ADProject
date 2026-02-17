using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Models;

[Table("CarbonReferences")]
public class CarbonReference : BaseEntity
{
	[Required]
	[MaxLength(100)]
	public string LabelName { get; set; } = string.Empty;

	[Required]
	public CarbonCategory Category { get; set; }

	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal Co2Factor { get; set; }

	/// <summary>
	/// 兼容文档/接口描述中的命名：CarbonEmission。
	/// 注意：数据库实际字段为 Co2Factor（单位见 Unit）。
	/// </summary>
	[NotMapped]
	public decimal CarbonEmission => Co2Factor;

	[Required]
	[MaxLength(50)]
	public string Unit { get; set; } = string.Empty;

	// 可选：用于 Utility 等按地区区分的因子
	[MaxLength(100)]
	public string? Region { get; set; }

	[Required]
	[MaxLength(50)]
	public string Source { get; set; } = "Local"; // 数据来源：Local, Climatiq, etc.

	[MaxLength(200)]
	public string? ClimatiqActivityId { get; set; } // 如果是 Climatiq 来源，存储其 Activity ID
}
