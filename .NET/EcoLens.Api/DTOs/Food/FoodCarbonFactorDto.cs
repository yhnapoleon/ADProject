namespace EcoLens.Api.DTOs.Food;

/// <summary>
/// 仅返回食物碳因子信息，用于前端在食物识别后自动填充排放因子。
/// </summary>
public class FoodCarbonFactorDto
{
	public string LabelName { get; set; } = string.Empty;
	public decimal Co2Factor { get; set; }
	public string Unit { get; set; } = string.Empty;
}
