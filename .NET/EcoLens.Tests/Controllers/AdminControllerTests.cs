using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Admin;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class AdminControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withAdmin = false, bool withUser = false)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withAdmin)
		{
			db.ApplicationUsers.Add(new ApplicationUser
			{
				Username = "admin1",
				Email = "admin@t.com",
				PasswordHash = "x",
				Role = UserRole.Admin,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-30),
				IsActive = true
			});
			await db.SaveChangesAsync();
		}
		if (withUser)
		{
			db.ApplicationUsers.Add(new ApplicationUser
			{
				Username = "u1",
				Email = "u1@t.com",
				PasswordHash = "x",
				Role = UserRole.User,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-20),
				IsActive = true
			});
			await db.SaveChangesAsync();
		}
		return db;
	}

	private static void SetUser(ControllerBase controller, int userId, string role = "User")
	{
		var identity = new ClaimsIdentity();
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
		identity.AddClaim(new Claim(ClaimTypes.Role, role));
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
		};
	}

	[Fact]
	public async Task GetCarbonReferences_ReturnsOk_WhenAdmin()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.GetCarbonReferences(null, null, null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonReferenceDto>>(ok.Value);
		Assert.NotNull(list);
	}

	[Fact]
	public async Task GetCarbonReferences_FiltersByCategory_WhenProvided()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		if (!await db.CarbonReferences.AnyAsync())
		{
			db.CarbonReferences.Add(new EcoLens.Api.Models.CarbonReference { LabelName = "F1", Category = CarbonCategory.Food, Co2Factor = 1m, Unit = "kg" });
			await db.SaveChangesAsync();
		}
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.GetCarbonReferences("Food", null, null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CarbonReferenceDto>>(ok.Value);
		Assert.All(list, c => Assert.Equal(CarbonCategory.Food, c.Category));
	}

	[Fact]
	public async Task UpsertCarbonReference_ReturnsBadRequest_WhenLabelNameEmpty()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.UpsertCarbonReference(new UpsertCarbonReferenceDto { LabelName = "", Unit = "kg" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("LabelName and Unit are required.", badRequest.Value);
	}

	[Fact]
	public async Task UpsertCarbonReference_ReturnsOk_WhenValidNew()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.UpsertCarbonReference(new UpsertCarbonReferenceDto
		{
			LabelName = "TestFood",
			Category = CarbonCategory.Food,
			Co2Factor = 1.5m,
			Unit = "kg"
		}, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<CarbonReferenceDto>(ok.Value);
		Assert.Equal("TestFood", dto.LabelName);
		Assert.True(dto.Id > 0);
	}

	[Fact]
	public async Task DeleteCarbonReference_ReturnsNotFound_WhenIdInvalid()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.DeleteCarbonReference(99999, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task DeleteCarbonReference_ReturnsNoContent_WhenDeleted()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var refToDelete = await db.CarbonReferences.OrderBy(c => c.Id).FirstOrDefaultAsync();
		if (refToDelete == null)
		{
			refToDelete = new EcoLens.Api.Models.CarbonReference { LabelName = "ToDelete", Category = CarbonCategory.Food, Co2Factor = 1m, Unit = "kg" };
			db.CarbonReferences.Add(refToDelete);
			await db.SaveChangesAsync();
		}
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.DeleteCarbonReference(refToDelete.Id, CancellationToken.None);

		Assert.IsType<NoContentResult>(result);
		var gone = await db.CarbonReferences.AnyAsync(c => c.Id == refToDelete.Id);
		Assert.False(gone);
	}

	[Fact]
	public async Task BanUser_ReturnsUnauthorized_WhenNoUser()
	{
		await using var db = await CreateDbAsync(withAdmin: true, withUser: true);
		var user = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.User);
		var controller = new AdminController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.BanUser(user.Id, new BanUserRequestDto { Ban = true }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result);
	}

	[Fact]
	public async Task BanUser_ReturnsBadRequest_WhenBanningSelf()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.BanUser(admin.Id, new BanUserRequestDto { Ban = true }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.NotNull(badRequest.Value);
	}

	[Fact]
	public async Task BanUser_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync(withAdmin: true, withUser: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var user = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.User);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.BanUser(user.Id, new BanUserRequestDto { Ban = true }, CancellationToken.None);

		Assert.IsType<OkResult>(result);
		var updated = await db.ApplicationUsers.FindAsync(user.Id);
		Assert.False(updated!.IsActive);
	}

	[Fact]
	public async Task GetSettings_ReturnsOk_WhenAdmin()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.GetSettings(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<AdminController.SettingsDto>(ok.Value);
		Assert.True(dto.ConfidenceThreshold >= 0);
	}

	[Fact]
	public async Task GetDatabaseStatistics_ReturnsOk_WhenAdmin()
	{
		await using var db = await CreateDbAsync(withAdmin: true);
		var admin = await db.ApplicationUsers.FirstAsync(u => u.Role == UserRole.Admin);
		var controller = new AdminController(db);
		SetUser(controller, admin.Id, "Admin");

		var result = await controller.GetDatabaseStatistics(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
	}
}
