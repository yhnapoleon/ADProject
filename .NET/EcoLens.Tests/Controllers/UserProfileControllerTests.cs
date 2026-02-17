using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.User;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcoLens.Tests.Controllers;

public class UserProfileControllerTests
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
				PasswordHash = PasswordHasher.Hash("oldpwd"),
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

	private static IMemoryCache CreateCache()
	{
		return new MemoryCache(new Microsoft.Extensions.Options.OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
	}

	[Fact]
	public async Task GetProfile_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new UserProfileController(db, CreateCache());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.GetProfile(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task GetProfile_ReturnsNotFound_WhenUserMissing()
	{
		await using var db = CreateDb(false);
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, 99999);

		var result = await controller.GetProfile(CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task GetProfile_ReturnsOkWithDto_WhenUserExists()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.GetProfile(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<UserProfileController.UserProfileResponseDto>(ok.Value);
		Assert.Equal(user.Id.ToString(), dto.Id);
		Assert.Equal("u1", dto.Name);
		Assert.Equal("u1@t.com", dto.Email);
	}

	[Fact]
	public async Task UpdateProfile_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new UserProfileController(db, CreateCache());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.UpdateProfile(new UpdateUserProfileDto { Nickname = "n1" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task UpdateProfile_ReturnsBadRequest_WhenInvalidBirthDate()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.UpdateProfile(new UpdateUserProfileDto { BirthDate = "not-a-date" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
	}

	[Fact]
	public async Task UpdateProfile_ReturnsConflict_WhenEmailInUse()
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
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.UpdateProfile(new UpdateUserProfileDto { Email = "other@t.com" }, CancellationToken.None);

		Assert.IsType<ConflictObjectResult>(result.Result);
	}

	[Fact]
	public async Task UpdateProfile_ReturnsOk_WhenValid()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.UpdateProfile(new UpdateUserProfileDto { Nickname = "newNick" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<UserProfileController.UserProfileResponseDto>(ok.Value);
		Assert.Equal("newNick", dto.Nickname);
	}

	[Fact]
	public async Task ChangePassword_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new UserProfileController(db, CreateCache());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.ChangePassword(new ChangePasswordRequestDto { OldPassword = "old", NewPassword = "new" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result);
	}

	[Fact]
	public async Task ChangePassword_ReturnsBadRequest_WhenMissingPasswords()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);

		var result = await controller.ChangePassword(new ChangePasswordRequestDto { OldPassword = "", NewPassword = "new" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal("OldPassword and NewPassword are required.", badRequest.Value);
	}

	[Fact]
	public async Task ChangePassword_ReturnsUnauthorized_WhenWrongOldPassword()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);

		var result = await controller.ChangePassword(new ChangePasswordRequestDto { OldPassword = "wrong", NewPassword = "new" }, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
		Assert.Equal("Old password is incorrect.", unauthorized.Value);
	}

	[Fact]
	public async Task ChangePassword_ReturnsOk_WhenValid()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);

		var result = await controller.ChangePassword(new ChangePasswordRequestDto { OldPassword = "oldpwd", NewPassword = "newpwd" }, CancellationToken.None);

		Assert.IsType<OkResult>(result);
		var updated = await db.ApplicationUsers.FindAsync(user.Id);
		Assert.NotEqual(PasswordHasher.Hash("oldpwd"), updated!.PasswordHash);
	}

	[Fact]
	public async Task VerifyPassword_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = CreateDb(true);
		var controller = new UserProfileController(db, CreateCache());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.VerifyPassword(new VerifyPasswordRequestDto { OldPassword = "x" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task VerifyPassword_ReturnsBadRequest_WhenOldPasswordEmpty()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);

		var result = await controller.VerifyPassword(new VerifyPasswordRequestDto { OldPassword = "   " }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("OldPassword is required.", badRequest.Value);
	}

	[Fact]
	public async Task VerifyPassword_ReturnsOkWithValidTrue_WhenCorrect()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);

		var result = await controller.VerifyPassword(new VerifyPasswordRequestDto { OldPassword = "oldpwd" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<VerifyPasswordResponseDto>(ok.Value);
		Assert.True(dto.Valid);
	}

	[Fact]
	public async Task VerifyPassword_ReturnsOkWithValidFalse_WhenIncorrect()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		SetUser(controller, user.Id);

		var result = await controller.VerifyPassword(new VerifyPasswordRequestDto { OldPassword = "wrong" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<VerifyPasswordResponseDto>(ok.Value);
		Assert.False(dto.Valid);
	}

	[Fact]
	public async Task GetAvatar_ReturnsNotFound_WhenUserHasNoAvatar()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new UserProfileController(db, CreateCache());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

		var result = await controller.GetAvatar(user.Id, null, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task GetAvatar_ReturnsFile_WhenBase64Avatar()
	{
		await using var db = CreateDb(true);
		var user = await db.ApplicationUsers.FirstAsync();
		user.AvatarUrl = "data:image/png;base64,iVBORw0KGgo="; // minimal valid base64
		await db.SaveChangesAsync();
		var controller = new UserProfileController(db, CreateCache());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

		var result = await controller.GetAvatar(user.Id, null, CancellationToken.None);

		var fileResult = Assert.IsType<FileContentResult>(result);
		Assert.Equal("image/png", fileResult.ContentType);
		Assert.NotNull(fileResult.FileContents);
	}
}
