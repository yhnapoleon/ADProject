using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class MainPageControllerTests
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
	public async Task Get_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new MainPageController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Get(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsOkWithStats_WhenUserExists()
	{
		await using var db = CreateDb(withUser: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new MainPageController(db);
		SetUser(controller, user.Id);

		var result = await controller.Get(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<MainPageController.MainPageStatsDto>(ok.Value);
		Assert.Equal(dto.Food + dto.Transport + dto.Utility, dto.Total);
		Assert.True(dto.Total >= 0);
	}
}
