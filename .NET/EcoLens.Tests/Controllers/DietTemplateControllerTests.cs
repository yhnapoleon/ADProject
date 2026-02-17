using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.DTOs.Diet;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EcoLens.Tests.Controllers;

public class DietTemplateControllerTests
{
	private static void SetUser(ControllerBase controller, Guid userId)
	{
		var identity = new ClaimsIdentity();
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
		var ctx = new DefaultHttpContext();
		ctx.Request.Scheme = "http";
		ctx.Request.Host = new HostString("localhost");
		ctx.User = new ClaimsPrincipal(identity);
		controller.ControllerContext = new ControllerContext { HttpContext = ctx };
	}

	[Fact]
	public async Task Create_ReturnsUnauthorized_WhenUserNotSet()
	{
		var mockService = new Mock<IDietTemplateService>();
		var controller = new DietTemplateController(mockService.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Create(new CreateDietTemplateRequest { TemplateName = "T1" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result);
		mockService.Verify(x => x.CreateTemplateAsync(It.IsAny<Guid>(), It.IsAny<CreateDietTemplateRequest>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Create_ReturnsCreated_WhenUserSet()
	{
		var userId = Guid.NewGuid();
		var createdDto = new DietTemplateDto
		{
			Id = 1,
			UserId = userId,
			TemplateName = "My Template",
			CreatedAt = DateTime.UtcNow,
			Items = new List<DietTemplateItemDto>()
		};
		var mockService = new Mock<IDietTemplateService>();
		mockService.Setup(x => x.CreateTemplateAsync(userId, It.IsAny<CreateDietTemplateRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdDto);
		var controller = new DietTemplateController(mockService.Object);
		SetUser(controller, userId);

		var result = await controller.Create(new CreateDietTemplateRequest { TemplateName = "My Template" }, CancellationToken.None);

		var created = Assert.IsType<CreatedResult>(result);
		Assert.NotNull(created.Location);
		Assert.Contains("/api/diet/templates", created.Location!.ToString());
		var body = Assert.IsType<DietTemplateDto>(created.Value);
		Assert.Equal("My Template", body.TemplateName);
		Assert.Equal(userId, body.UserId);
	}

	[Fact]
	public async Task GetList_ReturnsUnauthorized_WhenUserNotSet()
	{
		var mockService = new Mock<IDietTemplateService>();
		var controller = new DietTemplateController(mockService.Object);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.GetList(CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result);
	}

	[Fact]
	public async Task GetList_ReturnsOkWithList_WhenUserSet()
	{
		var userId = Guid.NewGuid();
		var list = new List<DietTemplateDto>
		{
			new() { Id = 1, UserId = userId, TemplateName = "T1", CreatedAt = DateTime.UtcNow, Items = new List<DietTemplateItemDto>() }
		};
		var mockService = new Mock<IDietTemplateService>();
		mockService.Setup(x => x.GetUserTemplatesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(list);
		var controller = new DietTemplateController(mockService.Object);
		SetUser(controller, userId);

		var result = await controller.GetList(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result);
		var items = Assert.IsAssignableFrom<List<DietTemplateDto>>(ok.Value);
		Assert.Single(items);
		Assert.Equal("T1", items[0].TemplateName);
	}

	[Fact]
	public async Task GetList_ReturnsOkWithEmptyList_WhenUserHasNoTemplates()
	{
		var userId = Guid.NewGuid();
		var mockService = new Mock<IDietTemplateService>();
		mockService.Setup(x => x.GetUserTemplatesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<DietTemplateDto>());
		var controller = new DietTemplateController(mockService.Object);
		SetUser(controller, userId);

		var result = await controller.GetList(CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result);
		var items = Assert.IsAssignableFrom<List<DietTemplateDto>>(ok.Value);
		Assert.NotNull(items);
		Assert.Empty(items);
	}
}
