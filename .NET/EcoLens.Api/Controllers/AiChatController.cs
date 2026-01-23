using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoLens.Api.Services;

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
}


