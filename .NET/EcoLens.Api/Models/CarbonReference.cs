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

	[Required]
	[MaxLength(50)]
	public string Unit { get; set; } = string.Empty;
}

