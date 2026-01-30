namespace EcoLens.Api.Services;

/// <summary>
/// 文档类型分类器接口
/// 用于识别上传的图片是否为水电账单
/// </summary>
public interface IDocumentTypeClassifier
{
	/// <summary>
	/// 对 OCR 识别的文本进行分类，判断文档类型
	/// </summary>
	/// <param name="ocrText">OCR 识别的文本</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>分类结果</returns>
	Task<DocumentTypeClassificationResult> ClassifyAsync(string ocrText, CancellationToken ct = default);
}

/// <summary>
/// 文档类型枚举
/// </summary>
public enum DocumentType
{
	/// <summary>
	/// 水电账单
	/// </summary>
	UtilityBill = 0,

	/// <summary>
	/// 机票/登机牌
	/// </summary>
	FlightTicket = 1,

	/// <summary>
	/// 收据
	/// </summary>
	Receipt = 2,

	/// <summary>
	/// 发票
	/// </summary>
	Invoice = 3,

	/// <summary>
	/// 未知类型
	/// </summary>
	Unknown = 99
}

/// <summary>
/// 文档类型分类结果
/// </summary>
public class DocumentTypeClassificationResult
{
	/// <summary>
	/// 检测到的文档类型
	/// </summary>
	public DocumentType DocumentType { get; set; }

	/// <summary>
	/// 是否为水电账单
	/// </summary>
	public bool IsUtilityBill => DocumentType == DocumentType.UtilityBill;

	/// <summary>
	/// 置信度得分（0-100）
	/// </summary>
	public int ConfidenceScore { get; set; }

	/// <summary>
	/// 匹配到的关键词列表（用于调试）
	/// </summary>
	public List<string> MatchedKeywords { get; set; } = new();

	/// <summary>
	/// 错误消息（如果不是账单）
	/// </summary>
	public string? ErrorMessage { get; set; }
}
