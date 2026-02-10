using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services;
using EcoLens.Api.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcoLens.Tests;

public class TravelServiceTests
{
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private class DummyGoogleMapsService : IGoogleMapsService
    {
        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken ct = default) =>
            Task.FromResult<GeocodingResult?>(null);

        public Task<ReverseGeocodingResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default) =>
            Task.FromResult<ReverseGeocodingResult?>(null);

        public Task<RouteResult?> GetRouteAsync(double originLat, double originLng, double destLat, double destLng, string travelMode = "driving", CancellationToken ct = default) =>
            Task.FromResult<RouteResult?>(null);

        public Task<DistanceResult?> GetDistanceAsync(double originLat, double originLng, double destLat, double destLng, string travelMode = "driving", CancellationToken ct = default) =>
            Task.FromResult<DistanceResult?>(null);

        public Task<PlacesSearchResult?> SearchPlacesAsync(string query, double? latitude = null, double? longitude = null, CancellationToken ct = default) =>
            Task.FromResult<PlacesSearchResult?>(null);

        public Task<DistanceResult?> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng, CancellationToken ct = default) =>
            Task.FromResult<DistanceResult?>(null);

        public Task<PlacesSearchResult?> SearchNearbyAsync(double latitude, double longitude, string query, int radiusMeters = 1000, CancellationToken ct = default) =>
            Task.FromResult<PlacesSearchResult?>(null);
    }

    private class DummyGeocodingCacheService : IGeocodingCacheService
    {
        public Task<GeocodingResult?> GetCachedGeocodeAsync(string address) =>
            Task.FromResult<GeocodingResult?>(null);

        public Task SetCachedGeocodeAsync(string address, GeocodingResult result) =>
            Task.CompletedTask;
    }

    private readonly ILogger<TravelService> _nullLogger =
        new LoggerFactory().CreateLogger<TravelService>();

    private static async Task InvokeValidateTransportModeAsync(
        TravelService svc,
        TransportMode mode,
        GeocodingResult origin,
        GeocodingResult dest,
        double distanceKm)
    {
        var method = typeof(TravelService).GetMethod(
            "ValidateTransportModeForRouteAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
            throw new InvalidOperationException("ValidateTransportModeForRouteAsync not found via reflection.");

        try
        {
            var task = (Task)method.Invoke(svc, new object[]
            {
                mode,
                origin,
                dest,
                distanceKm,
                CancellationToken.None
            })!;

            await task.ConfigureAwait(false);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // 解包内部异常以便 Assert.ThrowsAsync 能捕获到真实类型
            throw ex.InnerException;
        }
    }

    [Fact]
    public async Task CreateTravelLogAsync_ShouldThrow_WhenModeIsTaxi()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var dto = new CreateTravelLogDto
        {
            OriginAddress = "A",
            DestinationAddress = "B",
            TransportMode = TransportMode.Taxi
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTravelLogAsync(userId: 1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PreviewRouteAsync_ShouldThrow_WhenModeIsTaxi()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var dto = new CreateTravelLogDto
        {
            OriginAddress = "A",
            DestinationAddress = "B",
            TransportMode = TransportMode.Taxi
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PreviewRouteAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateTransportMode_ShouldThrow_ForPlane_WhenDistanceTooShort()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var origin = new GeocodingResult { Latitude = 1.0, Longitude = 103.0, FormattedAddress = "Singapore", Country = "Singapore" };
        var dest = new GeocodingResult { Latitude = 1.0001, Longitude = 103.0001, FormattedAddress = "Singapore", Country = "Singapore" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeValidateTransportModeAsync(svc, TransportMode.Plane, origin, dest, distanceKm: 0.5));
    }

    [Fact]
    public async Task ValidateTransportMode_ShouldThrow_ForPlane_WithinSingapore()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var origin = new GeocodingResult { Latitude = 1.3, Longitude = 103.8, FormattedAddress = "Singapore", Country = "Singapore" };
        var dest = new GeocodingResult { Latitude = 1.35, Longitude = 103.9, FormattedAddress = "Singapore", Country = "Singapore" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeValidateTransportModeAsync(svc, TransportMode.Plane, origin, dest, distanceKm: 50));
    }

    [Fact]
    public async Task ValidateTransportMode_ShouldThrow_ForShip_WhenDistanceTooShort()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var origin = new GeocodingResult { Latitude = 0.0, Longitude = 0.0, FormattedAddress = "Harbor A", Country = "CountryX" };
        var dest = new GeocodingResult { Latitude = 0.01, Longitude = 0.01, FormattedAddress = "Harbor B", Country = "CountryX" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeValidateTransportModeAsync(svc, TransportMode.Ship, origin, dest, distanceKm: 5));
    }

    [Fact]
    public async Task GetUserTravelStatisticsAsync_ShouldAggregateTotals_AndGroupByMode()
    {
        await using var db = CreateInMemoryDb();

        var today = DateTime.UtcNow.Date;
        db.TravelLogs.AddRange(
            new TravelLog
            {
                UserId = 1,
                CreatedAt = today.AddDays(-1),
                TransportMode = TransportMode.Bus,
                DistanceKilometers = 10,
                CarbonEmission = 5
            },
            new TravelLog
            {
                UserId = 1,
                CreatedAt = today,
                TransportMode = TransportMode.Bus,
                DistanceKilometers = 5,
                CarbonEmission = 3
            },
            new TravelLog
            {
                UserId = 1,
                CreatedAt = today.AddDays(-10),
                TransportMode = TransportMode.Walking,
                DistanceKilometers = 2,
                CarbonEmission = 0.1m
            },
            new TravelLog
            {
                UserId = 2,
                CreatedAt = today,
                TransportMode = TransportMode.Bus,
                DistanceKilometers = 100,
                CarbonEmission = 50
            }
        );
        await db.SaveChangesAsync();

        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        // 仅统计用户1，日期范围最近7天
        var start = today.AddDays(-7);
        var stats = await svc.GetUserTravelStatisticsAsync(1, startDate: start, endDate: today, ct: CancellationToken.None);

        Assert.Equal(2, stats.TotalRecords);               // 只包含最近7天的两条 Bus 记录
        Assert.Equal(15, stats.TotalDistanceKilometers);   // 10 + 5
        Assert.Equal(8, stats.TotalCarbonEmission);        // 5 + 3

        Assert.NotNull(stats.ByTransportMode);
        Assert.Single(stats.ByTransportMode);
        var busStats = stats.ByTransportMode[0];
        Assert.Equal(TransportMode.Bus, busStats.TransportMode);
        Assert.Equal(2, busStats.RecordCount);
        Assert.Equal(15, busStats.TotalDistanceKilometers);
        Assert.Equal(8, busStats.TotalCarbonEmission);
    }
}

