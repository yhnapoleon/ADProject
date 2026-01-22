namespace EcoLens.Api.DTOs;

public class VisionPredictionResponseDto
{
	public string Label { get; set; } = string.Empty;
	public double Confidence { get; set; }
	public string SourceModel { get; set; } = string.Empty;
}


