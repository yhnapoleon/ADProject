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
        public string[] Brands { get; set; } = Array.Empty<string>();
        public string ImageUrl { get; set; } = string.Empty;
        public EcoScoreDataDto? EcoScoreData { get; set; }
    }

    public class EcoScoreDataDto
    {
        public string? AdjustmentsNutritionDataGrade { get; set; }
        // 可以添加更多 Eco-Score 相关数据，如果需要
    }
}

