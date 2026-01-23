namespace EcoLens.Api.DTOs.Climatiq
{
    public class ClimatiqEstimateRequestDto
    {
        public EmissionFactorRequestDto EmissionFactor { get; set; } = new EmissionFactorRequestDto();
        public ParametersRequestDto Parameters { get; set; } = new ParametersRequestDto();
    }

    public class EmissionFactorRequestDto
    {
        public string ActivityId { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public class ParametersRequestDto
    {
        public decimal Weight { get; set; }
        public string WeightUnit { get; set; } = string.Empty;
    }
}

