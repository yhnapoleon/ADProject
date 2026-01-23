namespace EcoLens.Api.DTOs.Climatiq
{
    public class ClimatiqEstimateResponseDto
    {
        public decimal Co2e { get; set; }
        public string Co2eUnit { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        // 根据实际需要，可以添加更多响应字段，例如：
        // public string Category { get; set; } = string.Empty;
        // public string Source { get; set; } = string.Empty;
    }
}

