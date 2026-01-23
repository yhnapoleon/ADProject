namespace EcoLens.Api.DTOs.OpenFoodFacts
{
    public class OpenFoodFactsProductResponseDto
    {
        public string Code { get; set; } = string.Empty;
        public int Status { get; set; }
        public ProductDto? Product { get; set; }
    }

    public class ProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string[] CategoriesTags { get; set; } = Array.Empty<string>();
        public string? Brands { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public EcoScoreDataDto? EcoScoreData { get; set; }
    }

    public class EcoScoreDataDto
    {
        public string? AdjustmentsNutritionDataGrade { get; set; }
        public AgribalyseDataDto? Agribalyse { get; set; }
        // 可以添加更多 Eco-Score 相关数据，如果需要
    }

    public class AgribalyseDataDto
    {
        public decimal? Co2Total { get; set; }
        // Open Food Facts API 没有直接提供 Co2Unit 在 Agribalyse 对象中，我们需要假设或从其他地方获取
        // 在 BarcodeController 中处理单位转换
    }
}

