using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("ActivityLogs")]
public class ActivityLog : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	[Required]
	public int CarbonReferenceId { get; set; }

	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal Quantity { get; set; }

	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal TotalEmission { get; set; }

	[MaxLength(1024)]
	public string? ImageUrl { get; set; }

	[MaxLength(200)]
	public string? DetectedLabel { get; set; }

	public ApplicationUser? User { get; set; }
	public CarbonReference? CarbonReference { get; set; }
}

