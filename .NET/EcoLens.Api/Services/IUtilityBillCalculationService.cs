namespace EcoLens.Api.Services;

/// <summary>
/// 水电账单碳排放计算服务接口
/// </summary>
public interface IUtilityBillCalculationService
{
	/// <summary>
	/// 根据用量计算碳排放
	/// </summary>
	/// <param name="electricityUsage">用电量（kWh），可为空</param>
	/// <param name="waterUsage">用水量（m³），可为空</param>
	/// <param name="gasUsage">用气量（kWh 或 m³），可为空</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>碳排放计算结果</returns>
	Task<CarbonEmissionResult> CalculateCarbonEmissionAsync(
		decimal? electricityUsage,
		decimal? waterUsage,
		decimal? gasUsage,
		CancellationToken ct = default);
}

/// <summary>
/// 碳排放计算结果
/// </summary>
public class CarbonEmissionResult
{
	/// <summary>
	/// 电力碳排放（kg CO2）
	/// </summary>
	public decimal ElectricityCarbon { get; set; }

	/// <summary>
	/// 水碳排放（kg CO2）
	/// </summary>
	public decimal WaterCarbon { get; set; }

	/// <summary>
	/// 燃气碳排放（kg CO2）
	/// </summary>
	public decimal GasCarbon { get; set; }

	/// <summary>
	/// 总碳排放（kg CO2）
	/// </summary>
	public decimal TotalCarbon { get; set; }
}
