using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("DietTemplates")]
public class DietTemplate : BaseEntity
{
	[Required]
	public Guid UserId { get; set; }

	[Required]
	[MaxLength(100)]
	public string TemplateName { get; set; } = string.Empty;

	public ICollection<DietTemplateItem> Items { get; set; } = new List<DietTemplateItem>();
}

