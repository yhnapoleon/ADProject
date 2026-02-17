using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class LeaderboardControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withUsers = false, int userCount = 2, bool withPointAwardLogs = false)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withUsers)
		{
			for (var i = 0; i < userCount; i++)
			{
				db.ApplicationUsers.Add(new ApplicationUser
				{
					Username = $"u{i}",
					Email = $"u{i}@t.com",
					PasswordHash = "x",
					Role = UserRole.User,
					Region = "SG",
					BirthDate = DateTime.UtcNow.AddYears(-20),
					IsActive = true,
					TotalCarbonSaved = 10m - i,
					CurrentPoints = 100 - i * 10
				});
			}
			await db.SaveChangesAsync();
		}
		if (withPointAwardLogs)
		{
			var users = await db.ApplicationUsers.ToListAsync();
			foreach (var u in users)
			{
				db.PointAwardLogs.Add(new PointAwardLog { UserId = u.Id, Points = 10, AwardedAt = DateTime.UtcNow, Source = "Step" });
			}
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
	public async Task Get_ReturnsOkWithList_WhenUsersExist()
	{
		await using var db = await CreateDbAsync(withUsers: true, userCount: 3);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.Get("month", 50, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
		var items = list.ToList();
		Assert.True(items.Count >= 2);
	}

	[Fact]
	public async Task GetToday_ReturnsOk()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetToday(50, CancellationToken.None);

		Assert.IsType<OkObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetMonth_ReturnsOk()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetMonth(50, CancellationToken.None);

		Assert.IsType<OkObjectResult>(result.Result);
	}

	[Fact]
	public async Task GetByUsername_ReturnsNotFound_WhenUsernameNotInLeaderboard()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetByUsername("nonexistent", "month", CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task GetByUsername_ReturnsOk_WhenUsernameExists()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetByUsername("u0", "month", CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
	}

	[Fact]
	public async Task TopUsers_ReturnsOkWithUpTo10()
	{
		await using var db = await CreateDbAsync(withUsers: true, userCount: 5);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.TopUsers(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
		var items = list.ToList();
		Assert.True(items.Count <= 10);
		Assert.True(items.Count >= 2);
	}

	[Fact]
	public async Task Follow_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(withUsers: true, userCount: 2);
		var target = await db.ApplicationUsers.OrderBy(u => u.Id).Skip(1).FirstAsync();
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Follow(target.Id, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result);
	}

	[Fact]
	public async Task Follow_ReturnsBadRequest_WhenFollowingSelf()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new LeaderboardController(db);
		SetUser(controller, user.Id);

		var result = await controller.Follow(user.Id, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal("Cannot follow yourself.", badRequest.Value);
	}

	[Fact]
	public async Task Follow_ReturnsNotFound_WhenTargetMissing()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new LeaderboardController(db);
		SetUser(controller, user.Id);

		var result = await controller.Follow(99999, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result);
		Assert.Equal("Target user not found.", notFound.Value);
	}

	[Fact]
	public async Task Follow_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync(withUsers: true, userCount: 2);
		var follower = await db.ApplicationUsers.OrderBy(u => u.Id).FirstAsync();
		var followee = await db.ApplicationUsers.OrderBy(u => u.Id).Skip(1).FirstAsync();
		var controller = new LeaderboardController(db);
		SetUser(controller, follower.Id);

		var result = await controller.Follow(followee.Id, CancellationToken.None);

		Assert.IsType<OkResult>(result);
		var exists = await db.UserFollows.AnyAsync(f => f.FollowerId == follower.Id && f.FolloweeId == followee.Id);
		Assert.True(exists);
	}

	[Fact]
	public async Task Follow_ReturnsNoContent_WhenAlreadyFollowing()
	{
		await using var db = await CreateDbAsync(withUsers: true, userCount: 2);
		var follower = await db.ApplicationUsers.OrderBy(u => u.Id).FirstAsync();
		var followee = await db.ApplicationUsers.OrderBy(u => u.Id).Skip(1).FirstAsync();
		db.UserFollows.Add(new UserFollow { FollowerId = follower.Id, FolloweeId = followee.Id });
		await db.SaveChangesAsync();
		var controller = new LeaderboardController(db);
		SetUser(controller, follower.Id);

		var result = await controller.Follow(followee.Id, CancellationToken.None);

		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task Friends_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(withUsers: true);
		var controller = new LeaderboardController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Friends(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task Friends_ReturnsOkWithList_WhenUserSet()
	{
		await using var db = await CreateDbAsync(withUsers: true, userCount: 2);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new LeaderboardController(db);
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.Friends(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
		Assert.NotNull(list);
	}
}
