using System;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EcoLens.Tests;

public class UtilityBillCalculationServiceTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new ApplicationDbContext(options);
	}

	private static UtilityBillCalculationService CreateService(ApplicationDbContext db)
	{
		var logger = new LoggerFactory().CreateLogger<UtilityBillCalculationService>();
		return new UtilityBillCalculationService(db, logger);
	}

	[Fact]
	public async Task CalculateCarbonEmissionAsync_ShouldReturnZero_WhenNoUsage()
	{
		await using var db = CreateDb();
		db.CarbonReferences.AddRange(
			new CarbonReference { LabelName = "Electricity", Category = CarbonCategory.Utility, Co2Factor = 0.5m, Unit = "kWh" },
			new CarbonReference { LabelName = "Water", Category = CarbonCategory.Utility, Co2Factor = 0.2m, Unit = "m3" },
			new CarbonReference { LabelName = "Gas", Category = CarbonCategory.Utility, Co2Factor = 1.0m, Unit = "m3" }
		);
		await db.SaveChangesAsync();

		var service = CreateService(db);

		var result = await service.CalculateCarbonEmissionAsync(null, null, null, CancellationToken.None);

		Assert.Equal(0m, result.ElectricityCarbon);
		Assert.Equal(0m, result.WaterCarbon);
		Assert.Equal(0m, result.GasCarbon);
		Assert.Equal(0m, result.TotalCarbon);
	}

	[Fact]
	public async Task CalculateCarbonEmissionAsync_ShouldCalculatePerUtility()
	{
		await using var db = CreateDb();
		db.CarbonReferences.AddRange(
			new CarbonReference { LabelName = "Electricity", Category = CarbonCategory.Utility, Co2Factor = 0.5m, Unit = "kWh" },
			new CarbonReference { LabelName = "Water", Category = CarbonCategory.Utility, Co2Factor = 0.2m, Unit = "m3" },
			new CarbonReference { LabelName = "Gas", Category = CarbonCategory.Utility, Co2Factor = 1.0m, Unit = "m3" }
		);
		await db.SaveChangesAsync();

		var service = CreateService(db);

		var result = await service.CalculateCarbonEmissionAsync(
			electricityUsage: 100m,
			waterUsage: 10m,
			gasUsage: 5m,
			ct: CancellationToken.None);

		Assert.Equal(50m, result.ElectricityCarbon); // 100 * 0.5
		Assert.Equal(2m, result.WaterCarbon);        // 10 * 0.2
		Assert.Equal(5m, result.GasCarbon);         // 5 * 1.0
		Assert.Equal(57m, result.TotalCarbon);
	}

	[Fact]
	public async Task CalculateCarbonEmissionAsync_ShouldThrow_WhenElectricityFactorMissing()
	{
		await using var db = CreateDb();
		// 只插入 Water / Gas，缺少 Electricity 因子
		db.CarbonReferences.AddRange(
			new CarbonReference { LabelName = "Water", Category = CarbonCategory.Utility, Co2Factor = 0.2m, Unit = "m3" },
			new CarbonReference { LabelName = "Gas", Category = CarbonCategory.Utility, Co2Factor = 1.0m, Unit = "m3" }
		);
		await db.SaveChangesAsync();

		var service = CreateService(db);

		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			service.CalculateCarbonEmissionAsync(100m, 10m, 5m, CancellationToken.None));

		Assert.Contains("Electricity carbon emission factor not found", ex.Message);
	}
}

