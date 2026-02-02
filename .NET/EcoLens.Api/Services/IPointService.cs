using System;
using System.Threading.Tasks;

namespace EcoLens.Api.Services;

public interface IPointService
{
	/// <summary>
	/// 检查指定用户在给定日期是否满足“碳中和日”奖励条件，并按规则发放积分与连续天数奖励。
	/// 条件：
	/// - 当日至少有一条 FoodRecord 与一条 TravelLog
	/// - 当日 Food + Travel 排放和 ≤ 15.0 kg CO2e
	/// 规则：
	/// - 达成日奖励 +10 分
	/// - 连续 7 天（7 的倍数）额外 +50 分
	/// - 同一日期仅奖励一次
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="date">要检查的 UTC 日期（仅取 date 部分）</param>
	Task CheckAndAwardPointsAsync(int userId, DateTime date);

	/// <summary>
	/// 植树奖励：当用户新种下一棵或多棵树时，发放对应积分。
	/// 规则：每棵树 +30 分（可后续做成可配置）。
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="treesPlantedCount">本次新增的树数量（正整数）</param>
	Task AwardTreePlantingPointsAsync(int userId, int treesPlantedCount);

	/// <summary>
	/// 计算“当日净碳值”指标：
	/// DailyNetValue = (Steps * StepToCarbonFactor) + (CarbonNeutralBenchmark - DailyTotalEmission)。
	/// 若当日缺少 FoodRecord、TravelLog 或步数（Steps &gt; 0）任一项，则严格返回 0。
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="date">UTC 日期（仅取 date 部分）</param>
	/// <returns>当日净碳值（kg CO2e）</returns>
	Task<decimal> CalculateDailyNetValueAsync(int userId, DateTime date);

	/// <summary>
	/// 重算并更新用户的“总碳减排值”（TotalCarbonSaved）。
	/// 当前策略：按所有有记录的日期累加当日净碳值（不满足三要素则当日记 0）。
	/// </summary>
	/// <param name="userId">用户ID</param>
	Task RecalculateTotalCarbonSavedAsync(int userId);
}


