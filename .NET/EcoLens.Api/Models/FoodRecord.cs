using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("FoodRecords")]
public class FoodRecord : BaseEntity
{
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 食物数量，单位：kg
	/// </summary>
	[Required]
	public double Amount { get; set; }

	/// <summary>
	/// 排放因子，单位：kgCO2/kg
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal EmissionFactor { get; set; }

	/// <summary>
	/// 总排放量，单位：kgCO2
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal Emission { get; set; }

	[MaxLength(500)]
	public string? Note { get; set; }
}


