using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoLens.Api.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EcoLens.Api.Data;
using EcoLens.Api.Models;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiChatController : ControllerBase
{
	private readonly IAiService _aiService;
	private readonly ApplicationDbContext _context;

	public AiChatController(IAiService aiService, ApplicationDbContext context)
	{
		_aiService = aiService;
		_context = context;
	}

	public class ChatRequestDto
	{
		public string Message { get; set; } = string.Empty;
	}

	public class ChatResponseDto
	{
		public string Reply { get; set; } = string.Empty;
	}

	public class AnalysisRequestDto
	{
		public string TimeRange { get; set; } = "month";
	}

	[AllowAnonymous]
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

	[AllowAnonymous]
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

	[HttpPost("analysis")]
	public async Task<ActionResult<ChatResponseDto>> Analysis([FromBody] AnalysisRequestDto? req, CancellationToken ct)
	{
		// 获取用户ID（兼容不同 Claim 类型）
		var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst("sub")?.Value;
		if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
		{
			return Unauthorized("Invalid user identity.");
		}

		// 时间范围
		var range = (req?.TimeRange ?? "month").Trim().ToLowerInvariant();
		var nowUtc = DateTime.UtcNow;
		var startDate = range == "week" ? nowUtc.AddDays(-7) : nowUtc.AddDays(-30);
		var periodDesc = range == "week" ? "近7天" : "近30天";

		// 聚合数据（空集安全求和）
		var foodEmission = await _context.FoodRecords
			.AsNoTracking()
			.Where(f => f.UserId == userId && f.CreatedAt >= startDate)
			.SumAsync(f => (decimal?)f.Emission, ct) ?? 0m;

		var travelEmission = await _context.TravelLogs
			.AsNoTracking()
			.Where(t => t.UserId == userId && t.CreatedAt >= startDate)
			.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

		var utilityEmission = await _context.UtilityBills
			.AsNoTracking()
			.Where(b => b.UserId == userId && b.BillPeriodEnd >= startDate)
			.SumAsync(b => (decimal?)b.TotalCarbonEmission, ct) ?? 0m;

		var totalEmission = foodEmission + travelEmission + utilityEmission;

		// 4. 构建结构化 Prompt (针对纯文本 + 固定点数优化)
		string prompt = $@"
角色：环境科学顾问
任务：根据用户碳排放数据（{periodDesc}）提供简报。

【用户数据】
- 食物：{foodEmission:F2} kg
- 出行：{travelEmission:F2} kg
- 水电：{utilityEmission:F2} kg
- 总计：{totalEmission:F2} kg

【输出要求】
1. **格式**：纯文本 (Plain Text)。禁止使用 Markdown（不要出现 #、**、## 等）。
2. **结构**：严格包含 1 行现状简评 和 3 条具体建议，按 1. 2. 3. 编号。
3. **字数**：总字数严格少于 200 字，措辞简洁直接。
4. **现状**：必须点明占比最高的排放源并做一句话评价。

【输出模板】
现状：(一句话指出最大排放源及评价)
1. (建议一)
2. (建议二)
3. (建议三)
";

		var reply = await _aiService.GetAnswerAsync(prompt);
		return Ok(new ChatResponseDto { Reply = reply ?? string.Empty });
	}
}


