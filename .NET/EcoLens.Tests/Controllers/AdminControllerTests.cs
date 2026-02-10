using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using System.Security.Claims;
using EcoLens.Api.DTOs.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoLens.Tests;

public class AdminControllerTests
{
	private static ApplicationDbContext CreateInMemoryDbContext()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new ApplicationDbContext(options);
	}

	private class FakeFormFile : IFormFile
	{
		private readonly byte[] _content;

		public FakeFormFile(string fileName, string content, string contentType = "application/json")
		{
			FileName = fileName;
			Name = "file";
			ContentType = contentType;
			_content = Encoding.UTF8.GetBytes(content);
		}

		public string ContentType { get; }
		public string ContentDisposition { get; set; } = string.Empty;
		public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
		public long Length => _content.Length;
		public string Name { get; }
		public string FileName { get; }

		public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);

		public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
		{
			target.Write(_content, 0, _content.Length);
			return Task.CompletedTask;
		}

		public Stream OpenReadStream() => new MemoryStream(_content);
	}

	[Fact]
	public async Task GetEmissionFactors_ShouldReturnPagedItems_WithFilter()
	{
		await using var db = CreateInMemoryDbContext();

		db.CarbonReferences.AddRange(
			new CarbonReference
			{
				LabelName = "Food SG",
				Category = CarbonCategory.Food,
				Co2Factor = 0.5m,
				Unit = "kWh",
				Region = "SG",
				Source = "Seed"
			},
			new CarbonReference
			{
				LabelName = "Transport Global",
				Category = CarbonCategory.Transport,
				Co2Factor = 0.2m,
				Unit = "m3",
				Region = "Global",
				Source = "Seed"
			}
		);
		await db.SaveChangesAsync();

		var controller = new AdminController(db);

		// 按关键字 & Category 过滤，应只命中 Food 这一条
		var result = await controller.GetEmissionFactors("Food", CarbonCategory.Food.ToString(), page: 1, pageSize: 10, ct: CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);

		var valueType = ok.Value!.GetType();
		var itemsProp = valueType.GetProperty("items");
		var totalProp = valueType.GetProperty("total");

		Assert.NotNull(itemsProp);
		Assert.NotNull(totalProp);

		var items = Assert.IsAssignableFrom<IEnumerable<AdminController.EmissionFactorListItem>>(itemsProp!.GetValue(ok.Value)!);
		var total = Assert.IsType<int>(totalProp!.GetValue(ok.Value)!);

		Assert.Equal(1, total);
		Assert.Single(items);
	}

	[Fact]
	public async Task CreateEmissionFactor_ShouldReturnBadRequest_WhenMissingRequiredFields()
	{
		await using var db = CreateInMemoryDbContext();
		var controller = new AdminController(db);

		var dto = new AdminController.EmissionFactorListItem
		{
			// 缺少 ItemName / Unit / Category
			ItemName = "",
			Unit = "",
			Category = ""
		};

		var result = await controller.CreateEmissionFactor(dto, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Contains("ItemName/Unit/Category required", badRequest.Value!.ToString());
	}

	[Fact]
	public async Task CreateEmissionFactor_ShouldCreateEntity_OnValidInput()
	{
		await using var db = CreateInMemoryDbContext();
		var controller = new AdminController(db);

		var dto = new AdminController.EmissionFactorListItem
		{
			ItemName = "Test Factor",
			Category = CarbonCategory.Food.ToString(),
			Factor = 1.23m,
			Unit = "kWh",
			Source = "Test"
		};

		var result = await controller.CreateEmissionFactor(dto, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<AdminController.EmissionFactorListItem>(ok.Value);

		Assert.Equal("Test Factor", body.ItemName);
		Assert.Equal(CarbonCategory.Food.ToString(), body.Category);

		var entity = await db.CarbonReferences.SingleAsync();
		Assert.Equal("Test Factor", entity.LabelName);
		Assert.Equal(CarbonCategory.Food, entity.Category);
		Assert.Equal(1.23m, entity.Co2Factor);
		Assert.Equal("kWh", entity.Unit);
	}

	[Fact]
	public async Task ImportEmissionFactors_ShouldReturnParseError_OnInvalidJson()
	{
		await using var db = CreateInMemoryDbContext();
		var controller = new AdminController(db);

		// 非 JSON 内容，将触发 JsonException 分支
		var file = new FakeFormFile("factors.json", "not a json");

		var result = await controller.ImportEmissionFactors(file, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var valueType = ok.Value!.GetType();
		var importedProp = valueType.GetProperty("importedCount");
		var errorsProp = valueType.GetProperty("errors");

		Assert.NotNull(importedProp);
		Assert.NotNull(errorsProp);

		var imported = Assert.IsType<int>(importedProp!.GetValue(ok.Value)!);
		Assert.Equal(0, imported);
	}

	private static ApplicationDbContext CreateDbWithUsers(out ApplicationUser admin, out ApplicationUser user)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);

		admin = new ApplicationUser
		{
			Username = "admin",
			Email = "admin@test.com",
			PasswordHash = "x",
			Role = UserRole.Admin,
			Region = "West Region",
			BirthDate = DateTime.UtcNow.AddYears(-30),
			IsActive = true,
			TotalCarbonSaved = 10m
		};

		user = new ApplicationUser
		{
			Username = "user1",
			Email = "user1@test.com",
			PasswordHash = "x",
			Role = UserRole.User,
			Region = "East Region",
			BirthDate = DateTime.UtcNow.AddYears(-20),
			IsActive = true,
			TotalCarbonSaved = 20m
		};

		db.ApplicationUsers.AddRange(admin, user);
		db.SaveChanges();

		return db;
	}

	private static void SetAdminUser(ControllerBase controller, int adminId)
	{
		var identity = new ClaimsIdentity();
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, adminId.ToString()));
		identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
		};
	}

	[Fact]
	public async Task RegionStats_ShouldAggregateByUserTotalCarbonSaved_WhenNoDateRange()
	{
		var db = CreateDbWithUsers(out var admin, out var user);
		var controller = new AdminController(db);
		SetAdminUser(controller, admin.Id);

		var result = await controller.RegionStats(null, null, null, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var items = Assert.IsAssignableFrom<IEnumerable<AdminController.RegionStatItem>>(ok.Value);

		// West/East 两个区域的统计都应出现
		Assert.True(items.Any(i => i.RegionCode == "WR" || i.RegionCode == "ER"));
	}

	[Fact]
	public async Task WeeklyImpact_ShouldReturnFiveWeeksOfData()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var db = new ApplicationDbContext(options);

		// 种几条 ActivityLogs 覆盖最近几周
		var today = DateTime.UtcNow.Date;
		db.ActivityLogs.Add(new ActivityLog { UserId = 1, CreatedAt = today.AddDays(-3), TotalEmission = 1 });
		db.ActivityLogs.Add(new ActivityLog { UserId = 1, CreatedAt = today.AddDays(-10), TotalEmission = 2 });
		db.ActivityLogs.Add(new ActivityLog { UserId = 1, CreatedAt = today.AddDays(-20), TotalEmission = 3 });
		await db.SaveChangesAsync();

		var controller = new AdminController(db);

		var result = await controller.WeeklyImpact(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
		// 约定 5 周的聚合结果
		Assert.Equal(5, list.Count());
	}

	[Fact]
	public async Task AdminUsers_ShouldReturnPagedList()
	{
		var db = CreateDbWithUsers(out var admin, out var user);
		var controller = new AdminController(db);
		SetAdminUser(controller, admin.Id);

		var result = await controller.AdminUsers(null, page: 1, pageSize: 10, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
	}

	[Fact]
	public async Task GetUserEmissionStats_ShouldReturnNotFound_WhenUserMissing()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var db = new ApplicationDbContext(options);
		var controller = new AdminController(db);

		var result = await controller.GetUserEmissionStats(999, null, null, null, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
		Assert.Contains("User not found", notFound.Value!.ToString());
	}

	[Fact]
	public async Task GetUserEmissionStats_ShouldAggregateEmissions_WhenUserExists()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using var db = new ApplicationDbContext(options);

		var user = new ApplicationUser
		{
			Username = "u1",
			Email = "u1@test.com",
			PasswordHash = "x",
			Role = UserRole.User,
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20),
			IsActive = true
		};
		db.ApplicationUsers.Add(user);
		await db.SaveChangesAsync();

		db.ActivityLogs.Add(new ActivityLog { UserId = user.Id, CreatedAt = DateTime.UtcNow.AddDays(-1), TotalEmission = 1 });
		db.TravelLogs.Add(new TravelLog { UserId = user.Id, CreatedAt = DateTime.UtcNow.AddDays(-1), CarbonEmission = 2 });
		db.UtilityBills.Add(new UtilityBill { UserId = user.Id, CreatedAt = DateTime.UtcNow.AddDays(-1), TotalCarbonEmission = 3 });
		await db.SaveChangesAsync();

		var controller = new AdminController(db);

		var result = await controller.GetUserEmissionStats(user.Id, days: 7, from: null, to: null, ct: CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
	}
}