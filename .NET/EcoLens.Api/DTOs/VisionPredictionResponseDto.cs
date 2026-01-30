using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs;

public class VisionPredictionResponseDto
{
	[JsonPropertyName("label")]
	public string Label { get; set; } = string.Empty;

	[JsonPropertyName("confidence")]
	public double Confidence { get; set; }

	[JsonPropertyName("source_model")]
	public string SourceModel { get; set; } = string.Empty;
}


