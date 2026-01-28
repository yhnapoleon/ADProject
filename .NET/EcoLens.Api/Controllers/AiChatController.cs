using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoLens.Api.Services;
using System.Collections.Generic;
using System.Linq;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiChatController : ControllerBase
{
	private readonly IAiService _aiService;

	public AiChatController(IAiService aiService)
	{
		_aiService = aiService;
	}

	public class ChatRequestDto
	{
		public string Message { get; set; } = string.Empty;
	}

	public class ChatResponseDto
	{
		public string Reply { get; set; } = string.Empty;
	}

	[HttpPost("chat")]
	public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto dto, CancellationToken ct)
	{
		var message = (dto.Message ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(message))
		{
			return BadRequest("Message is required.");
		}

		var answer = await _aiService.GetAnswerAsync(message);
		return Ok(new ChatResponseDto { Reply = answer });
	}

	// --- Alias to match frontend spec: /api/assistant/chat with messages[] payload ---
	public class ChatMessageItem
	{
		public string Role { get; set; } = "user"; // "user" | "assistant"
		public string Content { get; set; } = string.Empty;
	}

	public class MessagesChatRequestDto
	{
		public List<ChatMessageItem> Messages { get; set; } = new();
		public bool? Stream { get; set; }
	}

	[HttpPost("/api/assistant/chat")]
	public async Task<ActionResult<object>> AssistantChat([FromBody] MessagesChatRequestDto req, CancellationToken ct)
	{
		if (req?.Messages == null || req.Messages.Count == 0)
		{
			return BadRequest("messages is required.");
		}

		var lastUser = req.Messages.LastOrDefault(m => string.Equals(m.Role, "user", System.StringComparison.OrdinalIgnoreCase));
		var prompt = (lastUser?.Content ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(prompt))
		{
			// fallback: take last message regardless role
			prompt = (req.Messages.LastOrDefault()?.Content ?? string.Empty).Trim();
		}
		if (string.IsNullOrWhiteSpace(prompt))
		{
			return BadRequest("No content to answer.");
		}

		var reply = await _aiService.GetAnswerAsync(prompt);
		// Shape aligned with spec: { message: { role, content } }
		return Ok(new
		{
			message = new { role = "assistant", content = reply }
		});
	}
}


