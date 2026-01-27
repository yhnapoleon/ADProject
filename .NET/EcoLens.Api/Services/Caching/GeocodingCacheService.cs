using EcoLens.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace EcoLens.Api.Services.Caching;

/// <summary>
/// 地址转坐标缓存服务实现
/// 使用内存缓存，减少 Google Maps API 调用次数
/// </summary>
public class GeocodingCacheService : IGeocodingCacheService
{
	private readonly IMemoryCache _cache;
	private readonly ILogger<GeocodingCacheService> _logger;
	private const int CacheExpirationMinutes = 60 * 24; // 24小时过期

	public GeocodingCacheService(IMemoryCache cache, ILogger<GeocodingCacheService> logger)
	{
		_cache = cache;
		_logger = logger;
	}

	public Task<GeocodingResult?> GetCachedGeocodeAsync(string address)
	{
		var cacheKey = GetCacheKey(address);
		if (_cache.TryGetValue(cacheKey, out GeocodingResult? cachedResult))
		{
			_logger.LogDebug("Getting geocode from cache: {Address}", address);
			return Task.FromResult<GeocodingResult?>(cachedResult);
		}

		return Task.FromResult<GeocodingResult?>(null);
	}

	public Task SetCachedGeocodeAsync(string address, GeocodingResult result)
	{
		var cacheKey = GetCacheKey(address);
		var cacheOptions = new MemoryCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
			SlidingExpiration = TimeSpan.FromHours(12) // 如果12小时内没有被访问，则过期
		};

		_cache.Set(cacheKey, result, cacheOptions);
		_logger.LogDebug("Saving geocode to cache: {Address}", address);

		return Task.CompletedTask;
	}

	private static string GetCacheKey(string address)
	{
		// 使用地址作为缓存键（转换为小写并去除空格，确保一致性）
		return $"geocode:{address.ToLowerInvariant().Trim()}";
	}
}
