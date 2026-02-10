using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcoLens.Tests.Controllers;

public class AdminAuthControllerTests
{
	private static ApplicationDbContext CreateDb(bool withAdmin = false, string? adminPassword = null)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withAdmin && adminPassword != null)
		{
			db.ApplicationUsers.Add(new ApplicationUser
			{
				Username = "admin",
				Email = "admin@test.com",
				PasswordHash = PasswordHasher.Hash(adminPassword),
				Role = UserRole.Admin,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-30),
				IsActive = true
			});
			db.SaveChanges();
		}
		return db;
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
	{
		await using var db = CreateDb();
		var mockAuth = new Mock<IAuthService>();
		var controller = new AdminAuthController(db, mockAuth.Object);

		var result = await controller.Login(new AdminAuthController.AdminLoginRequest { Username = "nobody", Password = "any" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
		mockAuth.Verify(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenUserBanned()
	{
		await using var db = CreateDb(withAdmin: true, adminPassword: "pwd");
		var user = await db.ApplicationUsers.FirstAsync(u => u.Username == "admin");
		user.IsActive = false;
		await db.SaveChangesAsync();
		var mockAuth = new Mock<IAuthService>();
		var controller = new AdminAuthController(db, mockAuth.Object);

		var result = await controller.Login(new AdminAuthController.AdminLoginRequest { Username = "admin", Password = "pwd" }, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
		Assert.Equal("User is banned.", unauthorized.Value);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenNotAdmin()
	{
		await using var db = CreateDb();
		db.ApplicationUsers.Add(new ApplicationUser
		{
			Username = "user1",
			Email = "u@t.com",
			PasswordHash = PasswordHasher.Hash("pwd"),
			Role = UserRole.User,
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20),
			IsActive = true
		});
		await db.SaveChangesAsync();
		var mockAuth = new Mock<IAuthService>();
		var controller = new AdminAuthController(db, mockAuth.Object);

		var result = await controller.Login(new AdminAuthController.AdminLoginRequest { Username = "user1", Password = "pwd" }, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
		Assert.Equal("Not an admin.", unauthorized.Value);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenWrongPassword()
	{
		await using var db = CreateDb(withAdmin: true, adminPassword: "correct");
		var mockAuth = new Mock<IAuthService>();
		var controller = new AdminAuthController(db, mockAuth.Object);

		var result = await controller.Login(new AdminAuthController.AdminLoginRequest { Username = "admin", Password = "wrong" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
		mockAuth.Verify(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
	}

	[Fact]
	public async Task Login_ReturnsOkWithToken_WhenValid()
	{
		await using var db = CreateDb(withAdmin: true, adminPassword: "secret");
		var user = await db.ApplicationUsers.FirstAsync(u => u.Username == "admin");
		var mockAuth = new Mock<IAuthService>();
		mockAuth.Setup(x => x.GenerateTokenAsync(user.Id.ToString(), It.IsAny<IDictionary<string, string>>()))
			.ReturnsAsync("fake-jwt-token");
		var controller = new AdminAuthController(db, mockAuth.Object);

		var result = await controller.Login(new AdminAuthController.AdminLoginRequest { Username = "admin", Password = "secret" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<AdminAuthController.AdminLoginResponse>(ok.Value);
		Assert.Equal("fake-jwt-token", body.AccessToken);
		Assert.Equal("fake-jwt-token", body.Token);
		Assert.Equal(3600, body.ExpiresIn);
		Assert.NotNull(body.Admin);
		mockAuth.Verify(x => x.GenerateTokenAsync(user.Id.ToString(), It.IsAny<IDictionary<string, string>>()), Times.Once);
	}

	[Fact]
	public async Task LoginAlias_DelegatesToLogin()
	{
		await using var db = CreateDb(withAdmin: true, adminPassword: "p");
		var user = await db.ApplicationUsers.FirstAsync(u => u.Username == "admin");
		var mockAuth = new Mock<IAuthService>();
		mockAuth.Setup(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>())).ReturnsAsync("token");
		var controller = new AdminAuthController(db, mockAuth.Object);

		var result = await controller.LoginAlias(new AdminAuthController.AdminLoginRequest { Username = "admin", Password = "p" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<AdminAuthController.AdminLoginResponse>(ok.Value);
		Assert.Equal("token", body.AccessToken);
	}
}
