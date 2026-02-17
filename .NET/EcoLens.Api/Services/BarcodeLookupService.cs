using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Barcode;
using EcoLens.Api.DTOs.OpenFoodFacts;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

/// <summary>
/// Lookup or create barcode reference; encapsulates OFF + Climatiq + default logic to keep controller simple.
/// </summary>
public class BarcodeLookupService : IBarcodeLookupService
{
	private const string DefaultCategoryLabelName = "Unknown Food";
	private const decimal DefaultCo2Factor = 0.5m;
	private const string DefaultUnit = "kgCO2e/kg";
	private const string DefaultSource = "Default";

	private static readonly string[] LanguagePrefixes = { "en:", "fr:", "de:", "es:", "it:", "pt:", "nl:", "pl:", "ru:", "ja:", "zh:" };

	private readonly ApplicationDbContext _db;
	private readonly IOpenFoodFactsService _openFoodFactsService;
	private readonly IClimatiqService _climatiqService;

	public BarcodeLookupService(
		ApplicationDbContext db,
		IOpenFoodFactsService openFoodFactsService,
		IClimatiqService climatiqService)
	{
		_db = db;
		_openFoodFactsService = openFoodFactsService;
		_climatiqService = climatiqService;
	}

	public async Task<BarcodeReferenceResponseDto> GetByBarcodeAsync(string barcode, bool? refresh = null, bool? useDefault = null, CancellationToken ct = default)
	{
		var barcodeRef = await _db.BarcodeReferences
			.Include(b => b.CarbonReference)
			.FirstOrDefaultAsync(b => b.Barcode == barcode, ct);

		if (barcodeRef != null && refresh == true)
		{
			await RefreshExistingFromOffAsync(barcodeRef, barcode, ct);
		}
		else if (barcodeRef == null)
		{
			if (useDefault == true)
			{
				barcodeRef = await CreateWithDefaultAsync(barcode, ct);
			}
			else
			{
				barcodeRef = await CreateFromOffOrDefaultAsync(barcode, ct);
			}
		}

		return ToDto(barcodeRef!);
	}

	private async Task RefreshExistingFromOffAsync(BarcodeReference barcodeRef, string barcode, CancellationToken ct)
	{
		var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);
		if (!IsOffProductValid(offProduct) || barcodeRef.CarbonReference == null)
		{
			await ApplyDefaultCarbonToExistingAsync(barcodeRef, ct);
			return;
		}

		var co2 = GetCo2FromOff(offProduct!);
		if (co2 != null)
		{
			barcodeRef.CarbonReference.Co2Factor = co2.Value;
			barcodeRef.CarbonReference.Unit = DefaultUnit;
			barcodeRef.CarbonReference.Source = "OpenFoodFacts";
			barcodeRef.CarbonReference.ClimatiqActivityId = null;
			await _db.SaveChangesAsync(ct);
			return;
		}

		var categoryLabel = NormalizeCategoryLabelFromTags(offProduct!.Product!.CategoriesTags);
		if (categoryLabel == DefaultCategoryLabelName || string.IsNullOrWhiteSpace(categoryLabel))
		{
			ApplyDefaultToCarbonRef(barcodeRef.CarbonReference);
			await _db.SaveChangesAsync(ct);
			return;
		}

