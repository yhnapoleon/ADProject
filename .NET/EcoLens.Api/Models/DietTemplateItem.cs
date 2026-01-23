using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("DietTemplateItems")]
public class DietTemplateItem : BaseEntity
{
	[Required]
	public int DietTemplateId { get; set; }

	[Required]
	public int FoodId { get; set; }

	[Required]
	public double Quantity { get; set; }

	[Required]
	[MaxLength(50)]
	public string Unit { get; set; } = string.Empty;

	[ForeignKey(nameof(DietTemplateId))]
	public DietTemplate? DietTemplate { get; set; }
}

