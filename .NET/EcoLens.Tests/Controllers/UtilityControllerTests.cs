using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EcoLens.Tests;

public class UtilityControllerTests
{
	private static ApplicationDbContext CreateDb()
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new ApplicationDbContext(options);
	}

	private class FakeAiService : IAiService
	{
		public string? LastPrompt { get; private set; }
		public IFormFile? LastImage { get; private set; }
		public string? AnalyzeImageResult { get; set; }
		public Exception? AnalyzeImageException { get; set; }

		public Task<string> GetAnswerAsync(string userPrompt) => Task.FromResult("");

		public Task<string> GetAnswerAsync(string userPrompt, string? systemPrompt, CancellationToken ct = default)
			=> Task.FromResult("");

		public Task<string> AnalyzeImageAsync(string prompt, IFormFile image)
		{
			LastPrompt = prompt;
			LastImage = image;
			if (AnalyzeImageException != null) throw AnalyzeImageException;
			return Task.FromResult(AnalyzeImageResult ?? "{}");
		}
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

	private static UtilityController CreateController(ApplicationDbContext db, IAiService? aiService = null)
	{
		var ai = aiService ?? new FakeAiService();
		var controller = new UtilityController(ai, db);
		return controller;
	}

	[Fact]
	public async Task SaveRecord_ShouldReturnUnauthorized_WhenNoUser()
	{
		await using var db = CreateDb();
		var controller = CreateController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var dto = new UtilityController.UpsertUtilityRecordDto
		{
			YearMonth = "2024-01",
			ElectricityUsage = 100,
			ElectricityCost = 50,
			WaterUsage = 10,
			WaterCost = 20,
			GasUsage = 0,
			GasCost = 0
		};

		var result = await controller.SaveRecord(dto, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task SaveRecord_ShouldReturnBadRequest_WhenYearMonthInvalid()
	{
		await using var db = CreateDb();
		var user = new ApplicationUser
		{
			Username = "u1",
			Email = "u1@t.com",
			PasswordHash = "x",
			Role = UserRole.User,
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20),
			IsActive = true
		};
		db.ApplicationUsers.Add(user);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		SetUser(controller, user.Id);

		var dto = new UtilityController.UpsertUtilityRecordDto
		{
			YearMonth = "2024", // 长度 != 7
			ElectricityUsage = 100,
			ElectricityCost = 0,
			WaterUsage = 0,
			WaterCost = 0,
			GasUsage = 0,
			GasCost = 0
		};

		var result = await controller.SaveRecord(dto, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Contains("YYYY-MM", badRequest.Value!.ToString());
	}

	[Fact]
	public async Task SaveRecord_ShouldReturnOk_WhenValid()
	{
		await using var db = CreateDb();
		var user = new ApplicationUser
		{
			Username = "u1",
			Email = "u1@t.com",
			PasswordHash = "x",
			Role = UserRole.User,
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-20),
			IsActive = true
		};
		db.ApplicationUsers.Add(user);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		SetUser(controller, user.Id);

		var dto = new UtilityController.UpsertUtilityRecordDto
		{
			YearMonth = "2024-06",
			ElectricityUsage = 200,
			ElectricityCost = 100,
			WaterUsage = 15,
			WaterCost = 30,
			GasUsage = 0,
			GasCost = 0
		};

		var result = await controller.SaveRecord(dto, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<UtilityController.UtilityRecordResponseDto>(ok.Value);
		Assert.Equal("2024-06", body.YearMonth);
		Assert.Equal(200, body.ElectricityUsage);
		Assert.Equal(15, body.WaterUsage);
		Assert.True(body.Id > 0);

		Assert.True(await db.UtilityBills.AnyAsync(b => b.UserId == user.Id && b.YearMonth == "2024-06"));
	}

	[Fact]
	public async Task MyRecords_ShouldReturnUnauthorized_WhenNoUser()
	{
		await using var db = CreateDb();
		var controller = CreateController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.MyRecords(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task MyRecords_ShouldReturnOk_WhenUserExists()
	{
		await using var db = CreateDb();
		var user = new ApplicationUser
		{
			Username = "u2",
			Email = "u2@t.com",
			PasswordHash = "x",
			Role = UserRole.User,
			Region = "SG",
			BirthDate = DateTime.UtcNow.AddYears(-25),
			IsActive = true
		};
		db.ApplicationUsers.Add(user);
		await db.SaveChangesAsync();

		var controller = CreateController(db);
		SetUser(controller, user.Id);

		var result = await controller.MyRecords(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<UtilityController.UtilityRecordResponseDto>>(ok.Value);
		Assert.NotNull(list);
	}

	[Fact]
	public async Task Ocr_ShouldReturnBadRequest_WhenNoFile()
	{
		await using var db = CreateDb();
		var controller = CreateController(db);
		SetUser(controller, 1);

		var result = await controller.Ocr(null!, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Contains("No bill image", badRequest.Value!.ToString());
	}

	[Fact]
	public async Task Ocr_ShouldReturnBadRequest_WhenFileEmpty()
	{
		await using var db = CreateDb();
		var controller = CreateController(db);
		SetUser(controller, 1);

		var emptyFile = new FakeFormFile("bill", "bill.jpg", "image/jpeg", Array.Empty<byte>());
		var result = await controller.Ocr(emptyFile, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Contains("No bill image", badRequest.Value!.ToString());
	}

	private class FakeFormFile : IFormFile
	{
		private readonly byte[] _content;

		public FakeFormFile(string name, string fileName, string contentType, byte[] content)
		{
			Name = name;
			FileName = fileName;
			ContentType = contentType;
			_content = content ?? Array.Empty<byte>();
		}

		public FakeFormFile(string name, string fileName, string contentType, string text)
			: this(name, fileName, contentType, Encoding.UTF8.GetBytes(text ?? "")) { }

		public string ContentType { get; }
		public string ContentDisposition { get; set; } = "";
		public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
		public long Length => _content.Length;
		public string Name { get; }
		public string FileName { get; }

		public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);
		public Task CopyToAsync(Stream target, CancellationToken ct = default)
		{
			target.Write(_content, 0, _content.Length);
			return Task.CompletedTask;
		}
		public Stream OpenReadStream() => new MemoryStream(_content);
	}
}
