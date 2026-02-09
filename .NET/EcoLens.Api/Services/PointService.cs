using System;
using System.Linq;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
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
		// Use UTC date range [date, date+1) uniformly
		var dayStart = date.Date;
		var dayEnd = dayStart.AddDays(1);

		// Synchronously read food and travel records for the day
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

		// Load user and apply duplicate prevention and streak logic
		var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
		if (user is null)
		{
			return;
		}

		// Skip if already awarded today
		if (user.LastNeutralDate.HasValue && user.LastNeutralDate.Value.Date == dayStart)
		{
			return;
		}

		// Consecutive days: if yesterday was the last time, +1; otherwise reset to 1
		if (user.LastNeutralDate.HasValue && user.LastNeutralDate.Value.Date == dayStart.AddDays(-1))
		{
			user.ContinuousNeutralDays = Math.Max(0, user.ContinuousNeutralDays) + 1;
		}
		else
		{
			user.ContinuousNeutralDays = 1;
		}

		// Base reward +10
		user.CurrentPoints += 10;
		await _db.PointAwardLogs.AddAsync(new PointAwardLog
		{
			UserId = userId,
			Points = 10,
			AwardedAt = dayStart,
			Source = "DailyNeutral"
		});

		// Every 7 days additional +50 (7, 14, 21...)
		if (user.ContinuousNeutralDays % 7 == 0)
		{
			user.CurrentPoints += 50;
			await _db.PointAwardLogs.AddAsync(new PointAwardLog
			{
				UserId = userId,
				Points = 50,
				AwardedAt = dayStart,
				Source = "WeeklyBonus"
			});
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

		var points = treesPlantedCount * PointsPerTree;
		user.CurrentPoints += points;
		await _db.PointAwardLogs.AddAsync(new PointAwardLog
		{
			UserId = userId,
			Points = points,
			AwardedAt = DateTime.UtcNow,
			Source = "TreePlanting"
		});
		await _db.SaveChangesAsync();
	}

	public async Task<decimal> CalculateDailyNetValueAsync(int userId, DateTime date)
	{
		var dayStart = date.Date;
		var dayEnd = dayStart.AddDays(1);

		// Basic existence check
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
		// Collect all dates for this user that participate in calculation (from steps, food, and travel)
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

		// TotalCarbonSaved column precision is (18,2), round to 2 decimal places here
		user.TotalCarbonSaved = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
		await _db.SaveChangesAsync();
	}

	public async Task LogPointAwardAsync(int userId, int points, DateTime awardedAt, string source)
	{
		if (points <= 0) return;
		await _db.PointAwardLogs.AddAsync(new PointAwardLog
		{
			UserId = userId,
			Points = points,
			AwardedAt = awardedAt.Date,
			Source = source
		});
		await _db.SaveChangesAsync();
	}
}


