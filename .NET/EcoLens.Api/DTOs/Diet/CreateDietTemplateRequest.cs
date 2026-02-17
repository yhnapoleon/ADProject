using System.Collections.Generic;

namespace EcoLens.Api.DTOs.Diet;

public class CreateDietTemplateRequest
{
	public string TemplateName { get; set; } = string.Empty;
	public List<CreateDietTemplateItemRequest> Items { get; set; } = new();
}

public class CreateDietTemplateItemRequest
{
	public int FoodId { get; set; }
	public double Quantity { get; set; }
	public string Unit { get; set; } = string.Empty;
}

