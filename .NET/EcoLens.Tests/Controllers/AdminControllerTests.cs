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
}