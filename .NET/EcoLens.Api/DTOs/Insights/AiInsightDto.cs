using System;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Insights;

public class AiInsightDto
{
	public int Id { get; set; }
	public string Content { get; set; } = string.Empty;
	public InsightType Type { get; set; }
	public bool IsRead { get; set; }
	public DateTime CreatedAt { get; set; }
}

