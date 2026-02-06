using System.Text;
using EcoLens.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace EcoLens.Api.Services.Caching;

/// <summary>
/// In-memory geocoding cache to reduce Google Maps API calls.
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

	/// <summary>
	/// Sanitize user input before writing to logs to prevent log forging. Removes newlines and control characters.
	/// </summary>
	private static string SanitizeForLog(string? value)
	{
		if (string.IsNullOrEmpty(value)) return string.Empty;
		var sb = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			if (char.IsControl(c) && c != '\t')
				sb.Append(' ');
			else if (c == '\r' || c == '\n')
				sb.Append(' ');
			else
				sb.Append(c);
		}
		return sb.ToString().Trim();
	}

	public Task<GeocodingResult?> GetCachedGeocodeAsync(string address)
	{
		var cacheKey = GetCacheKey(address);
		if (_cache.TryGetValue(cacheKey, out GeocodingResult? cachedResult))
		{
			_logger.LogDebug("Getting geocode from cache: {Address}", SanitizeForLog(address));
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
			SlidingExpiration = TimeSpan.FromHours(12) // Sliding 12h
		};

		_cache.Set(cacheKey, result, cacheOptions);
		_logger.LogDebug("Saving geocode to cache: {Address}", SanitizeForLog(address));

		return Task.CompletedTask;
	}

	private static string GetCacheKey(string address)
	{
		// Cache key: normalized address
		return $"geocode:{address.ToLowerInvariant().Trim()}";
	}
}
