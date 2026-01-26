using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.Climatiq
{
    public class ClimatiqEstimateResponseDto
    {
        [JsonPropertyName("co2e")]
        public decimal Co2e { get; set; }
        
        [JsonPropertyName("co2e_unit")]
        public string Co2eUnit { get; set; } = string.Empty;
        
        // ActivityId 在 emission_factor 对象内，如果需要可以从那里提取
        // 当前仅使用 co2e 值作为因子
    }
}

