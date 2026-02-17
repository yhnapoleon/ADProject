using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Diet;
using EcoLens.Api.Models;
using EcoLens.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoLens.Tests.Services;

public class DietTemplateServiceTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new ApplicationDbContext(options);
	}

	[Fact]
	public async Task CreateTemplateAsync_ThrowsArgumentException_WhenTemplateNameEmpty()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var request = new CreateDietTemplateRequest { TemplateName = "" };

		var ex = await Assert.ThrowsAsync<ArgumentException>(
			() => service.CreateTemplateAsync(Guid.NewGuid(), request));

		Assert.Contains("TemplateName", ex.Message);
		Assert.Equal("TemplateName", ex.ParamName);
	}

	[Fact]
	public async Task CreateTemplateAsync_ThrowsArgumentException_WhenTemplateNameWhitespace()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var request = new CreateDietTemplateRequest { TemplateName = "   " };

		var ex = await Assert.ThrowsAsync<ArgumentException>(
			() => service.CreateTemplateAsync(Guid.NewGuid(), request));

		Assert.Contains("TemplateName", ex.Message);
	}

	[Fact]
	public async Task CreateTemplateAsync_ReturnsDto_WhenValidWithoutItems()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var userId = Guid.NewGuid();
		var request = new CreateDietTemplateRequest { TemplateName = "Breakfast" };

		var result = await service.CreateTemplateAsync(userId, request);

		Assert.NotNull(result);
		Assert.True(result.Id > 0);
		Assert.Equal(userId, result.UserId);
		Assert.Equal("Breakfast", result.TemplateName);
		Assert.NotNull(result.Items);
		Assert.Empty(result.Items);
		Assert.True(result.CreatedAt <= DateTime.UtcNow.AddSeconds(2) && result.CreatedAt >= DateTime.UtcNow.AddSeconds(-2));

		var saved = await db.DietTemplates.FirstAsync(t => t.Id == result.Id);
		Assert.Equal(userId, saved.UserId);
		Assert.Equal("Breakfast", saved.TemplateName);
	}

	[Fact]
	public async Task CreateTemplateAsync_ReturnsDtoWithItems_WhenItemsProvided()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var userId = Guid.NewGuid();
		var request = new CreateDietTemplateRequest
		{
			TemplateName = "Lunch",
			Items = new List<CreateDietTemplateItemRequest>
			{
				new() { FoodId = 1, Quantity = 2, Unit = "portion" },
				new() { FoodId = 2, Quantity = 0.5, Unit = "cup" }
			}
		};

		var result = await service.CreateTemplateAsync(userId, request);

		Assert.NotNull(result);
		Assert.Equal("Lunch", result.TemplateName);
		Assert.Equal(2, result.Items.Count);
		Assert.Equal(1, result.Items[0].FoodId);
		Assert.Equal(2, result.Items[0].Quantity);
		Assert.Equal("portion", result.Items[0].Unit);
		Assert.Equal(2, result.Items[1].FoodId);
		Assert.Equal(0.5, result.Items[1].Quantity);
		Assert.Equal("cup", result.Items[1].Unit);

		var saved = await db.DietTemplates.Include(t => t.Items).FirstAsync(t => t.Id == result.Id);
		Assert.Equal(2, saved.Items.Count);
	}

	[Fact]
	public async Task GetUserTemplatesAsync_ReturnsEmptyList_WhenNoTemplates()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var userId = Guid.NewGuid();

		var result = await service.GetUserTemplatesAsync(userId);

		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetUserTemplatesAsync_ReturnsAllTemplatesForUser_WhenExist()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var userId = Guid.NewGuid();
		db.DietTemplates.Add(new DietTemplate { UserId = userId, TemplateName = "First" });
		db.DietTemplates.Add(new DietTemplate { UserId = userId, TemplateName = "Second" });
		await db.SaveChangesAsync();

		var result = await service.GetUserTemplatesAsync(userId);

		Assert.Equal(2, result.Count);
		var names = result.Select(t => t.TemplateName).OrderBy(n => n).ToList();
		Assert.Equal("First", names[0]);
		Assert.Equal("Second", names[1]);
	}

	[Fact]
	public async Task GetUserTemplatesAsync_ReturnsOnlyTemplatesForGivenUser()
	{
		await using var db = CreateDb();
		var service = new DietTemplateService(db);
		var userA = Guid.NewGuid();
		var userB = Guid.NewGuid();
		db.DietTemplates.Add(new DietTemplate { UserId = userA, TemplateName = "A" });
		db.DietTemplates.Add(new DietTemplate { UserId = userB, TemplateName = "B" });
		await db.SaveChangesAsync();

		var result = await service.GetUserTemplatesAsync(userA);

		Assert.Single(result);
		Assert.Equal("A", result[0].TemplateName);
	}
}
