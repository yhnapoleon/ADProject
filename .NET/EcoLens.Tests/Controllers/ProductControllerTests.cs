using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class ProductControllerTests
{
	private static ApplicationDbContext CreateDb(bool withBarcodeRef = false)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withBarcodeRef)
		{
			var carbon = new CarbonReference
			{
				LabelName = "Test Product",
				Category = CarbonCategory.Food,
				Co2Factor = 0.8m,
				Unit = "kg CO2e/kg"
			};
			db.CarbonReferences.Add(carbon);
			db.SaveChanges();
			db.BarcodeReferences.Add(new BarcodeReference
			{
				Barcode = "1234567890123",
				ProductName = "Test",
				CarbonReferenceId = carbon.Id
			});
			db.SaveChanges();
		}
		return db;
	}

	[Fact]
	public async Task GetByBarcode_ReturnsBadRequest_WhenBarcodeEmpty()
	{
		await using var db = CreateDb();
		var controller = new ProductController(db);

		var result = await controller.GetByBarcode("", CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Barcode is required.", badRequest.Value);
	}

	[Fact]
	public async Task GetByBarcode_ReturnsBadRequest_WhenBarcodeNull()
	{
		await using var db = CreateDb();
		var controller = new ProductController(db);

		var result = await controller.GetByBarcode(null!, CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetByBarcode_ReturnsNotFound_WhenNoMatch()
	{
		await using var db = CreateDb();
		var controller = new ProductController(db);

		var result = await controller.GetByBarcode("0000000000000", CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task GetByBarcode_ReturnsOkWithDto_WhenMatchExists()
	{
		await using var db = CreateDb(withBarcodeRef: true);
		var controller = new ProductController(db);

		var result = await controller.GetByBarcode("1234567890123", CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<EcoLens.Api.DTOs.Product.ProductLookupResponseDto>(ok.Value);
		Assert.Equal("Test Product", dto.Name);
		Assert.Equal(0.8m, dto.Co2Factor);
		Assert.Equal("kg CO2e/kg", dto.Unit);
		Assert.Equal("1234567890123", dto.Barcode);
	}
}
