using EcoLens.Api.DTOs.OpenFoodFacts;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace EcoLens.Api.Services
{
    public class OpenFoodFactsService : IOpenFoodFactsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public OpenFoodFactsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Open Food Facts API 没有 API Key，但最好还是从配置中读取 BaseUrl
            _baseUrl = configuration["OpenFoodFacts:BaseUrl"] ?? "https://world.openfoodfacts.org/api/v2/product/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public async Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync($"{barcode}?fields=product_name,categories_tags,brands,image_url,ecoscore_data", ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<OpenFoodFactsProductResponseDto>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        }
    }
}

