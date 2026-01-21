using EcoLens.Api.DTOs.Climatiq;

namespace EcoLens.Api.Services
{
    public interface IClimatiqService
    {
        Task<ClimatiqEstimateResponseDto?> GetCarbonEmissionEstimateAsync(
            string activityId,
            decimal quantity,
            string unit,
            string region = "US"); // 默认区域可以根据需求调整
    }
}

