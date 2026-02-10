using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Food;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;

namespace EcoLens.Tests.Controllers;

public class SimpleFoodControllerTests
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
				Email = "u1@test.com",
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
		var ctx = new DefaultHttpContext();
		ctx.Request.Scheme = "http";
		ctx.Request.Host = new HostString("localhost");
		ctx.User = new ClaimsPrincipal(identity);
		controller.ControllerContext = new ControllerContext { HttpContext = ctx };
	}

	private static IHttpClientFactory CreateMockFactory(string lookupJson)
	{
		var handler = new Mock<HttpMessageHandler>();
		handler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(lookupJson, Encoding.UTF8, "application/json")
			});
		var client = new HttpClient(handler.Object);
		var factory = new Mock<IHttpClientFactory>();
		factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
		return factory.Object;
	}

	[Fact]
	public async Task CalculateFood_ReturnsNotFound_WhenLookupReturnsEmpty()
	{
		var factory = CreateMockFactory("[]");
		var controller = new SimpleFoodController(CreateDb(), factory);
		SetUser(controller, 1);

		var result = await controller.CalculateFood(new CalculateFoodRequest { Name = "Unknown", Amount = 100 }, CancellationToken.None);

		var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
		Assert.NotNull(notFound.Value);
	}

	[Fact]
	public async Task CalculateFood_ReturnsOkWithEmission_WhenLookupReturnsFood()
	{
		var lookupJson = "[{\"Id\":0,\"LabelName\":\"Rice\",\"Unit\":\"kg\",\"Co2Factor\":0.5}]";
		var factory = CreateMockFactory(lookupJson);
		var controller = new SimpleFoodController(CreateDb(), factory);
		SetUser(controller, 1);

		var result = await controller.CalculateFood(new CalculateFoodRequest { Name = "Rice", Amount = 1000 }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodCalcResponse>(ok.Value);
		Assert.Equal("Rice", dto.Name);
		Assert.Equal(1.0, dto.Amount);
		Assert.Equal(0.5m, dto.EmissionFactor);
		Assert.Equal(0.5m, dto.Emission);
	}

	[Fact]
	public async Task UpdateFood_DelegatesToCalculateFood()
	{
		var lookupJson = "[{\"Id\":0,\"LabelName\":\"Bread\",\"Unit\":\"kg\",\"Co2Factor\":0.3}]";
		var factory = CreateMockFactory(lookupJson);
		var controller = new SimpleFoodController(CreateDb(), factory);
		SetUser(controller, 1);

		var result = await controller.UpdateFood(new CalculateFoodRequest { Name = "Bread", Amount = 500 }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<FoodCalcResponse>(ok.Value);
		Assert.Equal("Bread", dto.Name);
		Assert.Equal(0.15m, dto.Emission); // 0.5 kg * 0.3
	}

	[Fact]
	public async Task AddFood_ReturnsUnauthorized_WhenUserNotSet()
	{
		var controller = new SimpleFoodController(CreateDb(true), CreateMockFactory("[]"));
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.AddFood(new AddFoodRequest
		{
			Name = "Rice",
			Amount = 200,
			EmissionFactor = 0.5m,
			Emission = 0.1m
		}, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task AddFood_ReturnsOkAndSavesRecord_WhenUserExists()
	{
		await using var db = CreateDb(withUser: true);
		var userId = 1;
		var factory = CreateMockFactory("[]");
		var controller = new SimpleFoodController(db, factory);
		SetUser(controller, userId);

		var result = await controller.AddFood(new AddFoodRequest
		{
			Name = "Rice",
			Amount = 300,
			EmissionFactor = 0.5m,
			Emission = 0.15m,
			Note = "test"
		}, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var resp = Assert.IsType<AddFoodResponse>(ok.Value);
		Assert.True(resp.Success);
		var record = await db.FoodRecords.FirstOrDefaultAsync(f => f.UserId == userId);
		Assert.NotNull(record);
		Assert.Equal("Rice", record.Name);
		Assert.Equal(0.3, record.Amount);
		Assert.Equal(0.5m, record.EmissionFactor);
		Assert.Equal(0.15m, record.Emission);
	}
}
