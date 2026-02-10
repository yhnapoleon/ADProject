using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Steps;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcoLens.Tests.Controllers;

public class StepControllerTests
{
	private static ApplicationDbContext CreateDb(bool withUser = false)
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
			db.SaveChanges();
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
	public async Task Sync_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var mockPoint = new Mock<IPointService>();
		var controller = new StepController(db, mockPoint.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Sync(new SyncStepsRequestDto { StepCount = 1000, Date = DateTime.UtcNow.Date }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Sync_ReturnsBadRequest_WhenStepCountNegative()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockPoint = new Mock<IPointService>();
		var controller = new StepController(db, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.Sync(new SyncStepsRequestDto { StepCount = -1, Date = DateTime.UtcNow.Date }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("StepCount must be non-negative.", badRequest.Value);
	}

	[Fact]
	public async Task Sync_ReturnsNotFound_WhenUserMissing()
	{
		await using var db = CreateDb(false); // no user
		var mockPoint = new Mock<IPointService>();
		var controller = new StepController(db, mockPoint.Object);
		SetUser(controller, 99999);

		var result = await controller.Sync(new SyncStepsRequestDto { StepCount = 1000, Date = DateTime.UtcNow.Date }, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Sync_ReturnsOkWithResponse_WhenNewRecord()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.LogPointAwardAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), "Step")).Returns(Task.CompletedTask);
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new StepController(db, mockPoint.Object);
		SetUser(controller, user.Id);
		var date = DateTime.UtcNow.Date;

		var result = await controller.Sync(new SyncStepsRequestDto { StepCount = 5000, Date = date }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<SyncStepsResponseDto>(ok.Value);
		Assert.Equal(5000, dto.TotalSteps);
		Assert.True(dto.AvailableSteps >= 0);
		var record = await db.StepRecords.FirstOrDefaultAsync(r => r.UserId == user.Id && r.RecordDate == date);
		Assert.NotNull(record);
		Assert.Equal(5000, record.StepCount);
	}

	[Fact]
	public async Task Sync_ReturnsOkAndUpdates_WhenExistingRecord()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var date = DateTime.UtcNow.Date;
		db.StepRecords.Add(new StepRecord { UserId = user.Id, StepCount = 1000, RecordDate = date, CarbonOffset = 0.1m });
		await db.SaveChangesAsync();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.LogPointAwardAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), "Step")).Returns(Task.CompletedTask);
		mockPoint.Setup(x => x.RecalculateTotalCarbonSavedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new StepController(db, mockPoint.Object);
		SetUser(controller, user.Id);

		var result = await controller.Sync(new SyncStepsRequestDto { StepCount = 3000, Date = date }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<SyncStepsResponseDto>(ok.Value);
		Assert.Equal(3000, dto.TotalSteps);
		var record = await db.StepRecords.FirstAsync(r => r.UserId == user.Id && r.RecordDate == date);
		Assert.Equal(3000, record.StepCount);
	}
}
