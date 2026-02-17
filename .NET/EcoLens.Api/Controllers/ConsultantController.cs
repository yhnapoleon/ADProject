using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoLens.Api.Services;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/consultant")]
[Authorize]
public class ConsultantController : ControllerBase
{
	private readonly IAiService _aiService;

	public ConsultantController(IAiService aiService)
	{
		_aiService = aiService;
	}

	public class ChatQuestionDto
	{
		public string Question { get; set; } = string.Empty;
	}

	public class ChatAnswerDto
	{
		public string Answer { get; set; } = string.Empty;
	}

	[HttpPost("chat")]
	public async Task<ActionResult<ChatAnswerDto>> Chat([FromBody] ChatQuestionDto dto, CancellationToken ct)
	{
		var question = (dto.Question ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(question))
		{
			return BadRequest("question is required.");
		}

		var result = await _aiService.GetAnswerAsync(question);
		return Ok(new ChatAnswerDto { Answer = result });
	}
}


