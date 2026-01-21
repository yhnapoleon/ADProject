using System;

namespace EcoLens.Api.DTOs.Activity;

public class ActivityLogResponseDto
{
	public int Id { get; set; }
	public string Label { get; set; } = string.Empty;
	public decimal Quantity { get; set; }
	public decimal TotalEmission { get; set; }
	public string? ImageUrl { get; set; }
	public DateTime CreatedAt { get; set; }
}

