using EcoLens.Api.DTOs.OpenFoodFacts;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace EcoLens.Api.Services
{
    public class OpenFoodFactsService : IOpenFoodFactsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public OpenFoodFactsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["OpenFoodFacts:BaseUrl"] ?? "https://world.openfoodfacts.org/api/v2/product/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public async Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync($"{barcode}?fields=product_name,categories_tags,brands,image_url,ecoscore_data", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OpenFoodFactsProductResponseDto>(content, JsonOptions);

            // If co2_total still missing after deserialize, try to patch from raw JSON
            if (result?.Product != null && !TryGetCo2FromEcoScore(result.Product.EcoScoreData, out _))
                TryPatchEcoScoreFromRaw(content, result.Product);

            return result;
        }

        private static bool TryGetCo2FromEcoScore(EcoScoreDataDto? eco, out decimal co2)
        {
            co2 = 0;
            if (eco == null) return false;
            var v = eco.Agribalyse?.Co2Total ?? eco.AgribalyseCo2Total;
            if (v == null) return false;
            co2 = v.Value;
            return true;
        }

        private static void TryPatchEcoScoreFromRaw(string json, ProductDto product)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("product", out var prod)) return;
                if (!prod.TryGetProperty("ecoscore_data", out var eco)) return;

                decimal? co2 = null;
                if (eco.ValueKind == JsonValueKind.String)
                {
                    var s = eco.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return;
                    using var inner = JsonDocument.Parse(s);
                    TryGetCo2FromElement(inner.RootElement, out co2);
                }
                else if (eco.ValueKind == JsonValueKind.Object)
                {
                    TryGetCo2FromElement(eco, out co2);
                }
                if (co2 == null) return;

                product.EcoScoreData ??= new EcoScoreDataDto();
                if (product.EcoScoreData.Agribalyse != null)
                    product.EcoScoreData.Agribalyse.Co2Total = co2;
                else
                    product.EcoScoreData.Agribalyse = new AgribalyseDataDto { Co2Total = co2 };
            }
            catch { /* ignore parse error */ }
        }

        private static void TryGetCo2FromElement(JsonElement el, out decimal? co2)
        {
            co2 = null;
            if (el.TryGetProperty("agribalyse", out var ag) && ag.TryGetProperty("co2_total", out var c1))
                co2 = c1.GetDecimal();
            if (co2 == null && el.TryGetProperty("agribalyse_co2_total", out var c2))
                co2 = c2.GetDecimal();
        }
    }
}
