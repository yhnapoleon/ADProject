using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcoLens.Api.Services;

/// <summary>
/// Google Maps API service implementation.
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

	/// <summary>
	/// Mask coordinates for logging to avoid exposure of precise location. Rounds to 2 decimal places (~1.1 km precision).
	/// </summary>
	private static string MaskCoordinateForLog(double coord) => Math.Round(coord, 2).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

	/// <summary>
	/// Geocode address to lat/lng.
	/// </summary>
	public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken ct = default)
	{
		try
		{
			// Normalize Singapore-only input (postal code, short names); leave other addresses as-is for global recognition
			var normalizedAddress = NormalizeAddressForSingapore(address);
			var isSingaporeTarget = normalizedAddress.Contains("Singapore", StringComparison.OrdinalIgnoreCase) ||
			                       normalizedAddress.Contains("新加坡", StringComparison.OrdinalIgnoreCase);

			var url = "https://maps.googleapis.com/maps/api/geocode/json?address=" + Uri.EscapeDataString(normalizedAddress);
			if (isSingaporeTarget)
				url += "&region=sg";
			url += "&key=" + _apiKey;
			var response = await _httpClient.GetFromJsonAsync<GoogleGeocodingResponse>(url, ct);

			if (response?.Status != "OK" || response.Results == null || response.Results.Count == 0)
			{
				_logger.LogWarning("Geocoding failed: {Status}, Address: {Address}", response?.Status, SanitizeForLog(address));
				return null;
			}

			var result = response.Results[0];
			var location = result.Geometry?.Location;

			if (location == null)
				return null;

			// Extract address components (country, city, etc.)
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
			_logger.LogError(ex, "Geocoding error: {Address}", SanitizeForLog(address));
			return null;
		}
	}

	/// <summary>Reverse geocode: convert lat/lng to address.</summary>
	public async Task<ReverseGeocodingResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
	{
		try
		{
			var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";
			var response = await _httpClient.GetFromJsonAsync<GoogleGeocodingResponse>(url, ct);

			if (response?.Status != "OK" || response.Results == null || response.Results.Count == 0)
			{
				_logger.LogWarning("Reverse geocoding failed: {Status}, Coordinates: {Lat}, {Lng}", response?.Status, MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude));
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
			_logger.LogError(ex, "Reverse geocoding error: {Lat}, {Lng}", MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude));
			return null;
		}
	}

	/// <summary>
	/// Calculate distance (meters) and duration between two points.
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
	/// Search nearby places.
	/// </summary>
	public async Task<PlacesSearchResult?> SearchNearbyAsync(
		double latitude, double longitude,
		string keyword,
		int radiusMeters = 5000,
		CancellationToken ct = default)
	{
		try
		{
			// Use type for airport/port when available; keyword for others
			var isAirport = keyword.Equals("airport", StringComparison.OrdinalIgnoreCase);
			var isPort = keyword.Equals("port", StringComparison.OrdinalIgnoreCase) || 
			            keyword.Equals("ferry terminal", StringComparison.OrdinalIgnoreCase);
			
			var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json" +
				$"?location={latitude},{longitude}" +
				$"&radius={radiusMeters}";
			
			if (isAirport)
			{
				// Use type=airport for airport search
				url += $"&type=airport";
			}
			else if (isPort)
			{
				// Port: use keyword (Google Places has no dedicated port type)
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
					response?.Status, keyword, MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude));
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
	/// Get route details (including polyline for map).
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
				_logger.LogWarning("Route retrieval failed: Status={Status}, Origin=({OriginLat},{OriginLng}), Dest=({DestLat},{DestLng}), Mode={Mode}",
					response?.Status, originLat, originLng, destLat, destLng, travelMode);
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

	#region Google API response DTOs (for JSON deserialization)

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

	/// <summary>Normalize Singapore-only input: 6-digit postal code or known short names get "Singapore" suffix; other addresses unchanged.</summary>
	private string NormalizeAddressForSingapore(string address)
	{
		if (string.IsNullOrWhiteSpace(address))
			return address;

		var trimmed = address.Trim();

		// Already contains Singapore: do not modify
		if (trimmed.Contains("Singapore", StringComparison.OrdinalIgnoreCase) ||
		    trimmed.Contains("新加坡", StringComparison.OrdinalIgnoreCase))
			return trimmed;

		// Singapore postal code: 6 digits
		if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{6}$"))
		{
			_logger.LogDebug("Detected Singapore postal code: {PostalCode}, adding 'Singapore' suffix", SanitizeForLog(trimmed));
			return $"{trimmed} Singapore";
		}

		// Known Singapore short names only (avoid false match for other cities)
		var singaporeShortNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"NUS", "NTU", "SMU", "SUTD", "SIT", "SUSS", "SIM",
			"Orchard", "Marina Bay", "Changi", "Sentosa", "Jurong", "Woodlands",
			"CBD", "Raffles", "Bugis", "Tampines", "Bishan", "Ang Mo Kio"
		};
		if (singaporeShortNames.Contains(trimmed))
		{
			_logger.LogDebug("Singapore short name detected: {Address}, adding 'Singapore'", SanitizeForLog(trimmed));
			return $"{trimmed} Singapore";
		}

		return trimmed;
	}
}
