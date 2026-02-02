using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcoLens.Api.Services;

/// <summary>
/// Google Maps API 服务实现
/// </summary>
public class GoogleMapsService : IGoogleMapsService
{
	private readonly HttpClient _httpClient;
	private readonly string _apiKey;
	private readonly ILogger<GoogleMapsService> _logger;

	public GoogleMapsService(
		HttpClient httpClient,
		IConfiguration configuration,
		ILogger<GoogleMapsService> logger)
	{
		_httpClient = httpClient;
		_apiKey = configuration["GoogleMaps:ApiKey"] ?? throw new InvalidOperationException("GoogleMaps:ApiKey is not configured");
		_logger = logger;
	}

	/// <summary>
	/// 地理编码：将地址转换为经纬度坐标
	/// </summary>
	public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken ct = default)
	{
		try
		{
			// 支持新加坡邮编直接输入（6位数字，如160149）
			var normalizedAddress = NormalizeSingaporePostalCode(address);
			
			var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(normalizedAddress)}&key={_apiKey}";
			var response = await _httpClient.GetFromJsonAsync<GoogleGeocodingResponse>(url, ct);

			if (response?.Status != "OK" || response.Results == null || response.Results.Count == 0)
			{
				_logger.LogWarning("Geocoding failed: {Status}, Address: {Address}", response?.Status, address);
				return null;
			}

			var result = response.Results[0];
			var location = result.Geometry?.Location;

			if (location == null)
				return null;

			// 提取地址组件（国家、城市等）
			var addressComponents = result.AddressComponents ?? new List<AddressComponent>();
			var country = addressComponents.FirstOrDefault(c => c.Types?.Contains("country") == true)?.LongName;
			var city = addressComponents.FirstOrDefault(c => c.Types?.Contains("locality") == true || c.Types?.Contains("administrative_area_level_1") == true)?.LongName;

			return new GeocodingResult
			{
				Latitude = location.Lat,
				Longitude = location.Lng,
				FormattedAddress = result.FormattedAddress ?? address,
				Country = country,
				City = city
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Geocoding error: {Address}", address);
			return null;
		}
	}

	/// <summary>
	/// 反向地理编码：将经纬度坐标转换为地址
	/// </summary>
	public async Task<ReverseGeocodingResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
	{
		try
		{
			var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";
			var response = await _httpClient.GetFromJsonAsync<GoogleGeocodingResponse>(url, ct);

			if (response?.Status != "OK" || response.Results == null || response.Results.Count == 0)
			{
				_logger.LogWarning("Reverse geocoding failed: {Status}, Coordinates: {Lat}, {Lng}", response?.Status, latitude, longitude);
				return null;
			}

			var result = response.Results[0];
			var addressComponents = result.AddressComponents ?? new List<AddressComponent>();

			return new ReverseGeocodingResult
			{
				FormattedAddress = result.FormattedAddress ?? string.Empty,
				City = addressComponents.FirstOrDefault(c => c.Types?.Contains("locality") == true)?.LongName,
				Country = addressComponents.FirstOrDefault(c => c.Types?.Contains("country") == true)?.LongName
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Reverse geocoding error: {Lat}, {Lng}", latitude, longitude);
			return null;
		}
	}

	/// <summary>
	/// 计算两点间的距离（米）和行驶时间
	/// </summary>
	public async Task<DistanceResult?> CalculateDistanceAsync(
		double originLat, double originLng,
		double destLat, double destLng,
		CancellationToken ct = default)
	{
		try
		{
			var url = $"https://maps.googleapis.com/maps/api/distancematrix/json" +
				$"?origins={originLat},{originLng}" +
				$"&destinations={destLat},{destLng}" +
				$"&key={_apiKey}";

			var response = await _httpClient.GetFromJsonAsync<GoogleDistanceMatrixResponse>(url, ct);

			if (response?.Status != "OK" || 
			    response.Rows == null || 
			    response.Rows.Count == 0 ||
			    response.Rows[0].Elements == null ||
			    response.Rows[0].Elements.Count == 0)
			{
				_logger.LogWarning("Distance calculation failed: {Status}", response?.Status);
				return null;
			}

			var element = response.Rows[0].Elements[0];
			if (element.Status != "OK")
			{
				_logger.LogWarning("Distance calculation element status: {Status}", element.Status);
				return null;
			}

			return new DistanceResult
			{
				DistanceMeters = element.Distance?.Value ?? 0,
				DurationSeconds = element.Duration?.Value ?? 0,
				DistanceText = element.Distance?.Text,
				DurationText = element.Duration?.Text
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Distance calculation error");
			return null;
		}
	}

	/// <summary>
	/// 搜索附近的地点
	/// </summary>
	public async Task<PlacesSearchResult?> SearchNearbyAsync(
		double latitude, double longitude,
		string keyword,
		int radiusMeters = 5000,
		CancellationToken ct = default)
	{
		try
		{
			// 对于机场和港口，使用 type 参数更精确；对于其他关键词，使用 keyword 参数
			var isAirport = keyword.Equals("airport", StringComparison.OrdinalIgnoreCase);
			var isPort = keyword.Equals("port", StringComparison.OrdinalIgnoreCase) || 
			            keyword.Equals("ferry terminal", StringComparison.OrdinalIgnoreCase);
			
			var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json" +
				$"?location={latitude},{longitude}" +
				$"&radius={radiusMeters}";
			
			if (isAirport)
			{
				// 使用 type=airport 更精确地搜索机场
				url += $"&type=airport";
			}
			else if (isPort)
			{
				// 对于港口，使用 keyword 搜索（Google Places API 没有专门的 port type）
				url += $"&keyword={Uri.EscapeDataString(keyword)}";
			}
			else
			{
				url += $"&keyword={Uri.EscapeDataString(keyword)}";
			}
			
			url += $"&key={_apiKey}";

			var response = await _httpClient.GetFromJsonAsync<GooglePlacesResponse>(url, ct);

			if (response?.Status != "OK" || response.Results == null)
			{
				_logger.LogWarning("Places search failed: {Status}, Keyword: {Keyword}, Location: {Lat}, {Lng}", 
					response?.Status, keyword, latitude, longitude);
				return null;
			}

			var places = response.Results.Select(r => new PlaceItem
			{
				Name = r.Name ?? string.Empty,
				Address = r.Vicinity,
				Latitude = r.Geometry?.Location?.Lat ?? 0,
				Longitude = r.Geometry?.Location?.Lng ?? 0,
				Rating = r.Rating
			}).ToList();

			return new PlacesSearchResult { Places = places };
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Places search error");
			return null;
		}
	}

	/// <summary>
	/// 获取路线详情（包括polyline用于地图绘制）
	/// </summary>
	public async Task<RouteResult?> GetRouteAsync(
		double originLat, double originLng,
		double destLat, double destLng,
		string travelMode = "driving",
		CancellationToken ct = default)
	{
		try
		{
			var url = $"https://maps.googleapis.com/maps/api/directions/json" +
				$"?origin={originLat},{originLng}" +
				$"&destination={destLat},{destLng}" +
				$"&mode={travelMode}" +
				$"&key={_apiKey}";

			var response = await _httpClient.GetFromJsonAsync<GoogleDirectionsResponse>(url, ct);

			if (response?.Status != "OK" || 
			    response.Routes == null || 
			    response.Routes.Count == 0)
			{
				_logger.LogWarning("Route retrieval failed: {Status}", response?.Status);
				return null;
			}

			var route = response.Routes[0];
			var leg = route.Legs?.FirstOrDefault();

			if (leg == null)
				return null;

			return new RouteResult
			{
				DistanceMeters = leg.Distance?.Value ?? 0,
				DurationSeconds = leg.Duration?.Value ?? 0,
				DistanceText = leg.Distance?.Text,
				DurationText = leg.Duration?.Text,
				Polyline = route.OverviewPolyline?.Points
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Route retrieval error");
			return null;
		}
	}

	#region Google API 响应模型（内部类，用于JSON反序列化）

	private class GoogleGeocodingResponse
	{
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("results")]
		public List<GeocodingResultItem>? Results { get; set; }
	}

	private class GeocodingResultItem
	{
		[JsonPropertyName("formatted_address")]
		public string? FormattedAddress { get; set; }

		[JsonPropertyName("geometry")]
		public Geometry? Geometry { get; set; }

		[JsonPropertyName("address_components")]
		public List<AddressComponent>? AddressComponents { get; set; }
	}

	private class Geometry
	{
		[JsonPropertyName("location")]
		public Location? Location { get; set; }
	}

	private class Location
	{
		[JsonPropertyName("lat")]
		public double Lat { get; set; }

		[JsonPropertyName("lng")]
		public double Lng { get; set; }
	}

	private class AddressComponent
	{
		[JsonPropertyName("long_name")]
		public string? LongName { get; set; }

		[JsonPropertyName("types")]
		public List<string>? Types { get; set; }
	}

	private class GoogleDistanceMatrixResponse
	{
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("rows")]
		public List<DistanceRow>? Rows { get; set; }
	}

	private class DistanceRow
	{
		[JsonPropertyName("elements")]
		public List<DistanceElement>? Elements { get; set; }
	}

	private class DistanceElement
	{
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("distance")]
		public DistanceValue? Distance { get; set; }

		[JsonPropertyName("duration")]
		public DurationValue? Duration { get; set; }
	}

	private class DistanceValue
	{
		[JsonPropertyName("value")]
		public int Value { get; set; }

		[JsonPropertyName("text")]
		public string? Text { get; set; }
	}

	private class DurationValue
	{
		[JsonPropertyName("value")]
		public int Value { get; set; }

		[JsonPropertyName("text")]
		public string? Text { get; set; }
	}

	private class GooglePlacesResponse
	{
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("results")]
		public List<PlaceResult>? Results { get; set; }
	}

	private class PlaceResult
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("vicinity")]
		public string? Vicinity { get; set; }

		[JsonPropertyName("geometry")]
		public Geometry? Geometry { get; set; }

		[JsonPropertyName("rating")]
		public double? Rating { get; set; }
	}

	private class GoogleDirectionsResponse
	{
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("routes")]
		public List<RouteItem>? Routes { get; set; }
	}

	private class RouteItem
	{
		[JsonPropertyName("legs")]
		public List<LegItem>? Legs { get; set; }

		[JsonPropertyName("overview_polyline")]
		public Polyline? OverviewPolyline { get; set; }
	}

	private class LegItem
	{
		[JsonPropertyName("distance")]
		public DistanceValue? Distance { get; set; }

		[JsonPropertyName("duration")]
		public DurationValue? Duration { get; set; }
	}

	private class Polyline
	{
		[JsonPropertyName("points")]
		public string? Points { get; set; }
	}

	#endregion

	/// <summary>
	/// 标准化新加坡邮编输入
	/// 如果输入是6位数字，自动添加"Singapore"后缀以提高识别率
	/// </summary>
	private string NormalizeSingaporePostalCode(string address)
	{
		if (string.IsNullOrWhiteSpace(address))
			return address;

		// 检查是否为6位数字（新加坡邮编格式）
		var trimmedAddress = address.Trim();
		if (System.Text.RegularExpressions.Regex.IsMatch(trimmedAddress, @"^\d{6}$"))
		{
			_logger.LogDebug("Detected Singapore postal code: {PostalCode}, adding 'Singapore' suffix", trimmedAddress);
			return $"{trimmedAddress} Singapore";
		}

		return address;
	}
}
