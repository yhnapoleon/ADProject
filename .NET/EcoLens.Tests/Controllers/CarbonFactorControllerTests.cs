using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class CarbonFactorControllerTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new ApplicationDbContext(options);
	}

	[Fact]
	public async Task GetFactors_ReturnsOkEmptyList_WhenNoData()
	{
		await using var db = CreateDb();
		var controller = new CarbonFactorController(db);

		var result = await controller.GetFactors(null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonFactorController.CarbonFactorDto>>(ok.Value);
		Assert.Empty(list);
	}

	[Fact]
	public async Task GetFactors_ReturnsBadRequest_WhenCategoryInvalid()
	{
		await using var db = CreateDb();
		var controller = new CarbonFactorController(db);

		var result = await controller.GetFactors("InvalidCategory", CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetFactors_ReturnsOkWithItems_WhenCategoryValid()
	{
		await using var db = CreateDb();
		await db.CarbonReferences.AddAsync(new CarbonReference
		{
			LabelName = "Rice",
			Category = CarbonCategory.Food,
			Co2Factor = 0.5m,
			Unit = "kg"
		});
		await db.SaveChangesAsync();
		var controller = new CarbonFactorController(db);

		var result = await controller.GetFactors("Food", CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonFactorController.CarbonFactorDto>>(ok.Value).ToList();
		Assert.Single(list);
		Assert.Equal("Rice", list[0].LabelName);
		Assert.Equal(0.5m, list[0].Co2Factor);
	}

	[Fact]
	public async Task LookupFactors_ReturnsOkFromStatic_WhenLabelInCarbonEmissionData()
	{
		await using var db = CreateDb();
		var controller = new CarbonFactorController(db);
		// CarbonEmissionData has "Fried Rice" etc.

		var result = await controller.LookupFactors("Fried Rice", null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonFactorController.CarbonFactorDto>>(ok.Value).ToList();
		Assert.Single(list);
		Assert.Equal("Fried Rice", list[0].LabelName);
		Assert.Equal(0.90m, list[0].Co2Factor);
	}

	[Fact]
	public async Task LookupFactors_ReturnsOkFallback_WhenLabelNotFound()
	{
		await using var db = CreateDb();
		var controller = new CarbonFactorController(db);

		var result = await controller.LookupFactors("UnknownFood123", null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonFactorController.CarbonFactorDto>>(ok.Value).ToList();
		Assert.Single(list);
		Assert.Equal("UnknownFood123", list[0].LabelName);
		Assert.Equal(1.0m, list[0].Co2Factor);
	}

	[Fact]
	public async Task LookupFactors_ReturnsOkCombinedList_WhenNoLabel()
	{
		await using var db = CreateDb();
		var controller = new CarbonFactorController(db);

		var result = await controller.LookupFactors(null, null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonFactorController.CarbonFactorDto>>(ok.Value).ToList();
		Assert.NotEmpty(list);
		Assert.True(list.All(x => !string.IsNullOrEmpty(x.LabelName) && x.Co2Factor >= 0));
	}
}
