using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.CarbonEmission;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EcoLens.Tests.Controllers;

public class CarbonEmissionControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withUser = false, bool withFoodRecord = false, bool withTravelLog = false, bool withUtilityBill = false)
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
		if (withFoodRecord)
		{
			var user = await db.ApplicationUsers.FirstAsync();
			db.FoodRecords.Add(new FoodRecord { UserId = user.Id, Name = "Rice", Amount = 0.3, EmissionFactor = 0.5m, Emission = 0.15m });
			await db.SaveChangesAsync();
		}
		if (withTravelLog)
		{
			var user = await db.ApplicationUsers.FirstAsync();
			db.TravelLogs.Add(new TravelLog { UserId = user.Id, CarbonEmission = 1.5m });
			await db.SaveChangesAsync();
		}
		if (withUtilityBill)
		{
			var user = await db.ApplicationUsers.FirstAsync();
			db.UtilityBills.Add(new UtilityBill { UserId = user.Id, TotalCarbonEmission = 2m, BillPeriodEnd = DateTime.UtcNow });
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
	public async Task BatchDeleteTyped_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new CarbonEmissionController(db, mockPoint.Object, NullLogger<CarbonEmissionController>.Instance);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.BatchDeleteTyped(new List<BatchDeleteItemDto> { new() { Type = 1, Id = 1 } }, CancellationToken.None);

		Assert.IsType<UnauthorizedObjectResult>(result.Result);
	}

	[Fact]
	public async Task BatchDeleteTyped_ReturnsBadRequest_WhenItemsEmpty()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockPoint = new Mock<IPointService>();
		var controller = new CarbonEmissionController(db, mockPoint.Object, NullLogger<CarbonEmissionController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.BatchDeleteTyped(new List<BatchDeleteItemDto>(), CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
	}

	[Fact]
	public async Task BatchDeleteTyped_ReturnsBadRequest_WhenNoValidTypes()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockPoint = new Mock<IPointService>();
		var controller = new CarbonEmissionController(db, mockPoint.Object, NullLogger<CarbonEmissionController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.BatchDeleteTyped(new List<BatchDeleteItemDto> { new() { Type = 99, Id = 1 } }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
	}

	[Fact]
	public async Task BatchDeleteTyped_ReturnsOkAndDeletes_WhenValidItems()
	{
		await using var db = await CreateDbAsync(withUser: true, withFoodRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var foodRecord = await db.FoodRecords.FirstAsync();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new CarbonEmissionController(db, mockPoint.Object, NullLogger<CarbonEmissionController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.BatchDeleteTyped(new List<BatchDeleteItemDto> { new() { Type = 1, Id = foodRecord.Id } }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<BatchDeleteTypedResponseDto>(ok.Value);
		Assert.Equal(1, dto.FoodRecordsDeleted);
		Assert.Equal(1, dto.TotalDeleted);
		var stillThere = await db.FoodRecords.AnyAsync(f => f.Id == foodRecord.Id);
		Assert.False(stillThere);
	}
}
