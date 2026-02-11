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
using Moq;

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

        // 仅统计用户1（不指定日期范围，包含该用户全部记录）
        var stats = await svc.GetUserTravelStatisticsAsync(1, ct: CancellationToken.None);

        Assert.Equal(3, stats.TotalRecords);               // 用户1共有 3 条记录
        Assert.Equal(17, stats.TotalDistanceKilometers);   // 10 + 5 + 2
        Assert.Equal(8.1m, stats.TotalCarbonEmission);     // 5 + 3 + 0.1

        Assert.NotNull(stats.ByTransportMode);
        Assert.Equal(2, stats.ByTransportMode.Count);      // Bus + Walking

        var busStats = stats.ByTransportMode.Single(s => s.TransportMode == TransportMode.Bus);
        Assert.Equal(2, busStats.RecordCount);
        Assert.Equal(15, busStats.TotalDistanceKilometers);
        Assert.Equal(8, busStats.TotalCarbonEmission);
    }

    [Fact]
    public async Task ValidateTransportMode_ShouldThrow_ForPlane_BetweenMajorCities_WhenTooShort()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var origin = new GeocodingResult { City = "Tokyo", Country = "Japan", FormattedAddress = "Tokyo, Japan" };
        var dest = new GeocodingResult { City = "Tokyo", Country = "Japan", FormattedAddress = "Tokyo, Japan" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeValidateTransportModeAsync(svc, TransportMode.Plane, origin, dest, distanceKm: 150));
    }

    [Fact]
    public async Task ValidateTransportMode_ShouldThrow_ForPlane_GeneralLevel_WhenTooShort()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var origin = new GeocodingResult { City = "SmallTownA", Country = "CountryX", FormattedAddress = "SmallTownA, CountryX" };
        var dest = new GeocodingResult { City = "SmallTownB", Country = "CountryX", FormattedAddress = "SmallTownB, CountryX" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeValidateTransportModeAsync(svc, TransportMode.Plane, origin, dest, distanceKm: 50));
    }

    [Fact]
    public async Task ValidateTransportMode_ShouldThrow_ForSubway_BetweenCountries()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(
            db,
            new DummyGoogleMapsService(),
            new DummyGeocodingCacheService(),
            _nullLogger);

        var origin = new GeocodingResult { City = "London", Country = "United Kingdom", FormattedAddress = "London, UK" };
        var dest = new GeocodingResult { City = "Paris", Country = "France", FormattedAddress = "Paris, France" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeValidateTransportModeAsync(svc, TransportMode.Subway, origin, dest, distanceKm: 300));
    }

    [Fact]
    public async Task CreateTravelLogAsync_ShouldThrow_WhenOriginGeocodeFails()
    {
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodingResult?)null);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto
        {
            OriginAddress = "Unknown Place",
            DestinationAddress = "B",
            TransportMode = TransportMode.Bus
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTravelLogAsync(1, dto, CancellationToken.None));
        Assert.Contains("Unable to geocode origin address", ex.Message);
    }

    [Fact]
    public async Task CreateTravelLogAsync_ShouldThrow_WhenDestinationGeocodeFails()
    {
        var origin = new GeocodingResult { Latitude = 1.3, Longitude = 103.8, FormattedAddress = "Singapore A", Country = "Singapore" };
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(origin);
        mockMaps.Setup(x => x.GeocodeAsync("Bad Dest", It.IsAny<CancellationToken>())).ReturnsAsync((GeocodingResult?)null);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto
        {
            OriginAddress = "A",
            DestinationAddress = "Bad Dest",
            TransportMode = TransportMode.Bus
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTravelLogAsync(1, dto, CancellationToken.None));
        Assert.Contains("Unable to geocode destination address", ex.Message);
    }

    [Fact]
    public async Task CreateTravelLogAsync_ShouldThrow_WhenRouteNull_ForBus()
    {
        var origin = new GeocodingResult { Latitude = 1.3, Longitude = 103.8, FormattedAddress = "Singapore", Country = "Singapore" };
        var dest = new GeocodingResult { Latitude = 1.35, Longitude = 103.9, FormattedAddress = "Singapore B", Country = "Singapore" };
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string addr, CancellationToken _) => addr == "A" ? origin : dest);
        mockMaps.Setup(x => x.GetRouteAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RouteResult?)null);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto { OriginAddress = "A", DestinationAddress = "B", TransportMode = TransportMode.Bus };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTravelLogAsync(1, dto, CancellationToken.None));
        Assert.Contains("No route found", ex.Message);
    }

    [Fact]
    public async Task CreateTravelLogAsync_ShouldSucceed_ForWalking_WithMockedGeocodeAndRoute()
    {
        var origin = new GeocodingResult { Latitude = 1.3, Longitude = 103.8, FormattedAddress = "Start", Country = "Singapore" };
        var dest = new GeocodingResult { Latitude = 1.301, Longitude = 103.801, FormattedAddress = "End", Country = "Singapore" };
        var route = new RouteResult { DistanceMeters = 500, DurationSeconds = 300, DistanceText = "0.5 km", DurationText = "5 mins" };
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync("Start", It.IsAny<CancellationToken>())).ReturnsAsync(origin);
        mockMaps.Setup(x => x.GeocodeAsync("End", It.IsAny<CancellationToken>())).ReturnsAsync(dest);
        mockMaps.Setup(x => x.GetRouteAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto { OriginAddress = "Start", DestinationAddress = "End", TransportMode = TransportMode.Walking };

        var result = await svc.CreateTravelLogAsync(1, dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(500, result.DistanceMeters);
        Assert.Equal(TransportMode.Walking, result.TransportMode);
    }

    [Fact]
    public async Task CreateTravelLogAsync_ShouldSucceed_ForBus_WhenCarbonFactorExists()
    {
        var origin = new GeocodingResult { Latitude = 1.3, Longitude = 103.8, FormattedAddress = "A", Country = "Singapore" };
        var dest = new GeocodingResult { Latitude = 1.35, Longitude = 103.9, FormattedAddress = "B", Country = "Singapore" };
        var route = new RouteResult { DistanceMeters = 10_000, DurationSeconds = 600, DistanceText = "10 km" };
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(origin);
        mockMaps.Setup(x => x.GeocodeAsync("B", It.IsAny<CancellationToken>())).ReturnsAsync(dest);
        mockMaps.Setup(x => x.GetRouteAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        db.CarbonReferences.Add(new CarbonReference
        {
            LabelName = "Bus",
            Category = CarbonCategory.Transport,
            Co2Factor = 0.1m,
            Unit = "kg/km",
            Source = "Test"
        });
        await db.SaveChangesAsync();

        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto { OriginAddress = "A", DestinationAddress = "B", TransportMode = TransportMode.Bus };

        var result = await svc.CreateTravelLogAsync(1, dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10_000, result.DistanceMeters);
        Assert.Equal(TransportMode.Bus, result.TransportMode);
        Assert.True(result.CarbonEmission > 0);
    }

    [Fact]
    public async Task PreviewRouteAsync_ShouldThrow_WhenOriginGeocodeFails()
    {
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((GeocodingResult?)null);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto { OriginAddress = "X", DestinationAddress = "Y", TransportMode = TransportMode.Walking };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.PreviewRouteAsync(dto, CancellationToken.None));
        Assert.Contains("Unable to geocode origin address", ex.Message);
    }

    [Fact]
    public async Task PreviewRouteAsync_ShouldReturnPreview_ForWalking()
    {
        var origin = new GeocodingResult { Latitude = 1.3, Longitude = 103.8, FormattedAddress = "Start", Country = "Singapore" };
        var dest = new GeocodingResult { Latitude = 1.301, Longitude = 103.801, FormattedAddress = "End", Country = "Singapore" };
        var route = new RouteResult { DistanceMeters = 300, DurationSeconds = 240 };
        var mockMaps = new Mock<IGoogleMapsService>();
        mockMaps.Setup(x => x.GeocodeAsync("Start", It.IsAny<CancellationToken>())).ReturnsAsync(origin);
        mockMaps.Setup(x => x.GeocodeAsync("End", It.IsAny<CancellationToken>())).ReturnsAsync(dest);
        mockMaps.Setup(x => x.GetRouteAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(route);
        var mockCache = new Mock<IGeocodingCacheService>();
        mockCache.Setup(x => x.GetCachedGeocodeAsync(It.IsAny<string>())).ReturnsAsync((GeocodingResult?)null);

        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, mockMaps.Object, mockCache.Object, _nullLogger);
        var dto = new CreateTravelLogDto { OriginAddress = "Start", DestinationAddress = "End", TransportMode = TransportMode.Walking };

        var result = await svc.PreviewRouteAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(300, result.DistanceMeters);
        Assert.Equal(TransportMode.Walking, result.TransportMode);
    }

    [Fact]
    public async Task GetTravelLogByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, new DummyGoogleMapsService(), new DummyGeocodingCacheService(), _nullLogger);

        var result = await svc.GetTravelLogByIdAsync(999, 1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTravelLogByIdAsync_ShouldReturnNull_WhenUserMismatch()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, new DummyGoogleMapsService(), new DummyGeocodingCacheService(), _nullLogger);
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 2,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 1,
            OriginLongitude = 103,
            DestinationLatitude = 1,
            DestinationLongitude = 103,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 0.5m,
            PassengerCount = 1
        });
        await db.SaveChangesAsync();

        var id = db.TravelLogs.Local.First().Id;
        var result = await svc.GetTravelLogByIdAsync(id, 1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTravelLogByIdAsync_ShouldReturnDto_WhenFound()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, new DummyGoogleMapsService(), new DummyGeocodingCacheService(), _nullLogger);
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 1,
            OriginLongitude = 103,
            DestinationLatitude = 1,
            DestinationLongitude = 103,
            DistanceMeters = 5000,
            DistanceKilometers = 5,
            CarbonEmission = 2m,
            PassengerCount = 1
        });
        await db.SaveChangesAsync();
        var id = db.TravelLogs.Local.First().Id;

        var result = await svc.GetTravelLogByIdAsync(id, 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(5, result.DistanceKilometers);
        Assert.Equal(2m, result.CarbonEmission);
    }

    [Fact]
    public async Task DeleteTravelLogAsync_ShouldReturnFalse_WhenNotFound()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, new DummyGoogleMapsService(), new DummyGeocodingCacheService(), _nullLogger);

        var result = await svc.DeleteTravelLogAsync(999, 1, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteTravelLogAsync_ShouldReturnTrue_AndRemoveLog_WhenFound()
    {
        await using var db = CreateInMemoryDb();
        db.TravelLogs.Add(new TravelLog
        {
            UserId = 1,
            TransportMode = TransportMode.Bus,
            OriginAddress = "A",
            DestinationAddress = "B",
            OriginLatitude = 1,
            OriginLongitude = 103,
            DestinationLatitude = 1,
            DestinationLongitude = 103,
            DistanceMeters = 1000,
            DistanceKilometers = 1,
            CarbonEmission = 0.5m,
            PassengerCount = 1
        });
        await db.SaveChangesAsync();
        var id = db.TravelLogs.Local.First().Id;
        var svc = new TravelService(db, new DummyGoogleMapsService(), new DummyGeocodingCacheService(), _nullLogger);

        var result = await svc.DeleteTravelLogAsync(id, 1, CancellationToken.None);

        Assert.True(result);
        var deleted = await db.TravelLogs.FindAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetUserTravelLogsAsync_ShouldReturnEmptyPage_WhenNoLogs()
    {
        await using var db = CreateInMemoryDb();
        var svc = new TravelService(db, new DummyGoogleMapsService(), new DummyGeocodingCacheService(), _nullLogger);

        var result = await svc.GetUserTravelLogsAsync(1, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}

