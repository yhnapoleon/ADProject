using EcoLens.Api.Services;
using EcoLens.Api.Services.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcoLens.Tests.Services;

public class GeocodingCacheServiceTests
{
	private static GeocodingCacheService CreateService(IMemoryCache cache)
	{
		var logger = new Mock<ILogger<GeocodingCacheService>>();
		return new GeocodingCacheService(cache, logger.Object);
	}

	[Fact]
	public async Task GetCachedGeocodeAsync_ReturnsNull_WhenNotCached()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var service = CreateService(cache);
		var result = await service.GetCachedGeocodeAsync("Some Address");
		Assert.Null(result);
	}

	[Fact]
	public async Task SetCachedGeocodeAsync_ThenGetCachedGeocodeAsync_ReturnsCachedResult()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var service = CreateService(cache);
		var geocode = new GeocodingResult
		{
			Latitude = 1.35,
			Longitude = 103.8,
			FormattedAddress = "Singapore",
			Country = "Singapore",
			City = "Singapore"
		};

		await service.SetCachedGeocodeAsync("Singapore", geocode);
		var result = await service.GetCachedGeocodeAsync("Singapore");

		Assert.NotNull(result);
		Assert.Equal(1.35, result!.Latitude);
		Assert.Equal(103.8, result.Longitude);
		Assert.Equal("Singapore", result.FormattedAddress);
	}

	[Fact]
	public async Task GetCachedGeocodeAsync_IsCaseInsensitive_AndTrimmed()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var service = CreateService(cache);
		var geocode = new GeocodingResult
		{
			Latitude = 1.0,
			Longitude = 2.0,
			FormattedAddress = "Test"
		};

		await service.SetCachedGeocodeAsync("  Singapore  ", geocode);
		// 内部 key 为 geocode:address.tolowerinvariant().trim()，所以 "  singapore  " -> "singapore"
		var result = await service.GetCachedGeocodeAsync("  SINGAPORE  ");
		Assert.NotNull(result);
		Assert.Equal(1.0, result!.Latitude);
	}

	[Fact]
	public async Task GetCachedGeocodeAsync_ReturnsNull_ForDifferentAddress()
	{
		var cache = new MemoryCache(new MemoryCacheOptions());
		var service = CreateService(cache);
		var geocode = new GeocodingResult
		{
			Latitude = 1.0,
			Longitude = 2.0,
			FormattedAddress = "Address A"
		};
		await service.SetCachedGeocodeAsync("Address A", geocode);

		var result = await service.GetCachedGeocodeAsync("Address B");
		Assert.Null(result);
	}
}
