using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcoLens.Tests.Controllers;

public class MeControllerTests
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

	private static IWebHostEnvironment CreateMockEnv()
	{
		var mock = new Mock<IWebHostEnvironment>();
		return mock.Object;
	}

	[Fact]
	public async Task Get_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new MeController(db, CreateMockEnv());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Get(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsNotFound_WhenUserMissing()
	{
		await using var db = CreateDb(false);
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, 99999);

		var result = await controller.Get(CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsOkWithMeDto_WhenUserExists()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.Get(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<MeController.MeDto>(ok.Value);
		Assert.Equal(user.Id.ToString(), dto.Id);
		Assert.Equal("u1", dto.Name);
		Assert.Equal("u1@t.com", dto.Email);
	}

	[Fact]
	public async Task Update_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new MeController(db, CreateMockEnv());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Update(new MeController.UpdateMeRequest { Nickname = "n1" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Update_ReturnsConflict_WhenEmailInUse()
	{
		await using var db = CreateDb(true);
		db.ApplicationUsers.Add(new ApplicationUser
		{
			Username = "u2",
			Email = "other@t.com",
			PasswordHash = "x",
			Role = UserRole.User,
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20)
		});
		await db.SaveChangesAsync();
		var user = await db.ApplicationUsers.FirstAsync(u => u.Username == "u1");
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.Update(new MeController.UpdateMeRequest { Email = "other@t.com" }, CancellationToken.None);

		Assert.IsType<ConflictObjectResult>(result.Result);
	}

	[Fact]
	public async Task Update_ReturnsOkAndUpdates_WhenValid()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.Update(new MeController.UpdateMeRequest { Nickname = "newNick" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<MeController.MeDto>(ok.Value);
		Assert.Equal("newNick", dto.Nickname);
		var updated = await db.ApplicationUsers.FindAsync(user.Id);
		Assert.Equal("newNick", updated!.Nickname);
	}

	[Fact]
	public async Task ChangePassword_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new MeController(db, CreateMockEnv());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.ChangePassword(new MeController.ChangePasswordRequest { Password = "new" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task ChangePassword_ReturnsBadRequest_WhenPasswordEmpty()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, user.Id);

		var result = await controller.ChangePassword(new MeController.ChangePasswordRequest { Password = "   " }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Password required.", badRequest.Value);
	}

	[Fact]
	public async Task ChangePassword_ReturnsOk_WhenValid()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, user.Id);

		var result = await controller.ChangePassword(new MeController.ChangePasswordRequest { Password = "newPwd123" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var updated = await db.ApplicationUsers.FindAsync(user.Id);
		Assert.NotEqual("x", updated!.PasswordHash);
	}

	[Fact]
	public async Task UpdateAvatar_ReturnsBadRequest_WhenNoFile()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new MeController(db, CreateMockEnv());
		SetUser(controller, user.Id);

		var result = await controller.UpdateAvatar(null!, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No file uploaded.", badRequest.Value);
	}
}
