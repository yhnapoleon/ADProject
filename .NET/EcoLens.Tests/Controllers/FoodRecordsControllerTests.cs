using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EcoLens.Tests.Controllers;

public class FoodRecordsControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withUser = false, bool withRecord = false)
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
		if (withRecord)
		{
			var user = await db.ApplicationUsers.FirstAsync();
			db.FoodRecords.Add(new FoodRecord
			{
				UserId = user.Id,
				Name = "Rice",
				Amount = 0.3,
				EmissionFactor = 0.5m,
				Emission = 0.15m
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
	public async Task GetMyRecords_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var controller = new FoodRecordsController(db, NullLogger<FoodRecordsController>.Instance);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.GetMyRecords(null, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
		Assert.NotNull(unauthorized.Value);
	}

	[Fact]
	public async Task GetMyRecords_ReturnsOkWithPagedResult_WhenUserSet()
	{
		await using var db = await CreateDbAsync(withUser: true, withRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new FoodRecordsController(db, NullLogger<FoodRecordsController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.GetMyRecords(null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var paged = Assert.IsType<EcoLens.Api.DTOs.Travel.PagedResultDto<FoodRecordResponseDto>>(ok.Value);
		Assert.Equal(1, paged.TotalCount);
		Assert.Single(paged.Items);
		Assert.Equal("Rice", paged.Items[0].Name);
		Assert.Equal(0.15m, paged.Items[0].Emission);
	}

	[Fact]
	public async Task GetById_ReturnsNotFound_WhenRecordMissing()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new FoodRecordsController(db, NullLogger<FoodRecordsController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.GetById(999, CancellationToken.None);

		Assert.IsType<NotFoundObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetById_ReturnsOk_WhenRecordExists()
	{
		await using var db = await CreateDbAsync(withUser: true, withRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var record = await db.FoodRecords.FirstAsync();
		var controller = new FoodRecordsController(db, NullLogger<FoodRecordsController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.GetById(record.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodRecordResponseDto>(ok.Value);
		Assert.Equal(record.Id, dto.Id);
		Assert.Equal("Rice", dto.Name);
	}

	[Fact]
	public async Task Delete_ReturnsNotFound_WhenRecordMissing()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new FoodRecordsController(db, NullLogger<FoodRecordsController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Delete(999, CancellationToken.None);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task Delete_ReturnsOkAndRemovesRecord_WhenRecordExists()
	{
		await using var db = await CreateDbAsync(withUser: true, withRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var record = await db.FoodRecords.FirstAsync();
		var controller = new FoodRecordsController(db, NullLogger<FoodRecordsController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Delete(record.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result);
		var deleted = await db.FoodRecords.FirstOrDefaultAsync(r => r.Id == record.Id);
		Assert.Null(deleted);
	}
}
