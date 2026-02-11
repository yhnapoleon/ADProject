using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EcoLens.Tests.Controllers;

/// <summary>
/// Returns a fixed JSON response for carbon lookup (used to mock IHttpClientFactory).
/// </summary>
internal sealed class MockCarbonLookupHandler : DelegatingHandler
{
	private readonly string _json;

	public MockCarbonLookupHandler(string labelName = "Beef", decimal co2Factor = 27m, string unit = "kg")
	{
		_json = JsonSerializer.Serialize(new[] { new { Id = 1, LabelName = labelName, Unit = unit, Co2Factor = co2Factor } });
	}

	public MockCarbonLookupHandler(string json)
	{
		_json = json;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
	}
}

public class FoodControllerTests
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

	private static IHttpClientFactory CreateMockHttpClientFactory(string labelName = "Beef", decimal co2Factor = 27m, string unit = "kg")
	{
		var client = new HttpClient(new MockCarbonLookupHandler(labelName, co2Factor, unit)) { BaseAddress = new Uri("https://localhost") };
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
		return mockFactory.Object;
	}

	[Fact]
	public async Task CalculateByName_ReturnsBadRequest_WhenFoodNameEmpty()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.CalculateByName(new FoodCalculateByNameRequest { FoodName = "   ", Quantity = 1, Unit = "g" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("FoodName is required.", badRequest.Value);
	}

	[Fact]
	public async Task CalculateByName_ReturnsNotFound_WhenFactorNotFound()
	{
		var json = JsonSerializer.Serialize(Array.Empty<object>());
		var client = new HttpClient(new MockCarbonLookupHandler(json)) { BaseAddress = new Uri("https://localhost") };
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, mockFactory.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.CalculateByName(new FoodCalculateByNameRequest { FoodName = "UnknownFood", Quantity = 1, Unit = "g" }, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
		Assert.NotNull(notFound.Value);
	}

	[Fact]
	public async Task CalculateByName_ReturnsOk_WhenFactorFound()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory("Beef", 27m, "kg"));
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.CalculateByName(new FoodCalculateByNameRequest { FoodName = "Beef", Quantity = 1, Unit = "kg" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodCalculationResultDto>(ok.Value);
		Assert.Equal("Beef", dto.FoodName);
		Assert.Equal(27m, dto.Co2Factor);
		Assert.Equal(27m, dto.TotalEmission); // 1 kg * 27
	}

	[Fact]
	public async Task IngestByName_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.CheckAndAwardPointsAsync(It.IsAny<int>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(), Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.IngestByName(new FoodIngestByNameRequest { Name = "Beef", Quantity = 1, Unit = "kg" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task IngestByName_ReturnsOkAndSavesRecord_WhenValid()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		mockPoint.Setup(x => x.CheckAndAwardPointsAsync(It.IsAny<int>(), It.IsAny<DateTime>())).Returns(Task.CompletedTask);
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.IngestByName(new FoodIngestByNameRequest { Name = "Beef", Quantity = 0.5, Unit = "kg" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodSimpleCalcResponse>(ok.Value);
		Assert.Equal("Beef", dto.Name);
		Assert.True(dto.Emission > 0);
		var record = await db.FoodRecords.FirstOrDefaultAsync(r => r.UserId == user.Id);
		Assert.NotNull(record);
		Assert.Equal("Beef", record.Name);
	}

	[Fact]
	public async Task CalculateSimple_ReturnsNotFound_WhenFactorNotFound()
	{
		var json = JsonSerializer.Serialize(Array.Empty<object>());
		var client = new HttpClient(new MockCarbonLookupHandler(json)) { BaseAddress = new Uri("https://localhost") };
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, mockFactory.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.CalculateSimple(new FoodCalculateSimpleRequest { Name = "Unknown", Quantity = 1, Unit = "g" }, CancellationToken.None);

		Assert.IsType<NotFoundObjectResult>(result.Result);
	}

	[Fact]
	public async Task CalculateSimple_ReturnsOk_WhenFactorFound()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory("Beef", 27m, "kg"));
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.CalculateSimple(new FoodCalculateSimpleRequest { Name = "Beef", Quantity = 0.1, Unit = "kg" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodSimpleCalcResponse>(ok.Value);
		Assert.Equal("Beef", dto.Name);
		Assert.Equal(2.7m, dto.Emission); // 0.1 kg * 27
	}

	[Fact]
	public async Task Recognize_ReturnsBadRequest_WhenNoFile()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());

		var result = await controller.Recognize(new FoodImageUploadDto { File = null! }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No image uploaded.", badRequest.Value);
	}

	[Fact]
	public async Task Recognize_ReturnsOk_WhenFileProvided()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync(new EcoLens.Api.DTOs.VisionPredictionResponseDto { Label = "Beef", Confidence = 95, SourceModel = "test" });
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake image"));
		var file = new FormFile(stream, 0, stream.Length, "file", "x.jpg") { Headers = new HeaderDictionary() };

		var result = await controller.Recognize(new FoodImageUploadDto { File = file }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodRecognizeResponseDto>(ok.Value);
		Assert.Equal("Beef", dto.FoodName);
		Assert.Equal(95, dto.Confidence);
	}

	[Fact]
	public async Task IngestByName_ReturnsNotFound_WhenFactorNotFound()
	{
		var json = JsonSerializer.Serialize(Array.Empty<object>());
		var client = new HttpClient(new MockCarbonLookupHandler(json)) { BaseAddress = new Uri("https://localhost") };
		var mockFactory = new Mock<IHttpClientFactory>();
		mockFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, mockFactory.Object);
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.IngestByName(new FoodIngestByNameRequest { Name = "UnknownFood", Quantity = 1, Unit = "kg" }, CancellationToken.None);

		Assert.IsType<NotFoundObjectResult>(result.Result);
	}

	[Fact]
	public async Task IngestFromImage_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync(new EcoLens.Api.DTOs.VisionPredictionResponseDto { Label = "Beef", Confidence = 90, SourceModel = "test" });
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(), Request = { Scheme = "https", Host = new HostString("localhost") } } };
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake"));
		var file = new FormFile(stream, 0, stream.Length, "file", "x.jpg") { Headers = new HeaderDictionary() };

		var result = await controller.IngestFromImage(new FoodIngestFromImageRequest { File = file, Quantity = 0.2, Unit = "kg" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task IngestFromImage_ReturnsBadRequest_WhenNoFile()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.IngestFromImage(new FoodIngestFromImageRequest { File = null!, Quantity = 0.2, Unit = "kg" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No image uploaded.", badRequest.Value);
	}

	[Fact]
	public async Task CalculateFromImage_ReturnsBadRequest_WhenNoFile()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.CalculateFromImage(new FoodCalculateFromImageRequest { File = null!, Quantity = 0.1, Unit = "kg" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No image uploaded.", badRequest.Value);
	}

	[Fact]
	public async Task CalculateFromImage_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync();
		var mockVision = new Mock<IVisionService>();
		mockVision.Setup(x => x.PredictAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync(new EcoLens.Api.DTOs.VisionPredictionResponseDto { Label = "Beef", Confidence = 90, SourceModel = "test" });
		var mockPoint = new Mock<IPointService>();
		var controller = new FoodController(db, mockVision.Object, mockPoint.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<FoodController>.Instance, CreateMockHttpClientFactory());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake"));
		var file = new FormFile(stream, 0, stream.Length, "file", "x.jpg") { Headers = new HeaderDictionary() };

		var result = await controller.CalculateFromImage(new FoodCalculateFromImageRequest { File = file, Quantity = 0.1, Unit = "kg" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodCalculationResultDto>(ok.Value);
		Assert.Equal("Beef", dto.FoodName);
		Assert.Equal(2.7m, dto.TotalEmission);
	}
}
