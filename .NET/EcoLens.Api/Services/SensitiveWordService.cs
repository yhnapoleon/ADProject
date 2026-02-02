using System.Text.RegularExpressions;

namespace EcoLens.Api.Services;

/// <summary>
/// 敏感词检测服务实现
/// </summary>
public class SensitiveWordService : ISensitiveWordService
{
	private readonly HashSet<string> _sensitiveWords;
	private readonly Regex _wordBoundaryRegex;

	public SensitiveWordService()
	{
		// 初始化敏感词列表（包含常见脏话、不当用语等）
		_sensitiveWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			// 脏话类（英文）
			"fuck", "fucking", "fucked", "shit", "shitting", "damn", "damned",
			"bitch", "bitches", "asshole", "bastard", "crap", "piss", "pissed",
			
			// 中文拼音缩写（脏话）
			"sb", "nt", "cnm", "nmsl", "wcnm",
			
			// 歧视性用语
			"nigger", "nigga", "chink", "gook", "kike",
			
			// 系统保留词（避免与系统账户混淆）
			"administrator", "root", "system"
		};

		// 创建单词边界正则表达式，用于更精确的匹配
		_wordBoundaryRegex = new Regex(@"\b", RegexOptions.Compiled);
	}

	/// <summary>
	/// 检测文本中是否包含敏感词
	/// </summary>
	public string? ContainsSensitiveWord(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		// 转换为小写以便不区分大小写匹配
		var lowerText = text.ToLowerInvariant();

		// 检查是否包含完整的敏感词
		foreach (var word in _sensitiveWords)
		{
			// 使用单词边界匹配，避免部分匹配（例如 "class" 不会匹配 "ass"）
			var pattern = $@"\b{Regex.Escape(word)}\b";
			if (Regex.IsMatch(lowerText, pattern, RegexOptions.IgnoreCase))
			{
				return word;
			}

			// 也检查直接包含（用于中文等没有明确单词边界的语言）
			if (lowerText.Contains(word, StringComparison.OrdinalIgnoreCase))
			{
				return word;
			}
		}

		return null;
	}
}
