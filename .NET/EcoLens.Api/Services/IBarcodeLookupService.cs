using EcoLens.Api.DTOs.Barcode;

namespace EcoLens.Api.Services;

/// <summary>
/// Lookup or create barcode reference (local + Open Food Facts + Climatiq).
/// </summary>
public interface IBarcodeLookupService
{
	Task<BarcodeReferenceResponseDto> GetByBarcodeAsync(
		string barcode,
		bool? refresh = null,
		bool? useDefault = null,
		CancellationToken ct = default);
}
