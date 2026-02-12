using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoLens.Api.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Linq.Expressions;
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
	private readonly ISensitiveWordService _sensitiveWordService;

	public AiChatController(IAiService aiService, ApplicationDbContext context, ISensitiveWordService sensitiveWordService)
	{
		_aiService = aiService;
		_context = context;
		_sensitiveWordService = sensitiveWordService;
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
		// ========== 阶段一：请求与防御 (Guardrail) ==========
		var message = (dto.Message ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(message))
		{
			return BadRequest("Message is required.");
		}

		var hit = _sensitiveWordService.ContainsSensitiveWord(message);
		if (hit != null)
		{
			// 不回显命中的敏感词，避免“提示绕过”。
			return BadRequest("对不起，我无法协助处理包含不当内容的请求。请调整问题后再试。");
		}

		var reply = await GenerateSqlRagReplyAsync(message, ct);
		return Ok(new ChatResponseDto { Reply = reply });
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

		// ========== 阶段一：请求与防御 (Guardrail) ==========
		var hit = _sensitiveWordService.ContainsSensitiveWord(prompt);
		if (hit != null)
		{
			return BadRequest("对不起，我无法协助处理包含不当内容的请求。请调整问题后再试。");
		}

		var reply = await GenerateSqlRagReplyAsync(prompt, ct);
		// Shape aligned with spec: { message: { role, content } }
		return Ok(new
		{
			message = new { role = "assistant", content = reply }
		});
	}

	/// <summary>
	/// 基于 SQL 的双阶段 RAG：Pass1(意图关键词)->SQL 混合检索->Context->Pass2(专家回答)->清洗输出
	/// </summary>
	private async Task<string> GenerateSqlRagReplyAsync(string userMessage, CancellationToken ct)
	{
		// ========== 阶段二：意图理解 (Pass 1 - Keyword Extraction) ==========
		var keywords = await ExtractIntentKeywordsAsync(userMessage, ct);

		// ========== 阶段三：混合数据检索 (Hybrid Retrieval) ==========
		var userId = TryGetUserId(User);

		// 事实数据：CarbonReferences（按 LabelName LIKE 模糊匹配）
		var carbonRefs = await QueryCarbonReferencesByKeywordsAsync(keywords, ct);

		// 用户数据：当前用户积分、累计排放等（可能为匿名）
		ApplicationUser? me = null;
		if (userId.HasValue)
		{
			me = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
		}

		// 上下文组装
		var context = BuildNaturalLanguageContext(carbonRefs, me, keywords);

		// ========== 阶段四：引导式生成 (Pass 2 - Expert Generation) ==========
		var systemPrompt = BuildExpertSystemPrompt();
		var finalUserPrompt = BuildExpertUserPrompt(userMessage, context, hasFacts: carbonRefs.Count > 0);
		var raw = await _aiService.GetAnswerAsync(finalUserPrompt, systemPrompt, ct);

		// ========== 阶段五：输出清洗 ==========
		return CleanAiOutput(raw);
	}

	// -------------------- 阶段二：Pass 1 关键词提取 --------------------
	private async Task<List<string>> ExtractIntentKeywordsAsync(string userMessage, CancellationToken ct)
	{
		// System Prompt：要求仅返回 JSON 字符串数组（英文 Label）
		var systemPrompt = """
你是一个信息抽取器。你的任务：
1) 从用户问题中提取“可用于数据库检索的实体/概念”，并转换为英文数据库标签（Label）。
2) 仅输出 JSON 字符串数组，例如：["Beef","Car"]。
3) 禁止输出任何解释、Markdown、代码块、额外字段；不要输出对象，只能是数组。
4) 如果无法提取，输出空数组：[]。
""";

		// User Prompt：原始问题
		var userPrompt = userMessage.Trim();

		var raw = await _aiService.GetAnswerAsync(userPrompt, systemPrompt, ct);
		return ParseJsonStringArrayOrEmpty(raw);
	}

	private static List<string> ParseJsonStringArrayOrEmpty(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return new List<string>();
		}

		// 优先直接解析
		if (TryParseJsonStringArray(text, out var direct))
		{
			return direct;
		}

		// 兜底：从文本里抓出第一个 [...] 片段再解析（防止模型包裹了多余字符）
		var m = Regex.Match(text, @"\[[\s\S]*\]");
		if (m.Success && TryParseJsonStringArray(m.Value, out var extracted))
		{
			return extracted;
		}

		return new List<string>();
	}

	private static bool TryParseJsonStringArray(string text, out List<string> list)
	{
		list = new List<string>();

		try
		{
			var arr = JsonSerializer.Deserialize<List<string>>(text, new JsonSerializerOptions
			{
				AllowTrailingCommas = true
			});

			if (arr == null)
			{
				return false;
			}

			// 简单规整：去空、去重、限制长度、限制字符集（避免注入/异常匹配）
			list = arr
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s.Trim())
				.Where(s => s.Length <= 64)
				.Where(s => Regex.IsMatch(s, @"^[A-Za-z0-9 _\-\.\+]+$"))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(8)
				.ToList();

			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	// -------------------- 阶段三：SQL 混合检索 --------------------
	private async Task<List<CarbonReference>> QueryCarbonReferencesByKeywordsAsync(List<string> keywords, CancellationToken ct)
	{
		if (keywords == null || keywords.Count == 0)
		{
			return new List<CarbonReference>();
		}

		// 动态拼 OR：LabelName LIKE %kw1% OR ... （EF 可翻译，走 SQL）
		Expression<Func<CarbonReference, bool>> predicate = c => false;
		var param = predicate.Parameters[0];

		Expression body = predicate.Body;
		foreach (var kw in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
		{
			var likePattern = Expression.Constant($"%{kw}%");
			var labelProp = Expression.Property(param, nameof(CarbonReference.LabelName));
			var efFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);
			var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
				nameof(DbFunctionsExtensions.Like),
				new[] { typeof(DbFunctions), typeof(string), typeof(string) }
			)!;

			var likeCall = Expression.Call(likeMethod, efFunctions, labelProp, likePattern);
			body = Expression.OrElse(body, likeCall);
		}

		predicate = Expression.Lambda<Func<CarbonReference, bool>>(body, param);

		return await _context.CarbonReferences
			.AsNoTracking()
			.Where(predicate)
			.OrderBy(c => c.LabelName)
			.Take(30)
			.ToListAsync(ct);
	}

	private static string BuildNaturalLanguageContext(List<CarbonReference> refs, ApplicationUser? user, List<string> extractedKeywords)
	{
		var sb = new StringBuilder();

		sb.AppendLine("【数据库检索关键词】");
		if (extractedKeywords == null || extractedKeywords.Count == 0)
		{
			sb.AppendLine("- （无）");
		}
		else
		{
			sb.AppendLine("- " + string.Join(", ", extractedKeywords));
		}

		sb.AppendLine();
		sb.AppendLine("【碳排放事实（来自 CarbonReferences）】");
		if (refs == null || refs.Count == 0)
		{
			sb.AppendLine("- 未找到具体数据库记录。");
		}
		else
		{
			foreach (var r in refs)
			{
				// 说明：文档里叫 CarbonEmission，本库中字段为 Co2Factor（单位见 Unit）。
				sb.AppendLine($"- Label={r.LabelName} | Category={r.Category} | CarbonEmission={r.Co2Factor} {r.Unit} | Source={r.Source}");
			}
		}

		sb.AppendLine();
		sb.AppendLine("【用户当前状态】");
		if (user == null)
		{
			sb.AppendLine("- 用户未登录或未能识别身份，无法读取积分/累计排放。");
		}
		else
		{
			sb.AppendLine($"- Points={user.CurrentPoints}");
			sb.AppendLine($"- TotalCarbonEmission={user.TotalCarbonEmission:F4} kg CO2e");
			sb.AppendLine($"- TotalCarbonSaved={user.TotalCarbonSaved:F2} kg CO2e");
			sb.AppendLine($"- Region={user.Region}");
		}

		return sb.ToString().Trim();
	}

	// -------------------- 阶段四：专家生成 --------------------
	private static string BuildExpertSystemPrompt()
	{
		// 角色设定：环保科学顾问
		return """
你是一名环保科学顾问（Environmental Science Advisor）。
你的目标：基于提供的【Context】与用户问题，给出可执行、可量化的低碳建议。
要求：
- 输出为纯文本，禁止 Markdown（不要出现 #、**、``` 等）。
- 如果 Context 明确写明“未找到具体数据库记录”，请明确告诉用户你将基于通用环保知识回答，并避免捏造数据库数值。
- 回答要避免夸大其词，尽量给出具体建议（例如替代方案、频次、范围）。
""";
	}

	private static string BuildExpertUserPrompt(string userMessage, string context, bool hasFacts)
	{
		// 思维链（CoT）：要求先 Thought 再 Answer（后端会清洗并只返回 Answer 给前端）
		var notFoundHint = hasFacts
			? string.Empty
			: "提示：未找到具体数据库记录，请基于通用环保知识回答。";

		return $"""
【Context】
{context}

{notFoundHint}

【User Question】
{userMessage}

【Output Format】
Thought: （先简要推理你的依据与步骤）
Answer: （给用户的最终建议，清晰、可执行、尽量量化）
""";
	}

	// -------------------- 阶段五：输出清洗 --------------------
	private static string CleanAiOutput(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return string.Empty;
		}

		var text = raw.Trim();

		// 1) 若模型返回 JSON（例如 {"answer":"..."}），尽量解析出 answer 字段
		try
		{
			using var doc = JsonDocument.Parse(text);
			if (doc.RootElement.ValueKind == JsonValueKind.Object)
			{
				if (TryGetString(doc.RootElement, "answer", out var answer) ||
				    TryGetString(doc.RootElement, "reply", out answer) ||
				    TryGetString(doc.RootElement, "content", out answer))
				{
					text = answer.Trim();
				}
			}
		}
		catch (JsonException)
		{
			// ignore
		}

		// 2) 若包含 Thought/Answer 分段，只保留 Answer
		var m = Regex.Match(text, @"(?is)\bAnswer\s*:\s*(.+)$");
		if (m.Success)
		{
			text = m.Groups[1].Value.Trim();
		}

		// 3) 去除 Markdown / 代码围栏 / JSON 标记残留
		text = Regex.Replace(text, @"(?is)```.*?```", string.Empty).Trim();
		text = text.Replace("**", string.Empty, StringComparison.Ordinal);
		text = Regex.Replace(text, @"(?m)^\s*#+\s*", string.Empty).Trim(); // headings
		text = text.Replace("`", string.Empty, StringComparison.Ordinal);

		// 4) 收尾规整
		text = Regex.Replace(text, @"\n{3,}", "\n\n").Trim();

		return text;
	}

	private static bool TryGetString(JsonElement obj, string name, out string value)
	{
		value = string.Empty;
		if (obj.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
		{
			value = prop.GetString() ?? string.Empty;
			return !string.IsNullOrWhiteSpace(value);
		}
		return false;
	}

	private static int? TryGetUserId(ClaimsPrincipal? user)
	{
		var userIdStr = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
		return int.TryParse(userIdStr, out var userId) ? userId : null;
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


