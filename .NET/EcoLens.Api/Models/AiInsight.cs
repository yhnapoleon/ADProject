using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Models;

[Table("AiInsights")]
public class AiInsight : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	[Required]
	[MaxLength(5000)]
	public string Content { get; set; } = string.Empty;

	[Required]
	public InsightType Type { get; set; }

	[Required]
	public bool IsRead { get; set; }

	public ApplicationUser? User { get; set; }
}

