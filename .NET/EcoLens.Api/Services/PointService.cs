using System;
using System.Linq;
using EcoLens.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

public class PointService : IPointService
{
	private readonly ApplicationDbContext _db;
	private const decimal DailyBenchmark = 15.0m; // kg CO2e
	private const int PointsPerTree = 30;
	private const decimal StepToCarbonFactor = 0.0001m; // kg CO2e per step

	public PointService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task CheckAndAwardPointsAsync(int userId, DateTime date)
	{
		// 统一使用 UTC 日期的 [date, date+1) 区间
		var dayStart = date.Date;
		var dayEnd = dayStart.AddDays(1);

		// 同步读取当日食物与出行记录
		var foodQuery = _db.FoodRecords
			.Where(r => r.UserId == userId && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd);
		var travelQuery = _db.TravelLogs
			.Where(r => r.UserId == userId && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd);

		var hasFood = await foodQuery.AnyAsync();
		var hasTravel = await travelQuery.AnyAsync();
		if (!hasFood || !hasTravel)
		{
			return;
		}

		var foodEmission = await foodQuery.SumAsync(r => (decimal?)r.Emission) ?? 0m;
		var travelEmission = await travelQuery.SumAsync(r => (decimal?)r.CarbonEmission) ?? 0m;
		var totalEmission = foodEmission + travelEmission;

		if (totalEmission > DailyBenchmark)
		{
			return;
		}

		// 加载用户并进行防重复奖励与连击逻辑
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
		if (user is null)
		{
			return;
		}

		// 当日已奖励则忽略
		if (user.LastNeutralDate.HasValue && user.LastNeutralDate.Value.Date == dayStart)
		{
			return;
		}

		// 连续天数：如果昨天是最后一次，则 +1；否则重置为 1
		if (user.LastNeutralDate.HasValue && user.LastNeutralDate.Value.Date == dayStart.AddDays(-1))
		{
			user.ContinuousNeutralDays = Math.Max(0, user.ContinuousNeutralDays) + 1;
		}
		else
		{
			user.ContinuousNeutralDays = 1;
		}

		// 基础奖励 +10
		user.CurrentPoints += 10;

		// 每满 7 天额外 +50（7、14、21...）
		if (user.ContinuousNeutralDays % 7 == 0)
		{
			user.CurrentPoints += 50;
		}

		user.LastNeutralDate = dayStart;

		await _db.SaveChangesAsync();
	}

	public async Task AwardTreePlantingPointsAsync(int userId, int treesPlantedCount)
	{
		if (treesPlantedCount <= 0)
		{
			return;
		}

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
		if (user is null)
		{
			return;
		}

		user.CurrentPoints += treesPlantedCount * PointsPerTree;
		await _db.SaveChangesAsync();
	}

	public async Task<decimal> CalculateDailyNetValueAsync(int userId, DateTime date)
	{
		var dayStart = date.Date;
		var dayEnd = dayStart.AddDays(1);

		// 基础存在性检查
		var hasFood = await _db.FoodRecords.AnyAsync(r => r.UserId == userId && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd);
		var hasTravel = await _db.TravelLogs.AnyAsync(r => r.UserId == userId && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd);
		var stepRecord = await _db.StepRecords.FirstOrDefaultAsync(r => r.UserId == userId && r.RecordDate == dayStart);
		var steps = stepRecord?.StepCount ?? 0;

		if (!hasFood || !hasTravel || steps <= 0)
		{
			return 0m;
		}

		var foodEmission = await _db.FoodRecords
			.Where(r => r.UserId == userId && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
			.SumAsync(r => (decimal?)r.Emission) ?? 0m;
		var travelEmission = await _db.TravelLogs
			.Where(r => r.UserId == userId && r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
			.SumAsync(r => (decimal?)r.CarbonEmission) ?? 0m;

		var dailyTotalEmission = foodEmission + travelEmission;
		var stepSaving = (decimal)steps * StepToCarbonFactor;

		var net = stepSaving + (DailyBenchmark - dailyTotalEmission);
		return net;
	}

	public async Task RecalculateTotalCarbonSavedAsync(int userId)
	{
		// 收集该用户所有参与计算的日期（来自步数、食物与出行）
		var stepDates = await _db.StepRecords.Where(r => r.UserId == userId).Select(r => r.RecordDate.Date).Distinct().ToListAsync();
		var foodDates = await _db.FoodRecords.Where(r => r.UserId == userId).Select(r => r.CreatedAt.Date).Distinct().ToListAsync();
		var travelDates = await _db.TravelLogs.Where(r => r.UserId == userId).Select(r => r.CreatedAt.Date).Distinct().ToListAsync();

		var allDates = stepDates
			.Concat(foodDates)
			.Concat(travelDates)
			.Distinct()
			.OrderBy(d => d)
			.ToList();

		decimal total = 0m;
		foreach (var d in allDates)
		{
			total += await CalculateDailyNetValueAsync(userId, d);
		}

		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
		if (user is null) return;

		// TotalCarbonSaved 列精度为 (18,2)，此处四舍五入到 2 位
		user.TotalCarbonSaved = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
		await _db.SaveChangesAsync();
	}
}


