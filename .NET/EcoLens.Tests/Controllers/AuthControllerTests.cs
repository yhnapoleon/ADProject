using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Auth;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Utilities;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcoLens.Tests.Controllers;

public class AuthControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withUser = false, string? email = "u1@t.com", string? username = "u1")
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withUser)
		{
			db.ApplicationUsers.Add(new ApplicationUser
			{
				Username = username ?? "u1",
				Email = email ?? "u1@t.com",
				PasswordHash = PasswordHasher.Hash("pass123"),
				Role = UserRole.User,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-20),
				IsActive = true
			});
			await db.SaveChangesAsync();
		}
		return db;
	}

	[Fact]
	public async Task Register_ReturnsBadRequest_WhenSensitiveWord()
	{
		await using var db = await CreateDbAsync(false);
		var mockAuth = new Mock<IAuthService>();
		var mockSensitive = new Mock<ISensitiveWordService>();
		mockSensitive.Setup(x => x.ContainsSensitiveWord(It.IsAny<string>())).Returns("badword");
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);

		var result = await controller.Register(new RegisterRequestDto
		{
			Username = "baduser",
			Email = "a@b.com",
			Password = "pwd",
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20)
		}, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
		mockAuth.Verify(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
	}

	[Fact]
	public async Task Register_ReturnsConflict_WhenUserExists()
	{
		await using var db = await CreateDbAsync(true, "u1@t.com", "u1");
		var mockAuth = new Mock<IAuthService>();
		var mockSensitive = new Mock<ISensitiveWordService>();
		mockSensitive.Setup(x => x.ContainsSensitiveWord(It.IsAny<string>())).Returns((string?)null);
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);

		var result = await controller.Register(new RegisterRequestDto
		{
			Username = "u1",
			Email = "u1@t.com",
			Password = "pwd",
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20)
		}, CancellationToken.None);

		Assert.IsType<ConflictObjectResult>(result.Result);
	}

	[Fact]
	public async Task Register_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync(false);
		var mockAuth = new Mock<IAuthService>();
		mockAuth.Setup(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>())).ReturnsAsync("jwt-token");
		var mockSensitive = new Mock<ISensitiveWordService>();
		mockSensitive.Setup(x => x.ContainsSensitiveWord(It.IsAny<string>())).Returns((string?)null);
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } }
		};

		var result = await controller.Register(new RegisterRequestDto
		{
			Username = "newuser",
			Email = "new@b.com",
			Password = "pwd",
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20)
		}, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var response = Assert.IsType<AuthResponseDto>(ok.Value);
		Assert.Equal("jwt-token", response.Token);
		Assert.Equal("newuser", response.User.Username);
		var user = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == "new@b.com");
		Assert.NotNull(user);
	}

	[Fact]
	public async Task Login_ReturnsBadRequest_WhenEmailEmpty()
	{
		await using var db = await CreateDbAsync(false);
		var mockAuth = new Mock<IAuthService>();
		var mockSensitive = new Mock<ISensitiveWordService>();
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);

		var result = await controller.Login(new LoginRequestDto { Email = "   ", Password = "pwd" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Email is required.", badRequest.Value);
	}

	[Fact]
	public async Task Login_ReturnsNotFound_WhenNoAccount()
	{
		await using var db = await CreateDbAsync(false);
		var mockAuth = new Mock<IAuthService>();
		var mockSensitive = new Mock<ISensitiveWordService>();
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);

		var result = await controller.Login(new LoginRequestDto { Email = "nonexistent@t.com", Password = "pwd" }, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
		Assert.Equal("No account found with this email.", notFound.Value);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenBanned()
	{
		await using var db = await CreateDbAsync(true, "u1@t.com", "u1");
		var user = await db.ApplicationUsers.FirstAsync();
		user.IsActive = false;
		await db.SaveChangesAsync();
		var mockAuth = new Mock<IAuthService>();
		var mockSensitive = new Mock<ISensitiveWordService>();
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);

		var result = await controller.Login(new LoginRequestDto { Email = "u1@t.com", Password = "pass123" }, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
		Assert.Equal("User is banned.", unauthorized.Value);
	}

	[Fact]
	public async Task Login_ReturnsUnauthorized_WhenWrongPassword()
	{
		await using var db = await CreateDbAsync(true, "u1@t.com", "u1");
		var mockAuth = new Mock<IAuthService>();
		var mockSensitive = new Mock<ISensitiveWordService>();
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);

		var result = await controller.Login(new LoginRequestDto { Email = "u1@t.com", Password = "wrong" }, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
		Assert.Equal("Invalid credentials.", unauthorized.Value);
	}

	[Fact]
	public async Task Login_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync(true, "u1@t.com", "u1");
		var mockAuth = new Mock<IAuthService>();
		mockAuth.Setup(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>())).ReturnsAsync("jwt-token");
		var mockSensitive = new Mock<ISensitiveWordService>();
		var controller = new AuthController(db, mockAuth.Object, mockSensitive.Object);
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } }
		};

		var result = await controller.Login(new LoginRequestDto { Email = "u1@t.com", Password = "pass123" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var response = Assert.IsType<AuthResponseDto>(ok.Value);
		Assert.Equal("jwt-token", response.Token);
		Assert.Equal("u1", response.User.Username);
	}
}
