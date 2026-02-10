using System;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Services;
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
}