		var climatiqResult = await TryGetCo2FromClimatiqAsync(offProduct.Product.CategoriesTags, ct);
		if (climatiqResult != null)
		{
			barcodeRef.CarbonReference.Co2Factor = climatiqResult.Value.factor;
			barcodeRef.CarbonReference.Unit = DefaultUnit;
			barcodeRef.CarbonReference.Source = "Climatiq";
			barcodeRef.CarbonReference.ClimatiqActivityId = climatiqResult.Value.activityId;
		}
		else
		{
			ApplyDefaultToCarbonRef(barcodeRef.CarbonReference);
		}
		await _db.SaveChangesAsync(ct);
	}

	private async Task ApplyDefaultCarbonToExistingAsync(BarcodeReference barcodeRef, CancellationToken ct)
	{
		var defaultRef = await GetOrCreateDefaultCarbonReferenceAsync(ct);
		barcodeRef.CarbonReferenceId = defaultRef.Id;
		barcodeRef.CarbonReference = defaultRef;
		barcodeRef.ProductName = "Unknown Product";
		barcodeRef.Category = DefaultCategoryLabelName;
		barcodeRef.Brand = null;
		await _db.SaveChangesAsync(ct);
	}

	private async Task<BarcodeReference> CreateWithDefaultAsync(string barcode, CancellationToken ct)
	{
		var defaultRef = await GetOrCreateDefaultCarbonReferenceAsync(ct);
		var barcodeRef = new BarcodeReference
		{
			Barcode = barcode,
			ProductName = "Unknown Product",
			CarbonReferenceId = defaultRef.Id,
			Category = DefaultCategoryLabelName,
			Brand = null
		};
		barcodeRef.CarbonReference = defaultRef;
		await _db.BarcodeReferences.AddAsync(barcodeRef, ct);
		await _db.SaveChangesAsync(ct);
		return barcodeRef;
	}

	private async Task<BarcodeReference> CreateFromOffOrDefaultAsync(string barcode, CancellationToken ct)
	{
		var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);
		if (!IsOffProductValid(offProduct))
		{
			return await CreateWithDefaultAsync(barcode, ct);
		}

		var categoryLabel = NormalizeCategoryLabelFromTags(offProduct!.Product!.CategoriesTags);
		var co2FromOff = GetCo2FromOff(offProduct);
		decimal? extractedCo2 = co2FromOff;
		string? climatiqActivityId = null;
		var fromClimatiq = false;

		if (!extractedCo2.HasValue && categoryLabel != DefaultCategoryLabelName && !string.IsNullOrWhiteSpace(categoryLabel))
		{
			var climatiqResult = await TryGetCo2FromClimatiqAsync(offProduct.Product.CategoriesTags, ct);
			if (climatiqResult != null)
			{
				extractedCo2 = climatiqResult.Value.factor;
				climatiqActivityId = climatiqResult.Value.activityId;
				fromClimatiq = true;
			}
		}

		var carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
			c => c.LabelName == categoryLabel && c.Category == CarbonCategory.Food, ct);

		if (carbonRef != null)
		{
			carbonRef.Co2Factor = extractedCo2 ?? DefaultCo2Factor;
			carbonRef.Unit = DefaultUnit;
			carbonRef.Source = extractedCo2.HasValue ? (fromClimatiq ? "Climatiq" : "OpenFoodFacts") : DefaultSource;
			carbonRef.ClimatiqActivityId = fromClimatiq ? climatiqActivityId : null;
		}
		else
		{
			carbonRef = new CarbonReference
			{
				LabelName = categoryLabel,
				Category = CarbonCategory.Food,
				Co2Factor = extractedCo2 ?? DefaultCo2Factor,
				Unit = DefaultUnit,
				Source = extractedCo2.HasValue ? (fromClimatiq ? "Climatiq" : "OpenFoodFacts") : DefaultSource,
				ClimatiqActivityId = fromClimatiq ? climatiqActivityId : null
			};
			await _db.CarbonReferences.AddAsync(carbonRef, ct);
		}
		await _db.SaveChangesAsync(ct);

		var barcodeRef = new BarcodeReference
		{
			Barcode = barcode,
			ProductName = offProduct.Product.ProductName ?? "Unknown",
			CarbonReferenceId = carbonRef.Id,
			Category = categoryLabel,
			Brand = offProduct.Product.Brands
		};
		barcodeRef.CarbonReference = carbonRef;
		await _db.BarcodeReferences.AddAsync(barcodeRef, ct);
		await _db.SaveChangesAsync(ct);
		return barcodeRef;
	}

	private static bool IsOffProductValid(OpenFoodFactsProductResponseDto? offProduct)
	{
		if (offProduct?.Product == null || offProduct.Status != 1) return false;
		var name = offProduct.Product.ProductName;
		if (string.IsNullOrWhiteSpace(name) || name.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) return false;
		var tags = offProduct.Product.CategoriesTags;
		return tags != null && tags.Length > 0;
	}

	private static decimal? GetCo2FromOff(OpenFoodFactsProductResponseDto offProduct)
	{
		return offProduct.Product?.EcoScoreData?.Agribalyse?.Co2Total
			?? offProduct.Product?.EcoScoreData?.AgribalyseCo2Total;
	}

	private static string NormalizeCategoryLabelFromTags(string[]? tags)
	{
		if (tags == null || tags.Length == 0) return DefaultCategoryLabelName;
		var first = tags.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(first)) return DefaultCategoryLabelName;
		var label = first;
		foreach (var prefix in LanguagePrefixes)
		{
			if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				label = label.Substring(prefix.Length);
				break;
			}
		}
		return label.Replace("-", " ");
	}

	private async Task<(decimal factor, string? activityId)?> TryGetCo2FromClimatiqAsync(string[]? categoriesTags, CancellationToken ct)
	{
		try
		{
			var activityId = ClimatiqActivityMapping.GetActivityIdForFood(categoriesTags);
			var estimate = await _climatiqService.GetCarbonEmissionEstimateAsync(activityId, 1m, "kg", ClimatiqActivityMapping.DefaultFoodRegion);
			if (estimate == null || estimate.Co2e <= 0) return null;
			var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(categoriesTags);
			return (estimate.Co2e * multiplier, activityId);
		}
		catch
		{
			return null;
		}
	}

	private static void ApplyDefaultToCarbonRef(CarbonReference carbonRef)
	{
		carbonRef.Co2Factor = DefaultCo2Factor;
		carbonRef.Unit = DefaultUnit;
		carbonRef.Source = DefaultSource;
		carbonRef.ClimatiqActivityId = null;
	}

	private async Task<CarbonReference> GetOrCreateDefaultCarbonReferenceAsync(CancellationToken ct)
	{
		var existing = await _db.CarbonReferences.FirstOrDefaultAsync(
			c => c.LabelName == DefaultCategoryLabelName && c.Category == CarbonCategory.Food, ct);
		if (existing != null)
		{
			existing.Co2Factor = DefaultCo2Factor;
			existing.Unit = DefaultUnit;
			existing.Source = DefaultSource;
			existing.ClimatiqActivityId = null;
			await _db.SaveChangesAsync(ct);
			return existing;
		}
		var created = new CarbonReference
		{
			LabelName = DefaultCategoryLabelName,
			Category = CarbonCategory.Food,
			Co2Factor = DefaultCo2Factor,
			Unit = DefaultUnit,
			Source = DefaultSource
		};
		await _db.CarbonReferences.AddAsync(created, ct);
		await _db.SaveChangesAsync(ct);
		return created;
	}

	private static BarcodeReferenceResponseDto ToDto(BarcodeReference b)
	{
		return new BarcodeReferenceResponseDto
		{
			Id = b.Id,
			Barcode = b.Barcode,
			ProductName = b.ProductName,
			CarbonReferenceId = b.CarbonReferenceId,
			CarbonReferenceLabel = b.CarbonReference?.LabelName,
			Co2Factor = b.CarbonReference?.Co2Factor,
			Unit = b.CarbonReference?.Unit,
			Source = b.CarbonReference?.Source,
			ClimatiqActivityId = b.CarbonReference?.ClimatiqActivityId,
			Category = b.Category,
			Brand = b.Brand
		};
	}
}
