namespace EcoLens.Api.DTOs.Food;

public class FoodRecognizeResponseDto
{
	public string FoodName { get; set; } = string.Empty;
	public double Confidence { get; set; }
	public string SourceModel { get; set; } = string.Empty;
}


