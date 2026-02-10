using System;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Services;
using EcoLens.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests;

public class PointServiceTests
{
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AwardTreePlantingPointsAsync_ShouldIncreasePoints_AndLogAward()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var user = new ApplicationUser
        {
            Id = 1,
            Username = "test-user",
            Email = "test@example.com",
            PasswordHash = "hash",
            Region = "SG",
            BirthDate = DateTime.UtcNow.Date,
            CurrentPoints = 0
        };
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var service = new PointService(db);

        // Act
        await service.AwardTreePlantingPointsAsync(userId: 1, treesPlantedCount: 2);

        // Assert
        var reloadedUser = await db.ApplicationUsers.FirstAsync(u => u.Id == 1);
        Assert.Equal(2 * 30, reloadedUser.CurrentPoints); // PointsPerTree = 30

        var log = await db.PointAwardLogs.SingleAsync();
        Assert.Equal(1, log.UserId);
        Assert.Equal(2 * 30, log.Points);
        Assert.Equal("TreePlanting", log.Source);
    }

    [Fact]
    public async Task AwardTreePlantingPointsAsync_ShouldDoNothing_WhenTreesCountIsNonPositive()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var user = new ApplicationUser
        {
            Id = 1,
            Username = "test-user",
            Email = "test@example.com",
            PasswordHash = "hash",
            Region = "SG",
            BirthDate = DateTime.UtcNow.Date,
            CurrentPoints = 10
        };
        db.ApplicationUsers.Add(user);
        await db.SaveChangesAsync();

        var service = new PointService(db);

        // Act
        await service.AwardTreePlantingPointsAsync(userId: 1, treesPlantedCount: 0);

        // Assert
        var reloadedUser = await db.ApplicationUsers.FirstAsync(u => u.Id == 1);
        Assert.Equal(10, reloadedUser.CurrentPoints);
        Assert.Empty(await db.PointAwardLogs.ToListAsync());
    }

    [Fact]
    public async Task CheckAndAwardPointsAsync_ShouldAwardPoints_WhenDailyEmissionBelowBenchmark()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var day = DateTime.UtcNow.Date;

        var user = new ApplicationUser
        {
            Id = 1,
            Username = "test-user",
            Email = "test@example.com",
            PasswordHash = "hash",
            Region = "SG",
            BirthDate = DateTime.UtcNow.Date,
            CurrentPoints = 0,
            ContinuousNeutralDays = 0
        };
        db.ApplicationUsers.Add(user);

        db.FoodRecords.Add(new FoodRecord
        {
            UserId = 1,
            Name = "Salad",
            Amount = 1,
            EmissionFactor = 5m,
            Emission = 5m
        });
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 5m
        });

        await db.SaveChangesAsync();
        var service = new PointService(db);

        // Act
        await service.CheckAndAwardPointsAsync(userId: 1, date: day);

        // Assert
        var reloadedUser = await db.ApplicationUsers.FirstAsync(u => u.Id == 1);
        Assert.Equal(10, reloadedUser.CurrentPoints);
        Assert.Equal(1, reloadedUser.ContinuousNeutralDays);
        Assert.Equal(day.Date, reloadedUser.LastNeutralDate);

        var logs = await db.PointAwardLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(10, logs[0].Points);
        Assert.Equal("DailyNeutral", logs[0].Source);
    }

    [Fact]
    public async Task CheckAndAwardPointsAsync_ShouldNotAward_WhenEmissionAboveBenchmark()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var day = DateTime.UtcNow.Date;

        var user = new ApplicationUser
        {
            Id = 1,
            Username = "test-user",
            Email = "test@example.com",
            PasswordHash = "hash",
            Region = "SG",
            BirthDate = DateTime.UtcNow.Date,
            CurrentPoints = 0
        };
        db.ApplicationUsers.Add(user);

        db.FoodRecords.Add(new FoodRecord
        {
            UserId = 1,
            Name = "Steak",
            Amount = 1,
            EmissionFactor = 20m,
            Emission = 20m
        });
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.CarGasoline,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 1m
        });

        await db.SaveChangesAsync();
        var service = new PointService(db);

        // Act
        await service.CheckAndAwardPointsAsync(userId: 1, date: day);

        // Assert
        var reloadedUser = await db.ApplicationUsers.FirstAsync(u => u.Id == 1);
        Assert.Equal(0, reloadedUser.CurrentPoints);
        Assert.Null(reloadedUser.LastNeutralDate);
        Assert.Equal(0, reloadedUser.ContinuousNeutralDays);
        Assert.Empty(await db.PointAwardLogs.ToListAsync());
    }

    [Fact]
    public async Task CalculateDailyNetValueAsync_ShouldReturnExpectedNetValue()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var day = DateTime.UtcNow.Date;

        db.FoodRecords.Add(new FoodRecord
        {
            UserId = 1,
            Name = "Food",
            Amount = 1,
            EmissionFactor = 5m,
            Emission = 5m
        });
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 3m
        });
        db.StepRecords.Add(new StepRecord
        {
            UserId = 1,
            StepCount = 10_000,
            RecordDate = day.Date,
            CarbonOffset = 0 // not used in calculation
        });

        await db.SaveChangesAsync();
        var service = new PointService(db);

        // Act
        var net = await service.CalculateDailyNetValueAsync(userId: 1, date: day);

        // Assert
        // DailyBenchmark(15) - emissions(5+3) + steps(10000 * 0.0001) = 7 + 1 = 8
        Assert.Equal(8m, net);
    }

    [Fact]
    public async Task CheckAndAwardPointsAsync_ShouldGiveWeeklyBonus_OnSeventhConsecutiveDay()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var day = DateTime.UtcNow.Date;

        var user = new ApplicationUser
        {
            Id = 1,
            Username = "streak-user",
            Email = "test@example.com",
            PasswordHash = "hash",
            Region = "SG",
            BirthDate = DateTime.UtcNow.Date,
            CurrentPoints = 60,              // assume already got 6 * 10 points before
            ContinuousNeutralDays = 6,
            LastNeutralDate = day.AddDays(-1)
        };
        db.ApplicationUsers.Add(user);

        db.FoodRecords.Add(new FoodRecord
        {
            UserId = 1,
            Name = "LowCarbonFood",
            Amount = 1,
            EmissionFactor = 5m,
            Emission = 5m
        });
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 5m
        });

        await db.SaveChangesAsync();
        var service = new PointService(db);

        // Act
        await service.CheckAndAwardPointsAsync(1, day);

        // Assert
        var reloadedUser = await db.ApplicationUsers.FirstAsync(u => u.Id == 1);
        // 60 + 10 (base) + 50 (weekly bonus) = 120
        Assert.Equal(120, reloadedUser.CurrentPoints);
        Assert.Equal(7, reloadedUser.ContinuousNeutralDays);
        Assert.Equal(day.Date, reloadedUser.LastNeutralDate);

        var logs = await db.PointAwardLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Source == "DailyNeutral" && l.Points == 10);
        Assert.Contains(logs, l => l.Source == "WeeklyBonus" && l.Points == 50);
    }

    [Fact]
    public async Task CheckAndAwardPointsAsync_ShouldNotAwardTwice_ForSameDay()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var day = DateTime.UtcNow.Date;

        var user = new ApplicationUser
        {
            Id = 1,
            Username = "same-day-user",
            Email = "test@example.com",
            PasswordHash = "hash",
            Region = "SG",
            BirthDate = DateTime.UtcNow.Date,
            CurrentPoints = 10,
            ContinuousNeutralDays = 1,
            LastNeutralDate = day
        };
        db.ApplicationUsers.Add(user);

        db.FoodRecords.Add(new FoodRecord
        {
            UserId = 1,
            Name = "Food",
            Amount = 1,
            EmissionFactor = 5m,
            Emission = 5m
        });
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 5m
        });

        await db.SaveChangesAsync();
        var service = new PointService(db);

        // Act
        await service.CheckAndAwardPointsAsync(1, day);

        // Assert
        var reloadedUser = await db.ApplicationUsers.FirstAsync(u => u.Id == 1);
        Assert.Equal(10, reloadedUser.CurrentPoints); // unchanged
        Assert.Equal(1, reloadedUser.ContinuousNeutralDays);
        Assert.Equal(day.Date, reloadedUser.LastNeutralDate);
        Assert.Empty(await db.PointAwardLogs.ToListAsync());
    }

    [Fact]
    public async Task CalculateDailyNetValueAsync_ShouldReturnZero_WhenNoSteps()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var day = DateTime.UtcNow.Date;

        db.FoodRecords.Add(new FoodRecord
        {
            UserId = 1,
            Name = "Food",
            Amount = 1,
            EmissionFactor = 5m,
            Emission = 5m
        });
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationLatitude = 0,
            DestinationLongitude = 0,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 3m
        });

        await db.SaveChangesAsync();
        var service = new PointService(db);

        // Act
        var net = await service.CalculateDailyNetValueAsync(1, day);

        // Assert
        Assert.Equal(0m, net);
    }

    [Fact]
    public async Task LogPointAwardAsync_ShouldAddLog_WhenPointsPositive()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var service = new PointService(db);
        var day = DateTime.UtcNow.Date;

        // Act
        await service.LogPointAwardAsync(userId: 1, points: 15, awardedAt: day, source: "Manual");

        // Assert
        var log = await db.PointAwardLogs.SingleAsync();
        Assert.Equal(1, log.UserId);
        Assert.Equal(15, log.Points);
        Assert.Equal(day, log.AwardedAt);
        Assert.Equal("Manual", log.Source);
    }

    [Fact]
    public async Task LogPointAwardAsync_ShouldNotAddLog_WhenPointsNonPositive()
    {
        // Arrange
        await using var db = CreateInMemoryDb();
        var service = new PointService(db);

        // Act
        await service.LogPointAwardAsync(userId: 1, points: 0, awardedAt: DateTime.UtcNow, source: "Manual");

        // Assert
        Assert.Empty(await db.PointAwardLogs.ToListAsync());
    }
}

