using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Barcode;
using EcoLens.Api.DTOs.Climatiq;
using EcoLens.Api.DTOs.OpenFoodFacts;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoLens.Tests.Services;

public class BarcodeLookupServiceTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase($"BarcodeLookup_{Guid.NewGuid():N}")
			.Options;
		return new ApplicationDbContext(options);
	}

	private class NullOffService : IOpenFoodFactsService
	{
		public Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
			=> Task.FromResult<OpenFoodFactsProductResponseDto?>(null);
	}

	private class NullClimatiqService : IClimatiqService
	{
		public Task<ClimatiqEstimateResponseDto?> GetCarbonEmissionEstimateAsync(string activityId, decimal quantity, string unit, string region = "US")
			=> Task.FromResult<ClimatiqEstimateResponseDto?>(null);
	}

	[Fact]
	public async Task GetByBarcodeAsync_ReturnsExisting_WhenFoundInDb()
	{
		await using var db = CreateDb();
		var carbon = new CarbonReference
		{
			LabelName = "Snack",
			Category = CarbonCategory.Food,
			Co2Factor = 1.5m,
			Unit = "kgCO2e/kg",
			Source = "Test"
		};
		db.CarbonReferences.Add(carbon);
		db.BarcodeReferences.Add(new BarcodeReference
		{
			Barcode = "EXIST",
			ProductName = "Chips",
			CarbonReference = carbon,
			Category = "Snacks",
			Brand = "X"
		});
		await db.SaveChangesAsync();

		var svc = new BarcodeLookupService(db, new NullOffService(), new NullClimatiqService());
		var dto = await svc.GetByBarcodeAsync("EXIST", null, null, CancellationToken.None);

		Assert.Equal("EXIST", dto.Barcode);
		Assert.Equal("Chips", dto.ProductName);
		Assert.Equal(1.5m, dto.Co2Factor);
		Assert.Equal("Snack", dto.CarbonReferenceLabel);
	}

	[Fact]
	public async Task GetByBarcodeAsync_CreatesDefault_WhenMissingAndUseDefaultTrue()
	{
		await using var db = CreateDb();
		var svc = new BarcodeLookupService(db, new NullOffService(), new NullClimatiqService());

		var dto = await svc.GetByBarcodeAsync("NEWCODE", null, useDefault: true, CancellationToken.None);

		Assert.Equal("NEWCODE", dto.Barcode);
		Assert.Equal("Unknown Product", dto.ProductName);
		Assert.Equal(0.5m, dto.Co2Factor);
		Assert.Equal("Default", dto.Source);
		Assert.True(await db.BarcodeReferences.AnyAsync(b => b.Barcode == "NEWCODE"));
	}

	[Fact]
	public async Task GetByBarcodeAsync_CreatesFromOff_WhenMissingAndOffReturnsValidProduct()
	{
		await using var db = CreateDb();
		var offService = new StubOffServiceWithCo2("Beef steak", new[] { "en:beef" }, 3.0m);
		var svc = new BarcodeLookupService(db, offService, new NullClimatiqService());

		var dto = await svc.GetByBarcodeAsync("BEEF999", null, useDefault: null, CancellationToken.None);

		Assert.Equal("BEEF999", dto.Barcode);
		Assert.Equal("Beef steak", dto.ProductName);
		Assert.Equal(3.0m, dto.Co2Factor);
		Assert.Equal("OpenFoodFacts", dto.Source);
		var refs = await db.BarcodeReferences.Where(b => b.Barcode == "BEEF999").ToListAsync();
		Assert.Single(refs);
	}

	private class StubOffServiceWithCo2 : IOpenFoodFactsService
	{
		private readonly string _productName;
		private readonly string[] _categories;
		private readonly decimal _co2;

		public StubOffServiceWithCo2(string productName, string[] categories, decimal co2)
		{
			_productName = productName;
			_categories = categories;
			_co2 = co2;
		}

		public Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
		{
			return Task.FromResult<OpenFoodFactsProductResponseDto?>(new OpenFoodFactsProductResponseDto
			{
				Code = barcode,
				Status = 1,
				Product = new ProductDto
				{
					ProductName = _productName,
					CategoriesTags = _categories,
					EcoScoreData = new EcoScoreDataDto { Agribalyse = new AgribalyseDataDto { Co2Total = _co2 } }
				}
			});
		}
	}
}
