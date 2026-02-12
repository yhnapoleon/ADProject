using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using EcoLens.Api.Services;

namespace EcoLens.Tests.Controllers;

public class AiChatControllerTests
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
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
		};
	}

	[Fact]
	public async Task Chat_ReturnsBadRequest_WhenMessageEmpty()
	{
		var mockAi = new Mock<IAiService>();
		await using var db = CreateDb();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());

		var result = await controller.Chat(new AiChatController.ChatRequestDto { Message = "   " }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Message is required.", badRequest.Value);
		mockAi.Verify(x => x.GetAnswerAsync(It.IsAny<string>()), Times.Never);
		mockAi.Verify(x => x.GetAnswerAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Chat_ReturnsOkWithReply_WhenMessageValid()
	{
		var mockAi = new Mock<IAiService>();
		// 双阶段：Pass1 返回关键词数组；Pass2 返回最终答案
		mockAi.SetupSequence(x => x.GetAnswerAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("[]")
			.ReturnsAsync("Here is some advice.");
		await using var db = CreateDb();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());

		var result = await controller.Chat(new AiChatController.ChatRequestDto { Message = "How to reduce carbon?" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<AiChatController.ChatResponseDto>(ok.Value);
		Assert.Equal("Here is some advice.", dto.Reply);
		mockAi.Verify(x => x.GetAnswerAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[Fact]
	public async Task AssistantChat_ReturnsBadRequest_WhenMessagesEmpty()
	{
		var mockAi = new Mock<IAiService>();
		await using var db = CreateDb();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());

		var result = await controller.AssistantChat(new AiChatController.MessagesChatRequestDto { Messages = new List<AiChatController.ChatMessageItem>() }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("messages is required.", badRequest.Value);
	}

	[Fact]
	public async Task AssistantChat_ReturnsBadRequest_WhenNoContentToAnswer()
	{
		var mockAi = new Mock<IAiService>();
		await using var db = CreateDb();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());
		// Last user message and fallback last message are both empty/whitespace -> "No content to answer."
		var result = await controller.AssistantChat(new AiChatController.MessagesChatRequestDto
		{
			Messages = new List<AiChatController.ChatMessageItem>
			{
				new() { Role = "user", Content = "   " },
				new() { Role = "assistant", Content = "" }
			}
		}, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("No content to answer.", badRequest.Value);
	}

	[Fact]
	public async Task AssistantChat_ReturnsOkWithMessage_WhenValid()
	{
		var mockAi = new Mock<IAiService>();
		mockAi.SetupSequence(x => x.GetAnswerAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("[]")
			.ReturnsAsync("Assistant reply.");
		await using var db = CreateDb();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());

		var result = await controller.AssistantChat(new AiChatController.MessagesChatRequestDto
		{
			Messages = new List<AiChatController.ChatMessageItem>
			{
				new() { Role = "user", Content = "Hello" }
			}
		}, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.NotNull(ok.Value);
		var role = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value)?.GetType().GetProperty("role")?.GetValue(ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value));
		var content = ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value)?.GetType().GetProperty("content")?.GetValue(ok.Value.GetType().GetProperty("message")?.GetValue(ok.Value));
		Assert.Equal("assistant", role?.ToString());
		Assert.Equal("Assistant reply.", content?.ToString());
	}

	[Fact]
	public async Task Analysis_ReturnsUnauthorized_WhenUserNotSet()
	{
		var mockAi = new Mock<IAiService>();
		await using var db = CreateDb();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.Analysis(null, CancellationToken.None);

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
		Assert.Equal("Invalid user identity.", unauthorized.Value);
	}

	[Fact]
	public async Task Analysis_ReturnsOkWithReply_WhenUserSet()
	{
		var mockAi = new Mock<IAiService>();
		mockAi.Setup(x => x.GetAnswerAsync(It.IsAny<string>())).ReturnsAsync("Your emission summary and suggestions.");
		await using var db = CreateDb(withUser: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new AiChatController(mockAi.Object, db, new SensitiveWordService());
		SetUser(controller, user.Id);

		var result = await controller.Analysis(new AiChatController.AnalysisRequestDto { TimeRange = "month" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<AiChatController.ChatResponseDto>(ok.Value);
		Assert.Equal("Your emission summary and suggestions.", dto.Reply);
		mockAi.Verify(x => x.GetAnswerAsync(It.IsAny<string>()), Times.Once);
	}
}
