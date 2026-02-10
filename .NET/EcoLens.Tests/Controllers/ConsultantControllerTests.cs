using EcoLens.Api.Controllers;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EcoLens.Tests.Controllers;

public class ConsultantControllerTests
{
	[Fact]
	public async Task Chat_ReturnsBadRequest_WhenQuestionEmpty()
	{
		var mockAi = new Mock<IAiService>();
		var controller = new ConsultantController(mockAi.Object);

		var result = await controller.Chat(new ConsultantController.ChatQuestionDto { Question = "   " }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.NotNull(badRequest.Value);
		mockAi.Verify(x => x.GetAnswerAsync(It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task Chat_ReturnsBadRequest_WhenQuestionNull()
	{
		var mockAi = new Mock<IAiService>();
		var controller = new ConsultantController(mockAi.Object);

		var result = await controller.Chat(new ConsultantController.ChatQuestionDto { Question = null! }, CancellationToken.None);

		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task Chat_ReturnsOkWithAnswer_WhenQuestionValid()
	{
		var mockAi = new Mock<IAiService>();
		mockAi.Setup(x => x.GetAnswerAsync(It.IsAny<string>())).ReturnsAsync("Try reducing meat consumption.");
		var controller = new ConsultantController(mockAi.Object);

		var result = await controller.Chat(new ConsultantController.ChatQuestionDto { Question = "How to reduce carbon?" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<ConsultantController.ChatAnswerDto>(ok.Value);
		Assert.Equal("Try reducing meat consumption.", dto.Answer);
		mockAi.Verify(x => x.GetAnswerAsync("How to reduce carbon?"), Times.Once);
	}
}
