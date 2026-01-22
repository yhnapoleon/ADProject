using EcoLens.Api.Services;

namespace EcoLens.Api.Services.Caching;

/// <summary>
/// 地址转坐标缓存服务接口
/// </summary>
public interface IGeocodingCacheService
{
	/// <summary>
	/// 从缓存获取地址的坐标
	/// </summary>
	/// <param name="address">地址</param>
	/// <returns>坐标信息，如果缓存不存在则返回 null</returns>
	Task<GeocodingResult?> GetCachedGeocodeAsync(string address);

	/// <summary>
	/// 将地址的坐标保存到缓存
	/// </summary>
	/// <param name="address">地址</param>
	/// <param name="result">坐标信息</param>
	Task SetCachedGeocodeAsync(string address, GeocodingResult result);
}
