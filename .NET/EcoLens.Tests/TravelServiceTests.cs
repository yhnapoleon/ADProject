using System;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Travel;
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
}

