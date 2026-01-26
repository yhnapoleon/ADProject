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
            _baseUrl = configuration["Climatiq:BaseUrl"] ?? "https://api.climatiq.io";

            // BaseUrl 应该是基础 URL（如 https://api.climatiq.io），endpoint 路径在 PostAsync 中指定
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
                    Region = region,
                    DataVersion = "^3" // 使用 data version 3（包含 2025 年的数据）
                },
                Parameters = new ParametersRequestDto
                {
                    Weight = quantity,
                    WeightUnit = unit
                }
            };

            // Climatiq API 要求使用 snake_case，不使用 camelCase
            var jsonOptions = new JsonSerializerOptions 
            { 
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
            };
            var jsonString = JsonSerializer.Serialize(requestDto, jsonOptions);
            
            // 调试输出（生产环境应使用 ILogger）
            System.Diagnostics.Debug.WriteLine($"Climatiq request JSON: {jsonString}");
            
            var jsonContent = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/data/v1/estimate", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Climatiq API error: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            // Climatiq 响应使用 snake_case，需要配置反序列化
            return JsonSerializer.Deserialize<ClimatiqEstimateResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}

