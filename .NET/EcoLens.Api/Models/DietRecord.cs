using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("DietRecords")]
public class DietRecord : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 重量（kg）
	/// </summary>
	[Required]
	public double Amount { get; set; }

	/// <summary>
	/// 排放因子（kgCO2/kg）
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal EmissionFactor { get; set; }

	/// <summary>
	/// 碳排放值（kgCO2）
	/// </summary>
	[Column(TypeName = "decimal(18,4)")]
	public decimal Emission { get; set; }

	public ApplicationUser? User { get; set; }
}
