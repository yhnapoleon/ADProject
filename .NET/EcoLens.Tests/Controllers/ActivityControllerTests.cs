using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Activity;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EcoLens.Tests.Controllers;

public class ActivityControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withUser = false)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withUser)
		{
			db.ApplicationUsers.Add(new ApplicationUser
			{
				Username = "u1",
				Email = "u1@t.com",
				PasswordHash = "x",
				Role = UserRole.User,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-20)
			});
			await db.SaveChangesAsync();
		}
		return db;
	}

	private static void SetUser(ControllerBase controller, int userId)
	{
		var identity = new ClaimsIdentity();
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
		};
	}

	[Fact]
	public async Task Upload_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object, NullLogger<ActivityController>.Instance);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Upload(new CreateActivityLogDto { Label = "Beef", Quantity = 1, Unit = "kgCO2" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Upload_ReturnsNotFound_WhenCarbonReferenceMissing()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		mockClimatiq.Setup(x => x.GetCarbonEmissionEstimateAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((EcoLens.Api.DTOs.Climatiq.ClimatiqEstimateResponseDto?)null);
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object, NullLogger<ActivityController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Upload(new CreateActivityLogDto { Label = "NonExistentLabel", Quantity = 1, Unit = "kg" }, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
		Assert.NotNull(notFound.Value);
	}

	[Fact]
	public async Task Upload_ReturnsOk_WhenCarbonReferenceExists()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		// InMemory DB has seed CarbonReferences from OnModelCreating
		var carbonRef = await db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == "Beef");
		if (carbonRef == null)
		{
			carbonRef = new EcoLens.Api.Models.CarbonReference { LabelName = "Beef", Category = CarbonCategory.Food, Co2Factor = 27m, Unit = "kgCO2" };
			db.CarbonReferences.Add(carbonRef);
			await db.SaveChangesAsync();
		}
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object, NullLogger<ActivityController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Upload(new CreateActivityLogDto { Label = "Beef", Quantity = 2, Unit = "kgCO2" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<ActivityLogResponseDto>(ok.Value);
		Assert.Equal("Beef", dto.Label);
		Assert.Equal(2, dto.Quantity);
		Assert.Equal(54m, dto.TotalEmission); // 2 * 27
		var log = await db.ActivityLogs.FirstOrDefaultAsync(l => l.UserId == user.Id);
		Assert.NotNull(log);
	}

	[Fact]
	public async Task Dashboard_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Dashboard(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Dashboard_ReturnsOk_WhenUserSet()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.Dashboard(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<DashboardDto>(ok.Value);
		Assert.NotNull(dto.WeeklyTrend);
		Assert.Equal(7, dto.WeeklyTrend.Count);
	}

	[Fact]
	public async Task MyLogs_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.MyLogs(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task MyLogs_ReturnsOkWithList_WhenUserSet()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var carbonRef = await db.CarbonReferences.FirstOrDefaultAsync();
		if (carbonRef == null)
		{
			carbonRef = new EcoLens.Api.Models.CarbonReference { LabelName = "Beef", Category = CarbonCategory.Food, Co2Factor = 27m, Unit = "kgCO2" };
			db.CarbonReferences.Add(carbonRef);
			await db.SaveChangesAsync();
		}
		db.ActivityLogs.Add(new ActivityLog { UserId = user.Id, CarbonReferenceId = carbonRef.Id, Quantity = 1, TotalEmission = 27m, DetectedLabel = "Beef" });
		await db.SaveChangesAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.MyLogs(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<ActivityLogResponseDto>>(ok.Value);
		Assert.Single(list);
	}

	[Fact]
	public async Task Stats_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Stats(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Stats_ReturnsOk_WhenUserSet()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.Stats(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<ActivityStatsDto>(ok.Value);
		Assert.True(dto.Rank >= 1);
	}

	[Fact]
	public async Task ChartData_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.ChartData(7, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task ChartData_ReturnsOkWithDaysItems_WhenUserSet()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.ChartData(7, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<ChartDataPointDto>>(ok.Value);
		Assert.Equal(7, list.Count());
	}

	[Fact]
	public async Task Heatmap_ReturnsOk()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, (await db.ApplicationUsers.FirstAsync()).Id);

		var result = await controller.Heatmap(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<RegionHeatmapDto>>(ok.Value);
		Assert.NotNull(list);
	}

	[Fact]
	public async Task DailyNetValue_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.CalculateDailyNetValueAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(0m);
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.DailyNetValue(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task DailyNetValue_ReturnsOkWithBreakdown_WhenUserSet()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.CalculateDailyNetValueAsync(user.Id, It.IsAny<DateTime>())).ReturnsAsync(1.5m);
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.DailyNetValue(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<DailyNetValueResponseDto>(ok.Value);
		Assert.Equal(1.5m, dto.Value);
		Assert.NotNull(dto.Breakdown);
		Assert.Equal(15.0m, dto.Breakdown.Benchmark);
	}

	[Fact]
	public async Task Stats_ReturnsNotFound_WhenUserMissing()
	{
		await using var db = await CreateDbAsync(false);
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, 99999);

		var result = await controller.Stats(CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Upload_ReturnsOk_WhenUtilityCategoryAndRegionMatch()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var carbonRef = await db.CarbonReferences.FirstOrDefaultAsync(c => c.Category == CarbonCategory.Utility);
		if (carbonRef == null)
		{
			carbonRef = new EcoLens.Api.Models.CarbonReference { LabelName = "Electricity", Category = CarbonCategory.Utility, Co2Factor = 0.5m, Unit = "kWh", Region = "SG" };
			db.CarbonReferences.Add(carbonRef);
			await db.SaveChangesAsync();
		}
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object, NullLogger<ActivityController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Upload(new CreateActivityLogDto { Label = carbonRef.LabelName, Category = CarbonCategory.Utility, Quantity = 10, Unit = carbonRef.Unit }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<ActivityLogResponseDto>(ok.Value);
		Assert.Equal(carbonRef.LabelName, dto.Label);
	}

	[Fact]
	public async Task ChartData_DefaultsTo7Days_WhenDaysZeroOrNegative()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockClimatiq = new Mock<IClimatiqService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new ActivityController(db, mockClimatiq.Object, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.ChartData(0, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<ChartDataPointDto>>(ok.Value);
		Assert.Equal(7, list.Count());
	}
}
