namespace EcoLens.Api.Services;

/// <summary>
/// 敏感词检测服务接口
/// </summary>
public interface ISensitiveWordService
{
	/// <summary>
	/// 检测文本中是否包含敏感词
	/// </summary>
	/// <param name="text">要检测的文本</param>
	/// <returns>如果包含敏感词，返回第一个匹配的敏感词；否则返回 null</returns>
	string? ContainsSensitiveWord(string text);
}
