using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class AboutControllerTests
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
	public async Task Get_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var controller = new AboutController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Get(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsOkWith12Months_WhenUserExists()
	{
		await using var db = await CreateDbAsync(withUser: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new AboutController(db);
		SetUser(controller, user.Id);

		var result = await controller.Get(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<AboutController.MonthlyEmissionDto>>(ok.Value).ToList();
		Assert.Equal(12, list.Count);
		foreach (var m in list)
		{
			Assert.Equal(m.Food + m.Transport + m.Utility, m.EmissionsTotal);
			Assert.True(m.Month.Length == 7 && m.Month[4] == '-'); // yyyy-MM
		}
	}

	[Fact]
	public async Task Get_ReturnsOkWithZeroEmissions_WhenUserHasNoActivity()
	{
		await using var db = await CreateDbAsync(withUser: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new AboutController(db);
		SetUser(controller, user.Id);

		var result = await controller.Get(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<AboutController.MonthlyEmissionDto>>(ok.Value).ToList();
		Assert.Equal(12, list.Count);
		Assert.All(list, m => Assert.Equal(0, m.EmissionsTotal));
		Assert.All(list, m => Assert.Equal(0, m.AverageAllUsers));
	}
}
