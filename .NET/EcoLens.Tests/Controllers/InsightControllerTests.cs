using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class InsightControllerTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new ApplicationDbContext(options);
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
	public async Task WeeklyReport_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb();
		var controller = new InsightController(db);
		// User with no NameIdentifier
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
		};

		var result = await controller.WeeklyReport(CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedResult>(result.Result);
		Assert.NotNull(unauthorized);
	}

	[Fact]
	public async Task WeeklyReport_ReturnsOkWithDto_WhenUserSet()
	{
		await using var db = CreateDb();
		var controller = new InsightController(db);
		SetUser(controller, 1);

		var result = await controller.WeeklyReport(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<EcoLens.Api.DTOs.Insights.AiInsightDto>(ok.Value);
		Assert.Equal(0, dto.Id);
		Assert.False(dto.IsRead);
		Assert.Contains("plant-based", dto.Content, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task WeeklyReport_ReturnsOkWithTypeWeeklyReport_WhenUserSet()
	{
		await using var db = CreateDb();
		var controller = new InsightController(db);
		SetUser(controller, 1);

		var result = await controller.WeeklyReport(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<EcoLens.Api.DTOs.Insights.AiInsightDto>(ok.Value);
		Assert.Equal(InsightType.WeeklyReport, dto.Type);
	}
}
