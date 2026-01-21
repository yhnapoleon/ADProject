using System.Security.Claims;
using EcoLens.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiChatController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public AiChatController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	public class ChatRequestDto
	{
		public string Message { get; set; } = string.Empty;
	}

	public class ChatResponseDto
	{
		public string Reply { get; set; } = string.Empty;
	}

	/// <summary>
	/// 模拟 AI 对话接口。
	/// - 若用户问“如何减排”，返回环保建议；
	/// - 若用户问“我的数据”，查询该用户 TotalCarbonSaved 并生成表扬语。
	/// 
	/// 替换为真实大模型调用（Google Gemini / OpenAI）的方法：
	/// 1) 注入配置与 SDK 客户端（例如 IOpenAIClient 或 GeminiClient）；
	/// 2) 将历史对话与当前 message 拼接为 prompt；
	/// 3) 调用 chat/completions 或 generateContent 接口，返回模型输出。
	/// </summary>
	[HttpPost("chat")]
	public async Task<ActionResult<ChatResponseDto>> Chat([FromBody] ChatRequestDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var message = (dto.Message ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(message))
		{
			return BadRequest("Message is required.");
		}

		// Mock 逻辑分支
		if (message.Contains("如何减排", StringComparison.OrdinalIgnoreCase))
		{
			return Ok(new ChatResponseDto
			{
				Reply = "以下是减排建议：优先选择公共交通或骑行，减少一次性塑料；多吃蔬果少吃红肉；使用节能电器并及时关灯；合理设置空调温度；尽量本地采购以减少运输。"
			});
		}

		if (message.Contains("我的数据", StringComparison.OrdinalIgnoreCase))
		{
			var me = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
			var saved = me?.TotalCarbonSaved ?? 0m;
			return Ok(new ChatResponseDto
			{
				Reply = $"很棒！你已累计减排约 {saved:0.##} kg CO₂。继续保持低碳生活方式，你正在为地球带来积极改变！"
			});
		}

		// 默认回复（模拟）
		return Ok(new ChatResponseDto
		{
			Reply = "我是 EcoLens 助手。你可以问我如何更环保，或说“我的数据”了解你的个人减排表现。"
		});
	}
}


