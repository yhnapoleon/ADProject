using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.Climatiq
{
    public class ClimatiqEstimateRequestDto
    {
        [JsonPropertyName("emission_factor")]
        public EmissionFactorRequestDto EmissionFactor { get; set; } = new EmissionFactorRequestDto();
        
        [JsonPropertyName("parameters")]
        public ParametersRequestDto Parameters { get; set; } = new ParametersRequestDto();
    }

    public class EmissionFactorRequestDto
    {
        [JsonPropertyName("activity_id")]
        public string ActivityId { get; set; } = string.Empty;
        
        [JsonPropertyName("region")]
        public string? Region { get; set; }
        
        [JsonPropertyName("data_version")]
        public string? DataVersion { get; set; }
    }

    public class ParametersRequestDto
    {
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }
        
        [JsonPropertyName("weight_unit")]
        public string WeightUnit { get; set; } = string.Empty;
    }
}

