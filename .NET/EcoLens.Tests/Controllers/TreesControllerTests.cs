using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using EcoLens.Api.Services;

namespace EcoLens.Tests.Controllers;

public class TreesControllerTests
{
	/// <summary>与 TreesController 的「今天」一致（新加坡时区或 UTC），保证 CI/本地都能查到今日步数。</summary>
	private static DateTime GetTodayForSteps()
	{
		try
		{
			var tz = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
			return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
		}
		catch
		{
			return DateTime.UtcNow.Date;
		}
	}

	private static ApplicationDbContext CreateDb(bool withUser = false, int? todaySteps = null)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withUser)
		{
			var user = new ApplicationUser
			{
				Username = "u1",
				Email = "u1@test.com",
				PasswordHash = "x",
				Role = UserRole.User,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-20),
				TreesTotalCount = 2,
				CurrentTreeProgress = 50,
				StepsUsedToday = 0,
				LastStepUsageDate = null
			};
			db.ApplicationUsers.Add(user);
			db.SaveChanges();
			if (todaySteps.HasValue && todaySteps.Value > 0)
			{
				var today = GetTodayForSteps();
				db.StepRecords.Add(new StepRecord
				{
					UserId = user.Id,
					StepCount = todaySteps.Value,
					RecordDate = today,
					CarbonOffset = todaySteps.Value * 0.0001m
				});
				db.SaveChanges();
			}
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
	public async Task GetTreeAlias_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.GetTreeAlias(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task GetTreeAlias_ReturnsNotFound_WhenUserNotInDb()
	{
		await using var db = CreateDb(false);
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, 999);

		var result = await controller.GetTreeAlias(CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task GetTreeAlias_ReturnsOkWithState_WhenUserExists()
	{
		await using var db = CreateDb(withUser: true, todaySteps: 1000);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.GetTreeAlias(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var anonymous = ok.Value;
		var totalTrees = anonymous.GetType().GetProperty("totalTrees")?.GetValue(anonymous);
		var currentProgress = anonymous.GetType().GetProperty("currentProgress")?.GetValue(anonymous);
		var todaySteps = anonymous.GetType().GetProperty("todaySteps")?.GetValue(anonymous);
		var availableSteps = anonymous.GetType().GetProperty("availableSteps")?.GetValue(anonymous);
		Assert.Equal(2, totalTrees);
		Assert.Equal(50, currentProgress);
		Assert.Equal(1000, todaySteps);
		Assert.True(Convert.ToInt32(availableSteps) >= 0);
	}

	[Fact]
	public async Task GetState_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.GetState(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task GetState_ReturnsOkWithState_WhenUserExists()
	{
		await using var db = CreateDb(withUser: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.GetState(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var anonymous = ok.Value;
		var status = anonymous.GetType().GetProperty("status")?.GetValue(anonymous) as string;
		Assert.NotNull(status);
		Assert.True(status == "idle" || status == "growing" || status == "completed");
	}

	[Fact]
	public async Task UpdateState_ReturnsBadRequest_WhenBothFieldsNull()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.UpdateState(new TreesController.UpdateTreeStateRequest(), CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
	}

	[Fact]
	public async Task UpdateState_ReturnsBadRequest_WhenTotalTreesNegative()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.UpdateState(new TreesController.UpdateTreeStateRequest { TotalTrees = -1 }, CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task UpdateState_ReturnsBadRequest_WhenCurrentProgressOutOfRange()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.UpdateState(new TreesController.UpdateTreeStateRequest { CurrentProgress = 101 }, CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task UpdateState_ReturnsOkAndUpdatesUser_WhenValid()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		pointService.Setup(x => x.AwardTreePlantingPointsAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.UpdateState(new TreesController.UpdateTreeStateRequest { TotalTrees = 5, CurrentProgress = 80 }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var updated = await db.ApplicationUsers.FirstAsync(u => u.Id == user.Id);
		Assert.Equal(5, updated.TreesTotalCount);
		Assert.Equal(80, updated.CurrentTreeProgress);
		pointService.Verify(x => x.AwardTreePlantingPointsAsync(user.Id, 3), Times.Once); // 5 - 2 = 3 new trees
	}

	[Fact]
	public async Task PostTreeAlias_ReturnsOkWithUsedSteps_WhenUsedStepsProvided()
	{
		await using var db = CreateDb(withUser: true, todaySteps: 5000);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.PostTreeAlias(new TreesController.PostTreeRequest { UsedSteps = 1000 }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var usedSteps = ok.Value.GetType().GetProperty("usedSteps")?.GetValue(ok.Value);
		Assert.Equal(1000, usedSteps);
		var updated = await db.ApplicationUsers.FirstAsync(u => u.Id == user.Id);
		Assert.Equal(1000, updated.StepsUsedToday);
	}

	[Fact]
	public async Task Stats_ReturnsOk_WhenUserExists()
	{
		await using var db = CreateDb(withUser: true, todaySteps: 2000);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.Stats(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var todaySteps = ok.Value.GetType().GetProperty("todaySteps")?.GetValue(ok.Value);
		Assert.Equal(2000, todaySteps);
	}

	[Fact]
	public async Task Grow_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(withUser: true, todaySteps: 500);
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Grow(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Grow_ReturnsNotFound_WhenUserNotInDb()
	{
		await using var db = CreateDb(false);
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, 999);

		var result = await controller.Grow(CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Grow_ReturnsBadRequest_WhenNoStepsAvailable()
	{
		await using var db = CreateDb(withUser: true, todaySteps: 0);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.Grow(CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
		var msg = badRequest.Value!.GetType().GetProperty("message")?.GetValue(badRequest.Value)?.ToString();
		Assert.Contains("No steps available", msg!, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Grow_ReturnsOkWithProgress_WhenStepsAvailable()
	{
		await using var db = CreateDb(withUser: true, todaySteps: 300);
		var user = await db.ApplicationUsers.FirstAsync();
		var pointService = new Mock<IPointService>();
		var controller = new TreesController(db, pointService.Object);
		SetUser(controller, user.Id);

		var result = await controller.Grow(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var treesTotal = ok.Value!.GetType().GetProperty("TreesTotalCount")?.GetValue(ok.Value);
		var currentProgress = ok.Value.GetType().GetProperty("CurrentTreeProgress")?.GetValue(ok.Value);
		var availableSteps = ok.Value.GetType().GetProperty("AvailableSteps")?.GetValue(ok.Value);
		Assert.NotNull(treesTotal);
		Assert.NotNull(currentProgress);
		Assert.Equal(0, availableSteps); // 全部消耗后可用为 0
		var updated = await db.ApplicationUsers.FirstAsync(u => u.Id == user.Id);
		Assert.Equal(300, updated.StepsUsedToday);
	}
}
