using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Services;

/// <summary>
/// 水电账单数据提取服务接口
/// </summary>
public interface IUtilityBillParser
{
	/// <summary>
	/// 从OCR识别的文本中提取账单数据
	/// </summary>
	/// <param name="ocrText">OCR识别的文本</param>
	/// <param name="expectedType">预期的账单类型（可选，用于提高识别准确性）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>提取的账单数据，如果提取失败返回null</returns>
	Task<ExtractedBillData?> ParseBillDataAsync(string ocrText, UtilityBillType? expectedType = null, CancellationToken ct = default);
}

/// <summary>
/// 提取的账单数据
/// </summary>
public class ExtractedBillData
{
	/// <summary>
	/// 账单类型
	/// </summary>
	public UtilityBillType BillType { get; set; }

	/// <summary>
	/// 用电量（kWh）
	/// </summary>
	public decimal? ElectricityUsage { get; set; }

	/// <summary>
	/// 用水量（m³）
	/// </summary>
	public decimal? WaterUsage { get; set; }

	/// <summary>
	/// 用气量（kWh 或 m³）
	/// </summary>
	public decimal? GasUsage { get; set; }

	/// <summary>
	/// 账单周期开始日期
	/// </summary>
	public DateTime? BillPeriodStart { get; set; }

	/// <summary>
	/// 账单周期结束日期
	/// </summary>
	public DateTime? BillPeriodEnd { get; set; }

	/// <summary>
	/// 提取置信度（0-1）
	/// </summary>
	public decimal Confidence { get; set; }
}
