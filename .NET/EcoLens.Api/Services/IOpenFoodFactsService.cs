using EcoLens.Api.DTOs.OpenFoodFacts;

namespace EcoLens.Api.Services
{
    public interface IOpenFoodFactsService
    {
        Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default);
    }
}

