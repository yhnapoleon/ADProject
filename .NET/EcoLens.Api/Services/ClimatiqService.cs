using EcoLens.Api.DTOs.Climatiq;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EcoLens.Api.Services
{
    public class ClimatiqService : IClimatiqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public ClimatiqService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Climatiq:ApiKey"] ?? throw new ArgumentNullException("Climatiq:ApiKey not found.");
            _baseUrl = configuration["Climatiq:BaseUrl"] ?? throw new ArgumentNullException("Climatiq:BaseUrl not found.");

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ClimatiqEstimateResponseDto?> GetCarbonEmissionEstimateAsync(
            string activityId,
            decimal quantity,
            string unit,
            string region = "US")
        {
            var requestDto = new ClimatiqEstimateRequestDto
            {
                EmissionFactor = new EmissionFactorRequestDto
                {
                    ActivityId = activityId,
                    Region = region
                },
                Parameters = new ParametersRequestDto
                {
                    Weight = quantity,
                    WeightUnit = unit
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestDto, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/estimate", jsonContent);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ClimatiqEstimateResponseDto>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}

