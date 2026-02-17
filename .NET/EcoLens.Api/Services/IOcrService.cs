namespace EcoLens.Api.Services;

/// <summary>
/// OCR 服务接口
/// </summary>
public interface IOcrService
{
	/// <summary>
	/// 识别图片或PDF中的文字（从Stream）
	/// </summary>
	/// <param name="imageStream">图片或PDF的Stream</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>OCR识别结果，包含文本、置信度和页数</returns>
	Task<OcrResult?> RecognizeTextAsync(Stream imageStream, CancellationToken ct = default);

	/// <summary>
	/// 识别图片或PDF中的文字（从字节数组）
	/// </summary>
	/// <param name="imageBytes">图片或PDF的字节数组</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>OCR识别结果，包含文本、置信度和页数</returns>
	Task<OcrResult?> RecognizeTextAsync(byte[] imageBytes, CancellationToken ct = default);
}

/// <summary>
/// OCR识别结果
/// </summary>
public class OcrResult
{
	/// <summary>
	/// 识别的文本内容
	/// </summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// 识别置信度（0-1）
	/// </summary>
	public decimal Confidence { get; set; }

	/// <summary>
	/// 页数（PDF时有用，图片为1）
	/// </summary>
	public int Pages { get; set; } = 1;
}
