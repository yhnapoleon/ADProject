using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Barcode;
using EcoLens.Api.DTOs.OpenFoodFacts;
using EcoLens.Api.DTOs.Climatiq;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EcoLens.Tests;

public class BarcodeControllerTests
{
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"BarcodeTests_{System.Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private class DummyOpenFoodFactsService : IOpenFoodFactsService
    {
        public Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
            => Task.FromResult<OpenFoodFactsProductResponseDto?>(null);
    }

    private class DummyClimatiqService : IClimatiqService
    {
        public Task<ClimatiqEstimateResponseDto?> GetCarbonEmissionEstimateAsync(string activityId, decimal quantity, string unit, string region = "US")
            => Task.FromResult<ClimatiqEstimateResponseDto?>(null);
    }

    private static BarcodeController CreateController(ApplicationDbContext db)
    {
        var lookupService = new BarcodeLookupService(
            db,
            new DummyOpenFoodFactsService(),
            new DummyClimatiqService());
        return new BarcodeController(db, lookupService);
    }

    [Fact]
    public async Task Get_ShouldFilterByBarcode()
    {
        // Arrange
        await using var db = CreateInMemoryDb();

        var carbon = new CarbonReference
        {
            LabelName = "Drink",
            Category = CarbonCategory.Food,
            Co2Factor = 1.2m,
            Unit = "kgCO2e/kg",
            Source = "Test"
        };
        db.CarbonReferences.Add(carbon);

        db.BarcodeReferences.AddRange(
            new BarcodeReference
            {
                Barcode = "123456",
                ProductName = "Cola",
                CarbonReference = carbon,
                Category = "Beverages",
                Brand = "BrandA"
            },
            new BarcodeReference
            {
                Barcode = "999999",
                ProductName = "Juice",
                CarbonReference = carbon,
                Category = "Beverages",
                Brand = "BrandB"
            });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var search = new SearchBarcodeReferenceDto { Barcode = "123" };

        // Act
        var result = await controller.Get(search, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<BarcodeReferenceResponseDto>>(ok.Value);
        var list = items.ToList();
        Assert.Single(list);
        Assert.Equal("123456", list[0].Barcode);
        Assert.Equal("Cola", list[0].ProductName);
    }

    [Fact]
    public async Task GetByBarcode_ShouldReturnExisting_WhenFound()
    {
        // Arrange
        await using var db = CreateInMemoryDb();

        var carbon = new CarbonReference
        {
            LabelName = "Snack",
            Category = CarbonCategory.Food,
            Co2Factor = 0.8m,
            Unit = "kgCO2e/kg",
            Source = "Test"
        };
        db.CarbonReferences.Add(carbon);

        db.BarcodeReferences.Add(new BarcodeReference
        {
            Barcode = "ABC123",
            ProductName = "Chips",
            CarbonReference = carbon,
            Category = "Snacks",
            Brand = "BrandX"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        // Act
        var result = await controller.GetByBarcode("ABC123", null, null, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<ActionResult<BarcodeReferenceResponseDto>>(result);
        var okResult = Assert.IsType<OkObjectResult>(ok.Result);
        var dto = Assert.IsType<BarcodeReferenceResponseDto>(okResult.Value);
        Assert.Equal("ABC123", dto.Barcode);
        Assert.Equal("Chips", dto.ProductName);
        Assert.Equal("Snack", dto.CarbonReferenceLabel);
        Assert.Equal(0.8m, dto.Co2Factor);
    }

    [Fact]
    public async Task GetByBarcode_ShouldCreateDefaultRecord_WhenMissingAndUseDefaultTrue()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var controller = CreateController(db);

        // Act
        var result = await controller.GetByBarcode("NOPE", null, useDefault: true, CancellationToken.None);

        // Assert
        var action = Assert.IsType<ActionResult<BarcodeReferenceResponseDto>>(result);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var dto = Assert.IsType<BarcodeReferenceResponseDto>(ok.Value);

        Assert.Equal("NOPE", dto.Barcode);
        Assert.Equal("Unknown Product", dto.ProductName);
        Assert.Equal("Unknown Food", dto.Category);
        Assert.Equal(0.5m, dto.Co2Factor);
        Assert.Equal("Default", dto.Source);

        // And record should be persisted
        Assert.True(await db.BarcodeReferences.AnyAsync(b => b.Barcode == "NOPE"));
    }

    private class OpenFoodFactsWithCo2Service : IOpenFoodFactsService
    {
        public Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            var dto = new OpenFoodFactsProductResponseDto
            {
                Code = barcode,
                Status = 1,
                Product = new ProductDto
                {
                    ProductName = "Beef steak",
                    CategoriesTags = new[] { "en:beef" },
                    EcoScoreData = new EcoScoreDataDto
                    {
                        Agribalyse = new AgribalyseDataDto { Co2Total = 4.0m }
                    }
                }
            };

            return Task.FromResult<OpenFoodFactsProductResponseDto?>(dto);
        }
    }

    private class ClimatiqWithValueService : IClimatiqService
    {
        public Task<ClimatiqEstimateResponseDto?> GetCarbonEmissionEstimateAsync(string activityId, decimal quantity, string unit, string region = "US")
        {
            return Task.FromResult<ClimatiqEstimateResponseDto?>(new ClimatiqEstimateResponseDto
            {
                Co2e = 2m,
                Co2eUnit = "kg"
            });
        }
    }

    private class OpenFoodFactsNoCo2Service : IOpenFoodFactsService
    {
        public Task<OpenFoodFactsProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            var dto = new OpenFoodFactsProductResponseDto
            {
                Code = barcode,
                Status = 1,
                Product = new ProductDto
                {
                    ProductName = "Beef snack",
                    CategoriesTags = new[] { "en:beef" },
                    EcoScoreData = new EcoScoreDataDto
                    {
                        Agribalyse = null,
                        AgribalyseCo2Total = null
                    }
                }
            };
            return Task.FromResult<OpenFoodFactsProductResponseDto?>(dto);
        }
    }

    [Fact]
    public async Task GetByBarcode_WithRefresh_ShouldUpdateFromOpenFoodFacts_WhenCo2Available()
    {
        await using var db = CreateInMemoryDb();

        var carbon = new CarbonReference
        {
            LabelName = "Unknown Food",
            Category = CarbonCategory.Food,
            Co2Factor = 0.5m,
            Unit = "kgCO2e/kg",
            Source = "Default"
        };
        db.CarbonReferences.Add(carbon);

        db.BarcodeReferences.Add(new BarcodeReference
        {
            Barcode = "BEEF123",
            ProductName = "Old Name",
            CarbonReference = carbon,
            Category = "Unknown Food",
            Brand = "BrandY"
        });
        await db.SaveChangesAsync();

        var lookupService = new BarcodeLookupService(db, new OpenFoodFactsWithCo2Service(), new DummyClimatiqService());
        var controller = new BarcodeController(db, lookupService);

        var result = await controller.GetByBarcode("BEEF123", refresh: true, useDefault: null, CancellationToken.None);

        var action = Assert.IsType<ActionResult<BarcodeReferenceResponseDto>>(result);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var dto = Assert.IsType<BarcodeReferenceResponseDto>(ok.Value);

        Assert.Equal("BEEF123", dto.Barcode);
        Assert.Equal(4.0m, dto.Co2Factor);
        Assert.Equal("OpenFoodFacts", dto.Source);

        var updated = await db.CarbonReferences.SingleAsync();
        Assert.Equal(4.0m, updated.Co2Factor);
        Assert.Equal("OpenFoodFacts", updated.Source);
    }

    [Fact]
    public async Task GetByBarcode_WithRefresh_ShouldFallbackToClimatiq_WhenNoCo2InOff()
    {
        await using var db = CreateInMemoryDb();

        var carbon = new CarbonReference
        {
            LabelName = "Unknown Food",
            Category = CarbonCategory.Food,
            Co2Factor = 0.5m,
            Unit = "kgCO2e/kg",
            Source = "Default"
        };
        db.CarbonReferences.Add(carbon);

        db.BarcodeReferences.Add(new BarcodeReference
        {
            Barcode = "BEEFCLIM",
            ProductName = "Old Name",
            CarbonReference = carbon,
            Category = "Unknown Food",
            Brand = "BrandY"
        });
        await db.SaveChangesAsync();

        var lookupService = new BarcodeLookupService(db, new OpenFoodFactsNoCo2Service(), new ClimatiqWithValueService());
        var controller = new BarcodeController(db, lookupService);

        var result = await controller.GetByBarcode("BEEFCLIM", refresh: true, useDefault: null, CancellationToken.None);

        var action = Assert.IsType<ActionResult<BarcodeReferenceResponseDto>>(result);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var dto = Assert.IsType<BarcodeReferenceResponseDto>(ok.Value);

        // Climatiq 返回 2，beef multiplier 7.3 => 14.6
        Assert.Equal("BEEFCLIM", dto.Barcode);
        Assert.Equal("Climatiq", dto.Source);
        Assert.True(dto.Co2Factor > 0.5m);
    }

    [Fact]
    public async Task DeleteByBarcode_ShouldRemoveRecord_WhenExists()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        db.BarcodeReferences.Add(new BarcodeReference
        {
            Barcode = "DEL123",
            ProductName = "ToDelete"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        // Act
        var result = await controller.DeleteByBarcode("DEL123", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.False(await db.BarcodeReferences.AnyAsync(b => b.Barcode == "DEL123"));
    }
}

