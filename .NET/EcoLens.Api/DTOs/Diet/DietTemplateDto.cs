using System;
using System.Collections.Generic;

namespace EcoLens.Api.DTOs.Diet;

public class DietTemplateDto
{
	public int Id { get; set; }
	public Guid UserId { get; set; }
	public string TemplateName { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public List<DietTemplateItemDto> Items { get; set; } = new();
}

public class DietTemplateItemDto
{
	public int Id { get; set; }
	public int FoodId { get; set; }
	public double Quantity { get; set; }
	public string Unit { get; set; } = string.Empty;
}

