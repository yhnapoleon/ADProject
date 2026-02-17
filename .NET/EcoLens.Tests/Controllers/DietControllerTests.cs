using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Diet;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EcoLens.Tests.Controllers;

public class DietControllerTests
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
			db.DietRecords.Add(new DietRecord
			{
				UserId = user.Id,
				Name = "Salad",
				Amount = 0.2,
				EmissionFactor = 0.4m,
				Emission = 0.08m
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
	public async Task Create_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var controller = new DietController(db, NullLogger<DietController>.Instance);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Create(new CreateDietRecordDto { Name = "X", Amount = 0.1, EmissionFactor = 0.5m }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Create_ReturnsOkAndSavesRecord_WhenValid()
	{
		await using var db = await CreateDbAsync(withUser: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new DietController(db, NullLogger<DietController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Create(new CreateDietRecordDto { Name = "Pasta", Amount = 0.25, EmissionFactor = 1.2m }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<DietRecordResponseDto>(ok.Value);
		Assert.Equal("Pasta", dto.Name);
		Assert.Equal(0.3m, dto.Emission); // 0.25 * 1.2
		var saved = await db.DietRecords.FirstOrDefaultAsync(r => r.UserId == user.Id);
		Assert.NotNull(saved);
		Assert.Equal("Pasta", saved.Name);
	}

	[Fact]
	public async Task GetMyDiets_ReturnsOkWithPagedResult_WhenUserSet()
	{
		await using var db = await CreateDbAsync(withUser: true, withRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new DietController(db, NullLogger<DietController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.GetMyDiets(null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var paged = Assert.IsType<EcoLens.Api.DTOs.Travel.PagedResultDto<DietRecordResponseDto>>(ok.Value);
		Assert.Equal(1, paged.TotalCount);
		Assert.Single(paged.Items);
		Assert.Equal("Salad", paged.Items[0].Name);
	}

	[Fact]
	public async Task GetById_ReturnsNotFound_WhenRecordMissing()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new DietController(db, NullLogger<DietController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.GetById(999, CancellationToken.None);

		Assert.IsType<NotFoundObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetById_ReturnsOk_WhenRecordExists()
	{
		await using var db = await CreateDbAsync(withUser: true, withRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var record = await db.DietRecords.FirstAsync();
		var controller = new DietController(db, NullLogger<DietController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.GetById(record.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<DietRecordResponseDto>(ok.Value);
		Assert.Equal("Salad", dto.Name);
	}

	[Fact]
	public async Task Delete_ReturnsOkAndRemovesRecord_WhenRecordExists()
	{
		await using var db = await CreateDbAsync(withUser: true, withRecord: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var record = await db.DietRecords.FirstAsync();
		var controller = new DietController(db, NullLogger<DietController>.Instance);
		SetUser(controller, user.Id);

		var result = await controller.Delete(record.Id, CancellationToken.None);

		Assert.IsType<OkObjectResult>(result);
		var deleted = await db.DietRecords.FirstOrDefaultAsync(r => r.Id == record.Id);
		Assert.Null(deleted);
	}
}
