using System;
using System.Threading.Tasks;

namespace EcoLens.Api.Services;

public interface IPointService
{
	/// <summary>
	/// Check if the specified user meets the "carbon neutral day" reward conditions on the given date, and award points and consecutive days rewards according to rules.
	/// Conditions:
	/// - At least one FoodRecord and one TravelLog on that day
	/// - Food + Travel emissions sum ≤ 15.0 kg CO2e
	/// Rules:
	/// - Achievement day reward: +10 points
	/// - Every 7 consecutive days (multiples of 7): additional +50 points
	/// - Only reward once per date
	/// </summary>
	/// <param name="userId">User ID</param>
	/// <param name="date">UTC date to check (date part only)</param>
	Task CheckAndAwardPointsAsync(int userId, DateTime date);

	/// <summary>
	/// Tree planting reward: Award corresponding points when a user plants one or more new trees.
	/// Rule: +30 points per tree (can be made configurable later).
	/// </summary>
	/// <param name="userId">User ID</param>
	/// <param name="treesPlantedCount">Number of trees planted this time (positive integer)</param>
	Task AwardTreePlantingPointsAsync(int userId, int treesPlantedCount);

	/// <summary>
	/// Calculate "daily net carbon value" metric:
	/// DailyNetValue = (Steps * StepToCarbonFactor) + (CarbonNeutralBenchmark - DailyTotalEmission).
	/// If any of FoodRecord, TravelLog, or steps (Steps &gt; 0) is missing on that day, strictly return 0.
	/// </summary>
	/// <param name="userId">User ID</param>
	/// <param name="date">UTC date (date part only)</param>
	/// <returns>Daily net carbon value (kg CO2e)</returns>
	Task<decimal> CalculateDailyNetValueAsync(int userId, DateTime date);

	/// <summary>
	/// Recalculate and update user's "total carbon saved" (TotalCarbonSaved).
	/// Current strategy: Sum daily net carbon values for all dates with records (if three elements not met, record 0 for that day).
	/// </summary>
	/// <param name="userId">User ID</param>
	Task RecalculateTotalCarbonSavedAsync(int userId);

	/// <summary>
	/// Only log point award (does not modify CurrentPoints), used for scenarios like steps where other logic adds points, facilitating today/monthly point statistics.
	/// </summary>
	/// <param name="userId">User ID</param>
	/// <param name="points">Number of points</param>
	/// <param name="awardedAt">Date the points belong to (UTC)</param>
	/// <param name="source">Source, e.g. Step / TreePlanting</param>
	Task LogPointAwardAsync(int userId, int points, DateTime awardedAt, string source);
}
