namespace EcoLens.Api.Services;

/// <summary>
/// Google Maps API 服务接口
/// </summary>
public interface IGoogleMapsService
{
	/// <summary>
	/// 地理编码：将地址转换为经纬度坐标
	/// </summary>
	Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken ct = default);

	/// <summary>
	/// 反向地理编码：将经纬度坐标转换为地址
	/// </summary>
	Task<ReverseGeocodingResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

	/// <summary>
	/// 计算两点间的距离（米）
	/// </summary>
	Task<DistanceResult?> CalculateDistanceAsync(
		double originLat, double originLng,
		double destLat, double destLng,
		CancellationToken ct = default);

	/// <summary>
	/// 搜索附近的地点（例如：充电桩、回收站等）
	/// </summary>
	Task<PlacesSearchResult?> SearchNearbyAsync(
		double latitude, double longitude,
		string keyword,
		int radiusMeters = 5000,
		CancellationToken ct = default);

	/// <summary>
	/// 获取路线详情（包括polyline用于地图绘制）
	/// </summary>
	Task<RouteResult?> GetRouteAsync(
		double originLat, double originLng,
		double destLat, double destLng,
		string travelMode = "driving",
		CancellationToken ct = default);
}

/// <summary>
/// 地理编码结果
/// </summary>
public class GeocodingResult
{
	public double Latitude { get; set; }
	public double Longitude { get; set; }
	public string FormattedAddress { get; set; } = string.Empty;
}

/// <summary>
/// 反向地理编码结果
/// </summary>
public class ReverseGeocodingResult
{
	public string FormattedAddress { get; set; } = string.Empty;
	public string? City { get; set; }
	public string? Country { get; set; }
}

/// <summary>
/// 距离计算结果
/// </summary>
public class DistanceResult
{
	public int DistanceMeters { get; set; }
	public int DurationSeconds { get; set; }
	public string? DistanceText { get; set; }
	public string? DurationText { get; set; }
}

/// <summary>
/// 地点搜索结果
/// </summary>
public class PlacesSearchResult
{
	public List<PlaceItem> Places { get; set; } = new();
}

/// <summary>
/// 地点信息
/// </summary>
public class PlaceItem
{
	public string Name { get; set; } = string.Empty;
	public string? Address { get; set; }
	public double Latitude { get; set; }
	public double Longitude { get; set; }
	public double? Rating { get; set; }
}

/// <summary>
/// 路线结果（包含polyline用于地图绘制）
/// </summary>
public class RouteResult
{
	public int DistanceMeters { get; set; }
	public int DurationSeconds { get; set; }
	public string? DistanceText { get; set; }
	public string? DurationText { get; set; }
	public string? Polyline { get; set; }
}
