using System.Text;
using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

/// <summary>Travel log service implementation.</summary>
public class TravelService : ITravelService
{
	private readonly ApplicationDbContext _db;
	private readonly IGoogleMapsService _googleMapsService;
	private readonly IGeocodingCacheService _cacheService;
	private readonly ILogger<TravelService> _logger;

	public TravelService(
		ApplicationDbContext db,
		IGoogleMapsService googleMapsService,
		IGeocodingCacheService cacheService,
		ILogger<TravelService> logger)
	{
		_db = db;
		_googleMapsService = googleMapsService;
		_cacheService = cacheService;
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

	/// <summary>Create travel log.</summary>
	public async Task<TravelLogResponseDto> CreateTravelLogAsync(int userId, CreateTravelLogDto dto, CancellationToken ct = default)
	{
		if (dto.TransportMode == TransportMode.Taxi)
			throw new InvalidOperationException("Taxi was cancelled; please choose another transport mode.");

		// 1. Geocode (check cache first)
		var originGeocode = await _cacheService.GetCachedGeocodeAsync(dto.OriginAddress);
		if (originGeocode == null)
		{
			originGeocode = await _googleMapsService.GeocodeAsync(dto.OriginAddress, ct);
			if (originGeocode == null)
			{
				throw new InvalidOperationException($"Unable to geocode origin address: {dto.OriginAddress}");
			}
			// save to cache
			await _cacheService.SetCachedGeocodeAsync(dto.OriginAddress, originGeocode);
		}

		var destGeocode = await _cacheService.GetCachedGeocodeAsync(dto.DestinationAddress);
		if (destGeocode == null)
		{
			destGeocode = await _googleMapsService.GeocodeAsync(dto.DestinationAddress, ct);
			if (destGeocode == null)
			{
				throw new InvalidOperationException($"Unable to geocode destination address: {dto.DestinationAddress}");
			}
			// save to cache
			await _cacheService.SetCachedGeocodeAsync(dto.DestinationAddress, destGeocode);
		}

		// 1.5. PreFlightCheck: sanity check before map request (straight-line distance)
		var straightLineDistanceKm = CalculateHaversineDistance(
			originGeocode.Latitude, originGeocode.Longitude,
			destGeocode.Latitude, destGeocode.Longitude);
		await PreFlightCheckAsync(dto.TransportMode, originGeocode, destGeocode, straightLineDistanceKm, ct);

		// 2. Get route (navigation distance for validation and calculation)
		RouteResult? route = null;
		
		// Plane/Ship: use great-circle distance (Google Maps has no plane/ship route)
		if (dto.TransportMode == TransportMode.Plane || dto.TransportMode == TransportMode.Ship)
		{
			var greatCircleMeters = GetGreatCircleDistanceMeters(
				originGeocode.Latitude, originGeocode.Longitude,
				destGeocode.Latitude, destGeocode.Longitude);
			route = new RouteResult
			{
				DistanceMeters = greatCircleMeters,
				DurationSeconds = 0,
				DistanceText = $"{greatCircleMeters / 1000.0:F0} km",
				DurationText = null,
				Polyline = null
			};
			_logger.LogInformation("Using great-circle distance for {Mode}: {DistanceKm:F1} km",
				dto.TransportMode, greatCircleMeters / 1000.0);
		}
		else
		{
			// Other modes: get route via Google Maps API
			var travelMode = GetGoogleMapsTravelMode(dto.TransportMode);
			route = await _googleMapsService.GetRouteAsync(
				originGeocode.Latitude,
				originGeocode.Longitude,
				destGeocode.Latitude,
				destGeocode.Longitude,
				travelMode,
				ct);
		}

		if (route == null)
		{
			_logger.LogWarning("GetRouteAsync returned null for Origin={Origin}, Dest={Dest}, Mode={Mode}",
				SanitizeForLog(dto.OriginAddress), SanitizeForLog(dto.DestinationAddress), dto.TransportMode);
			throw new InvalidOperationException(
				"No route found for the selected transport mode between these locations. For international or long-distance travel (e.g. London to New York), please select Plane.");
		}

		// 2.5. Validate transport mode for route (using actual navigation distance)
		var navigationDistanceKm = route.DistanceMeters / 1000.0;
		await ValidateTransportModeForRouteAsync(dto.TransportMode, originGeocode, destGeocode, navigationDistanceKm, ct);

		// 3. Compute carbon emission (walking/bicycle use reduction factor, others use DB factor)
		var distanceKm = (decimal)navigationDistanceKm;
		decimal totalCarbonEmission;
		
		if (dto.TransportMode == TransportMode.Walking || dto.TransportMode == TransportMode.Bicycle)
		{
			var reductionFactor = dto.TransportMode == TransportMode.Walking ? -0.12m : -0.20m;
			totalCarbonEmission = distanceKm * reductionFactor;
		}
		else
		{
			var carbonFactor = await GetCarbonFactorAsync(dto.TransportMode, ct);
			if (carbonFactor == null)
			{
				throw new InvalidOperationException($"Carbon emission factor not found for transport mode: {dto.TransportMode}");
			}
			totalCarbonEmission = distanceKm * carbonFactor.Co2Factor;
		}

		// 4. Shared transport: allocate emission by passenger count
		var passengerCount = GetPassengerCount(dto.TransportMode);
		var carbonEmission = CalculatePerPersonCarbonEmission(dto.TransportMode, totalCarbonEmission, passengerCount);

		// 5. Create entity
		var travelLog = new TravelLog
		{
			UserId = userId,
			TransportMode = dto.TransportMode,
			OriginAddress = originGeocode.FormattedAddress,
			OriginLatitude = (decimal)originGeocode.Latitude,
			OriginLongitude = (decimal)originGeocode.Longitude,
			DestinationAddress = destGeocode.FormattedAddress,
			DestinationLatitude = (decimal)destGeocode.Latitude,
			DestinationLongitude = (decimal)destGeocode.Longitude,
			DistanceMeters = route.DistanceMeters,
			DistanceKilometers = distanceKm,
			DurationSeconds = route.DurationSeconds,
			CarbonEmission = carbonEmission,
			PassengerCount = passengerCount,
			RoutePolyline = route.Polyline,
			Notes = dto.Notes
		};

		// 6. Save to DB
		await _db.TravelLogs.AddAsync(travelLog, ct);
		await _db.SaveChangesAsync(ct);

		// 7. Update user total emission
		await UpdateUserTotalCarbonEmissionAsync(userId, ct);

		_logger.LogInformation("Travel log created successfully: UserId={UserId}, TravelLogId={TravelLogId}", userId, travelLog.Id);

		// 8. Map to DTO
		return ToResponseDto(travelLog);
	}

	/// <summary>Preview route and emission (no save).</summary>
	public async Task<RoutePreviewDto> PreviewRouteAsync(CreateTravelLogDto dto, CancellationToken ct = default)
	{
		if (dto.TransportMode == TransportMode.Taxi)
			throw new InvalidOperationException("Taxi was cancelled; please choose another transport mode.");

		// 1. Geocode (check cache first)
		var originGeocode = await _cacheService.GetCachedGeocodeAsync(dto.OriginAddress);
		if (originGeocode == null)
		{
			originGeocode = await _googleMapsService.GeocodeAsync(dto.OriginAddress, ct);
			if (originGeocode == null)
			{
				throw new InvalidOperationException($"Unable to geocode origin address: {dto.OriginAddress}");
			}
			await _cacheService.SetCachedGeocodeAsync(dto.OriginAddress, originGeocode);
		}

		var destGeocode = await _cacheService.GetCachedGeocodeAsync(dto.DestinationAddress);
		if (destGeocode == null)
		{
			destGeocode = await _googleMapsService.GeocodeAsync(dto.DestinationAddress, ct);
			if (destGeocode == null)
			{
				throw new InvalidOperationException($"Unable to geocode destination address: {dto.DestinationAddress}");
			}
			await _cacheService.SetCachedGeocodeAsync(dto.DestinationAddress, destGeocode);
		}

		// 1.5. PreFlightCheck: sanity check before map request (using straight-line distance)
		var straightLineDistanceKm = CalculateHaversineDistance(
			originGeocode.Latitude, originGeocode.Longitude,
			destGeocode.Latitude, destGeocode.Longitude);
		await PreFlightCheckAsync(dto.TransportMode, originGeocode, destGeocode, straightLineDistanceKm, ct);

		// 2. Get route (navigation distance for validation and calculation)
		RouteResult? route = null;
		
		// Plane/Ship: use great-circle distance (Google Maps has no plane/ship route)
		if (dto.TransportMode == TransportMode.Plane || dto.TransportMode == TransportMode.Ship)
		{
			var greatCircleMeters = GetGreatCircleDistanceMeters(
				originGeocode.Latitude, originGeocode.Longitude,
				destGeocode.Latitude, destGeocode.Longitude);
			route = new RouteResult
			{
				DistanceMeters = greatCircleMeters,
				DurationSeconds = 0,
				DistanceText = $"{greatCircleMeters / 1000.0:F0} km",
				DurationText = null,
				Polyline = null
			};
		}
		else
		{
			// Other modes: get route via Google Maps API
			var travelMode = GetGoogleMapsTravelMode(dto.TransportMode);
			route = await _googleMapsService.GetRouteAsync(
				originGeocode.Latitude,
				originGeocode.Longitude,
				destGeocode.Latitude,
				destGeocode.Longitude,
				travelMode,
				ct);
		}

		if (route == null)
		{
			_logger.LogWarning("GetRouteAsync returned null (preview) for Origin={Origin}, Dest={Dest}, Mode={Mode}",
				SanitizeForLog(dto.OriginAddress), SanitizeForLog(dto.DestinationAddress), dto.TransportMode);
			throw new InvalidOperationException(
				"No route found for the selected transport mode between these locations. For international or long-distance travel (e.g. London to New York), please select Plane.");
		}

		// 2.5. Validate transport mode for route (using actual navigation distance)
		var navigationDistanceKm = route.DistanceMeters / 1000.0;
		await ValidateTransportModeForRouteAsync(dto.TransportMode, originGeocode, destGeocode, navigationDistanceKm, ct);

		// 3. Compute carbon emission (walking/bicycle use reduction factor)
		var distanceKm = (decimal)navigationDistanceKm;
		decimal totalCarbonEmission;
		
		if (dto.TransportMode == TransportMode.Walking || dto.TransportMode == TransportMode.Bicycle)
		{
			// Walking/bicycle use reduction factor
			var reductionFactor = dto.TransportMode == TransportMode.Walking ? -0.12m : -0.20m;
			totalCarbonEmission = distanceKm * reductionFactor;
		}
		else
		{
			var carbonFactor = await GetCarbonFactorAsync(dto.TransportMode, ct);
			if (carbonFactor == null)
			{
				throw new InvalidOperationException($"Carbon emission factor not found for transport mode: {dto.TransportMode}");
			}
			totalCarbonEmission = distanceKm * carbonFactor.Co2Factor;
		}

		// 4. Shared transport: allocate emission by passenger count
		var passengerCount = GetPassengerCount(dto.TransportMode);
		var carbonEmission = CalculatePerPersonCarbonEmission(dto.TransportMode, totalCarbonEmission, passengerCount);

		// 5. Build preview DTO
		return new RoutePreviewDto
		{
			OriginAddress = originGeocode.FormattedAddress,
			OriginLatitude = (decimal)originGeocode.Latitude,
			OriginLongitude = (decimal)originGeocode.Longitude,
			DestinationAddress = destGeocode.FormattedAddress,
			DestinationLatitude = (decimal)destGeocode.Latitude,
			DestinationLongitude = (decimal)destGeocode.Longitude,
			TransportMode = dto.TransportMode,
			TransportModeName = GetTransportModeName(dto.TransportMode),
			DistanceMeters = route.DistanceMeters,
			DistanceKilometers = distanceKm,
			DurationSeconds = route.DurationSeconds,
			DurationText = route.DurationText,
			EstimatedCarbonEmission = carbonEmission,
			RoutePolyline = route.Polyline
		};
	}

	/// <summary>Get user travel logs with optional filter and paging.</summary>
	public async Task<PagedResultDto<TravelLogResponseDto>> GetUserTravelLogsAsync(
		int userId,
		GetTravelLogsQueryDto? query = null,
		CancellationToken ct = default)
	{
		query ??= new GetTravelLogsQueryDto();

		var baseQuery = _db.TravelLogs.Where(t => t.UserId == userId);

		// Date filter
		if (query.StartDate.HasValue)
		{
			baseQuery = baseQuery.Where(t => t.CreatedAt >= query.StartDate.Value);
		}

		if (query.EndDate.HasValue)
		{
			var endDateInclusive = query.EndDate.Value.Date.AddDays(1);
			baseQuery = baseQuery.Where(t => t.CreatedAt < endDateInclusive);
		}

		// Transport mode filter
		if (query.TransportMode.HasValue)
		{
			baseQuery = baseQuery.Where(t => t.TransportMode == query.TransportMode.Value);
		}

		var totalCount = await baseQuery.CountAsync(ct);

		// Paged query
		var travelLogs = await baseQuery
			.OrderByDescending(t => t.CreatedAt)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.ToListAsync(ct);

		var items = travelLogs.Select(ToResponseDto).ToList();

		return new PagedResultDto<TravelLogResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			Page = query.Page,
			PageSize = query.PageSize
		};
	}

	/// <summary>Get a single travel log by ID.</summary>
	public async Task<TravelLogResponseDto?> GetTravelLogByIdAsync(int id, int userId, CancellationToken ct = default)
	{
		var travelLog = await _db.TravelLogs
			.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

		if (travelLog == null)
			return null;

		return ToResponseDto(travelLog);
	}

	/// <summary>Delete a travel log.</summary>
	public async Task<bool> DeleteTravelLogAsync(int id, int userId, CancellationToken ct = default)
	{
		var travelLog = await _db.TravelLogs
			.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

		if (travelLog == null)
			return false;

		_db.TravelLogs.Remove(travelLog);
		await _db.SaveChangesAsync(ct);

		await UpdateUserTotalCarbonEmissionAsync(userId, ct);

		_logger.LogInformation("Travel log deleted successfully: UserId={UserId}, TravelLogId={TravelLogId}", userId, id);

		return true;
	}

	/// <summary>Get user travel statistics.</summary>
	public async Task<TravelStatisticsDto> GetUserTravelStatisticsAsync(
		int userId,
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default)
	{
		var query = _db.TravelLogs.Where(t => t.UserId == userId);

		// Date filter
		if (startDate.HasValue)
		{
			query = query.Where(t => t.CreatedAt >= startDate.Value);
		}

		if (endDate.HasValue)
		{
			// End date inclusive: add one day and use less-than
			var endDateInclusive = endDate.Value.Date.AddDays(1);
			query = query.Where(t => t.CreatedAt < endDateInclusive);
		}

		var travelLogs = await query.ToListAsync(ct);

		var statistics = new TravelStatisticsDto
		{
			TotalRecords = travelLogs.Count,
			TotalDistanceKilometers = travelLogs.Sum(t => t.DistanceKilometers),
			TotalCarbonEmission = travelLogs.Sum(t => t.CarbonEmission)
		};

		var byMode = travelLogs
			.GroupBy(t => t.TransportMode)
			.Select(g => new TransportModeStatisticsDto
			{
				TransportMode = g.Key,
				TransportModeName = GetTransportModeName(g.Key),
				RecordCount = g.Count(),
				TotalDistanceKilometers = g.Sum(t => t.DistanceKilometers),
				TotalCarbonEmission = g.Sum(t => t.CarbonEmission)
			})
			.OrderByDescending(s => s.TotalCarbonEmission)
			.ToList();

		statistics.ByTransportMode = byMode;

		return statistics;
	}

	#region Helpers

	/// <summary>Map TravelLog entity to response DTO.</summary>
	private TravelLogResponseDto ToResponseDto(TravelLog travelLog)
	{
		return new TravelLogResponseDto
		{
			Id = travelLog.Id,
			CreatedAt = travelLog.CreatedAt,
			TransportMode = travelLog.TransportMode,
			TransportModeName = GetTransportModeName(travelLog.TransportMode),
			OriginAddress = travelLog.OriginAddress,
			OriginLatitude = travelLog.OriginLatitude,
			OriginLongitude = travelLog.OriginLongitude,
			DestinationAddress = travelLog.DestinationAddress,
			DestinationLatitude = travelLog.DestinationLatitude,
			DestinationLongitude = travelLog.DestinationLongitude,
			DistanceMeters = travelLog.DistanceMeters,
			DistanceKilometers = travelLog.DistanceKilometers,
			DurationSeconds = travelLog.DurationSeconds,
			DurationText = FormatDuration(travelLog.DurationSeconds),
			CarbonEmission = travelLog.CarbonEmission,
			PassengerCount = travelLog.PassengerCount,
			RoutePolyline = travelLog.RoutePolyline,
			Notes = travelLog.Notes
		};
	}

	/// <summary>
	/// <summary>Get display name for transport mode.</summary>
	private string GetTransportModeName(TransportMode mode)
	{
		return mode switch
		{
			TransportMode.Walking => "Walking",
			TransportMode.Bicycle => "Bicycle",
			TransportMode.Motorcycle => "Motorcycle (Gas)",
			TransportMode.Subway => "Subway",
			TransportMode.Bus => "Bus",
			TransportMode.Taxi => "Taxi/Rideshare",
			TransportMode.CarGasoline => "Private Car (Gasoline)",
			TransportMode.CarElectric => "Private Car (Electric)",
			TransportMode.Ship => "Ship",
			TransportMode.Plane => "Plane",
			_ => mode.ToString()
		};
	}

	/// <summary>Great-circle distance in meters (for plane/ship when no driving route).</summary>
	private static int GetGreatCircleDistanceMeters(double lat1, double lon1, double lat2, double lon2)
	{
		const double R = 6_371_000; // Earth radius (m)
		var dLat = (lat2 - lat1) * Math.PI / 180;
		var dLon = (lon2 - lon1) * Math.PI / 180;
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
		        Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
		        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return (int)(R * c);
	}

	/// <summary>Map TransportMode to Google Maps travel mode string.</summary>
	private string GetGoogleMapsTravelMode(TransportMode mode)
	{
		return mode switch
		{
			TransportMode.Walking => "walking",
			TransportMode.Bicycle => "bicycling",
			TransportMode.Motorcycle => "driving",
			TransportMode.Subway => "transit",
			TransportMode.Bus => "transit",
			TransportMode.Taxi => "driving",
			TransportMode.CarGasoline => "driving",
			TransportMode.CarElectric => "driving",
			TransportMode.Ship => "driving",
			TransportMode.Plane => "driving",
			_ => "driving"
		};
	}

	/// <summary>Get carbon emission factor for transport mode.</summary>
	private async Task<CarbonReference?> GetCarbonFactorAsync(TransportMode mode, CancellationToken ct)
	{
		var labelName = GetCarbonReferenceLabelName(mode);
		return await _db.CarbonReferences
			.FirstOrDefaultAsync(c => c.LabelName == labelName, ct);
	}

	/// <summary>Map TransportMode to CarbonReference LabelName.</summary>
	private string GetCarbonReferenceLabelName(TransportMode mode)
	{
		return mode switch
		{
			TransportMode.Walking => "Walking",
			TransportMode.Bicycle => "Bicycle",
			TransportMode.Motorcycle => "Motorcycle",
			TransportMode.Subway => "Subway",
			TransportMode.Bus => "Bus",
			TransportMode.Taxi => "Taxi",
			TransportMode.CarGasoline => "CarGasoline",
			TransportMode.CarElectric => "CarElectric",
			TransportMode.Ship => "Ship",
			TransportMode.Plane => "Plane",
			_ => mode.ToString()
		};
	}

	/// <summary>PreFlightCheck: sanity check before map request; distinguishes Singapore vs global city level.</summary>
	private async Task PreFlightCheckAsync(
		TransportMode transportMode,
		GeocodingResult origin,
		GeocodingResult destination,
		double straightLineDistanceKm,
		CancellationToken ct)
	{
		var isSingapore = IsInSingapore(origin) && IsInSingapore(destination);
		var isGlobalCityLevel = IsMajorCity(origin) && IsMajorCity(destination);

		if (isSingapore)
			ValidateSingaporeLevel(transportMode, origin, destination, straightLineDistanceKm);
		else if (isGlobalCityLevel)
			ValidateGlobalCityLevel(transportMode, origin, destination, straightLineDistanceKm);
		else
			ValidateGeneralLevel(transportMode, origin, destination, straightLineDistanceKm);

		_logger.LogInformation(
			"PreFlightCheck passed: Mode={TransportMode}, Origin={Origin}, Dest={Dest}, Distance={Distance}km, Singapore={IsSG}, CityLevel={IsCity}",
			transportMode, SanitizeForLog(origin.FormattedAddress), SanitizeForLog(destination.FormattedAddress), straightLineDistanceKm, isSingapore, isGlobalCityLevel);
	}

	/// <summary>Whether the address is in Singapore.</summary>
	private bool IsInSingapore(GeocodingResult geocode)
	{
		var addressLower = geocode.FormattedAddress?.ToLowerInvariant() ?? string.Empty;
		var countryLower = geocode.Country?.ToLowerInvariant() ?? string.Empty;
		
		return countryLower.Contains("singapore") || 
		       countryLower.Contains("新加坡") ||
		       addressLower.Contains("singapore") || 
		       addressLower.Contains("新加坡") ||
		       System.Text.RegularExpressions.Regex.IsMatch(addressLower, @"\b\d{6}\b"); // Singapore postal code (6 digits)
	}

	/// <summary>Whether the address is a major global city (for city-level validation).</summary>
	private bool IsMajorCity(GeocodingResult geocode)
	{
		var majorCities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			// Asia
			"Tokyo", "Tokyo, Japan", "北京", "Beijing", "上海", "Shanghai", "广州", "Guangzhou", "深圳", "Shenzhen",
			"Hong Kong", "香港", "Seoul", "首尔", "Singapore", "新加坡", "Bangkok", "曼谷", "Kuala Lumpur", "吉隆坡",
			"Jakarta", "雅加达", "Manila", "马尼拉", "Mumbai", "孟买", "Delhi", "德里", "Bangalore", "班加罗尔",
			"Chennai", "钦奈", "Kolkata", "加尔各答", "Hyderabad", "海得拉巴", "Dubai", "迪拜", "Riyadh", "利雅得",
			"Tel Aviv", "特拉维夫", "Istanbul", "伊斯坦布尔",
			
			// Europe
			"London", "伦敦", "Paris", "巴黎", "Berlin", "柏林", "Madrid", "马德里", "Rome", "罗马", "Amsterdam", "阿姆斯特丹",
			"Brussels", "布鲁塞尔", "Vienna", "维也纳", "Zurich", "苏黎世", "Stockholm", "斯德哥尔摩", "Copenhagen", "哥本哈根",
			"Oslo", "奥斯陆", "Helsinki", "赫尔辛基", "Dublin", "都柏林", "Warsaw", "华沙", "Prague", "布拉格",
			"Budapest", "布达佩斯", "Athens", "雅典", "Lisbon", "里斯本", "Moscow", "莫斯科", "Saint Petersburg", "圣彼得堡",
			
			// North America
			"New York", "纽约", "Los Angeles", "洛杉矶", "Chicago", "芝加哥", "San Francisco", "旧金山", "Boston", "波士顿",
			"Washington", "华盛顿", "Seattle", "西雅图", "Miami", "迈阿密", "Toronto", "多伦多", "Vancouver", "温哥华",
			"Montreal", "蒙特利尔", 			"Mexico City", "墨西哥城",
			
			// South America
			"São Paulo", "圣保罗", "Rio de Janeiro", "里约热内卢", "Buenos Aires", "布宜诺斯艾利斯", "Lima", "利马",
			"Bogotá", "波哥大", "Santiago", "圣地亚哥",
			
			// Oceania
			"Sydney", "悉尼", "Melbourne", "墨尔本", "Auckland", "奥克兰", "Brisbane", "布里斯班", 			"Perth", "珀斯",
			
			// Africa
			"Cairo", "开罗", "Johannesburg", "约翰内斯堡", "Cape Town", "开普敦", "Lagos", "拉各斯", "Nairobi", "内罗毕"
		};

		var city = geocode.City ?? string.Empty;
		var address = geocode.FormattedAddress ?? string.Empty;
		
		return majorCities.Contains(city) || 
		       majorCities.Any(mc => address.Contains(mc, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>Singapore-level validation (street/postal).</summary>
	private void ValidateSingaporeLevel(TransportMode transportMode, GeocodingResult origin, GeocodingResult destination, double distanceKm)
	{
		if (transportMode == TransportMode.Plane)
		{
			throw new InvalidOperationException(
				"Plane is not valid within Singapore. Please use MRT, bus, taxi, or car.");
		}

		if (transportMode == TransportMode.Ship)
		{
			if (distanceKm > 5)
			{
				throw new InvalidOperationException(
					$"Within Singapore, distance {distanceKm:F1} km is not suitable for ship. Island ferries are usually under 5 km. Please use MRT, bus or other ground transport.");
			}
		}

		if (transportMode == TransportMode.Subway)
		{
			if (distanceKm > 50)
			{
				throw new InvalidOperationException(
					$"Within Singapore, distance {distanceKm:F1} km exceeds MRT range. Please check the addresses.");
			}
		}

		if (transportMode == TransportMode.Bus)
		{
			if (distanceKm > 50)
			{
				throw new InvalidOperationException(
					$"Within Singapore, distance {distanceKm:F1} km exceeds bus range. Please check the addresses.");
			}
		}

		if (transportMode == TransportMode.Walking)
		{
			if (distanceKm > 20)
			{
				throw new InvalidOperationException(
					$"Within Singapore, distance {distanceKm:F1} km is not suitable for walking. Please use MRT, bus or other transport.");
			}
		}

		if (transportMode == TransportMode.Bicycle || transportMode == TransportMode.Motorcycle)
		{
			if (distanceKm > 30)
			{
				var modeName = transportMode == TransportMode.Bicycle ? "bicycle" : "motorcycle";
				throw new InvalidOperationException(
					$"Within Singapore, distance {distanceKm:F1} km is not suitable for {modeName}. Please use MRT, bus or other transport.");
			}
		}

		if (transportMode == TransportMode.CarGasoline || transportMode == TransportMode.CarElectric || transportMode == TransportMode.Taxi)
		{
			if (distanceKm > 50)
			{
				throw new InvalidOperationException(
					$"Within Singapore, distance {distanceKm:F1} km exceeds reasonable range. Please check the addresses.");
			}
		}
	}

	/// <summary>
	/// <summary>Global major-city level validation.</summary>
	private void ValidateGlobalCityLevel(TransportMode transportMode, GeocodingResult origin, GeocodingResult destination, double distanceKm)
	{
		var sameCountry = !string.IsNullOrWhiteSpace(origin.Country) &&
		                  !string.IsNullOrWhiteSpace(destination.Country) &&
		                  string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);

		if (transportMode == TransportMode.Plane)
		{
			if (distanceKm < 200)
			{
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for plane. Please use train or other transport.");
			}
		}

		if (transportMode == TransportMode.Ship)
		{
			if (distanceKm < 50)
			{
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for ship. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Subway)
		{
			if (!sameCountry)
			{
				throw new InvalidOperationException(
					$"Subway cannot be used across countries. From {origin.Country} to {destination.Country} please use plane, train or other transport.");
			}
			if (distanceKm > 200)
			{
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for subway. Please use train or plane.");
			}
		}

		if (transportMode == TransportMode.Bus)
		{
			if (distanceKm > 500)
			{
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for bus. Please use train or plane.");
			}
		}

		if (transportMode == TransportMode.Walking)
		{
			if (distanceKm > 50)
			{
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for walking. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Bicycle || transportMode == TransportMode.Motorcycle)
		{
			if (distanceKm > 100)
			{
				var modeName = transportMode == TransportMode.Bicycle ? "bicycle" : "motorcycle";
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for {modeName}. Please use train or plane.");
			}
		}

		if (transportMode == TransportMode.CarGasoline || transportMode == TransportMode.CarElectric || transportMode == TransportMode.Taxi)
		{
			if (distanceKm > 1000)
			{
				throw new InvalidOperationException(
					$"Between major cities, distance {distanceKm:F1} km is not suitable for car/taxi. Consider plane or train.");
			}
		}
	}

	/// <summary>General-level validation (other cases).</summary>
	private void ValidateGeneralLevel(TransportMode transportMode, GeocodingResult origin, GeocodingResult destination, double distanceKm)
	{
		var sameCountry = !string.IsNullOrWhiteSpace(origin.Country) &&
		                  !string.IsNullOrWhiteSpace(destination.Country) &&
		                  string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);

		if (transportMode == TransportMode.Plane)
		{
			if (distanceKm < 100)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is not suitable for plane. Plane is typically for 100 km or more. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Ship)
		{
			if (distanceKm < 10)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is not suitable for ship. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Subway)
		{
			if (!sameCountry)
			{
				throw new InvalidOperationException(
					$"Subway cannot be used across countries. From {origin.Country} to {destination.Country} please use plane, train or other transport.");
			}
			if (distanceKm > 200)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is not suitable for subway. Please use train or plane.");
			}
		}

		if (transportMode == TransportMode.Bus)
		{
			if (distanceKm > 500)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is not suitable for bus. Please use train or plane.");
			}
		}

		if (transportMode == TransportMode.Walking)
		{
			if (distanceKm > 100)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is not suitable for walking. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Bicycle || transportMode == TransportMode.Motorcycle)
		{
			if (distanceKm > 50)
			{
				var modeName = transportMode == TransportMode.Bicycle ? "bicycle" : "motorcycle";
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is not suitable for {modeName}. Please choose another transport.");
			}
		}
	}

	/// <summary>Validate transport mode for route (async; checks infrastructure e.g. airport/port).</summary>
	private async Task ValidateTransportModeForRouteAsync(
		TransportMode transportMode, 
		GeocodingResult origin, 
		GeocodingResult destination, 
		double navigationDistanceKm,
		CancellationToken ct)
	{
		var distanceKm = navigationDistanceKm;

		var originAddressLower = origin.FormattedAddress?.ToLowerInvariant() ?? string.Empty;
		var destAddressLower = destination.FormattedAddress?.ToLowerInvariant() ?? string.Empty;
		var originCountryLower = origin.Country?.ToLowerInvariant() ?? string.Empty;
		var destCountryLower = destination.Country?.ToLowerInvariant() ?? string.Empty;

		var hasSingaporeKeyword = (originAddressLower.Contains("singapore") || originAddressLower.Contains("新加坡") ||
		                           originCountryLower.Contains("singapore") || originCountryLower.Contains("新加坡")) &&
		                          (destAddressLower.Contains("singapore") || destAddressLower.Contains("新加坡") ||
		                           destCountryLower.Contains("singapore") || destCountryLower.Contains("新加坡"));

		var sameCountry = !string.IsNullOrWhiteSpace(origin.Country) &&
		                  !string.IsNullOrWhiteSpace(destination.Country) &&
		                  string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);

		var isSingapore = (sameCountry && 
		                  (string.Equals(origin.Country, "Singapore", StringComparison.OrdinalIgnoreCase) ||
		                   string.Equals(origin.Country, "新加坡", StringComparison.OrdinalIgnoreCase))) ||
		                 hasSingaporeKeyword;

		if (transportMode == TransportMode.Plane)
		{
			if (distanceKm < 1)
			{
				throw new InvalidOperationException(
					$"Origin and destination are too close ({distanceKm:F2} km). Plane is not suitable. Please choose another transport.");
			}

			if (isSingapore)
			{
				throw new InvalidOperationException(
					"Plane is not valid within Singapore. Please use MRT, bus, taxi or car.");
			}

			if (distanceKm < 100)
			{
				throw new InvalidOperationException(
					$"Origin and destination are too close ({distanceKm:F1} km) for plane. Please choose another transport.");
			}

			var isVeryLongDistance = distanceKm > 1000;
			var isLongDistance = distanceKm > 500;
			var searchRadius = isVeryLongDistance ? 200000 : (isLongDistance ? 150000 : 100000);
			var strictMode = !isLongDistance;
			
			var originHasAirport = await HasNearbyInfrastructureAsync(
				origin.Latitude, origin.Longitude, "airport", searchRadius, ct, strict: strictMode);
			var destHasAirport = await HasNearbyInfrastructureAsync(
				destination.Latitude, destination.Longitude, "airport", searchRadius, ct, strict: strictMode);

			if (!originHasAirport)
			{
				if (isLongDistance)
				{
					_logger.LogWarning(
						"Airport check failed for origin at {Lat}, {Lng} ({Country}), but allowing {DistanceType} flight ({Distance:F1}km). " +
						"Assuming major cities have airports.", 
						origin.Latitude, origin.Longitude, origin.Country ?? "Unknown",
						isVeryLongDistance ? "very long-distance" : "long-distance", distanceKm);
				}
				else
				{
					throw new InvalidOperationException(
						$"No airport found within {searchRadius / 1000} km of origin. Plane is not suitable. Please choose another transport.");
				}
			}

			if (!destHasAirport)
			{
				if (isLongDistance)
				{
					_logger.LogWarning(
						"Airport check failed for destination at {Lat}, {Lng} ({Country}), but allowing {DistanceType} flight ({Distance:F1}km). " +
						"Assuming major cities have airports.", 
						destination.Latitude, destination.Longitude, destination.Country ?? "Unknown",
						isVeryLongDistance ? "very long-distance" : "long-distance", distanceKm);
				}
				else
				{
					throw new InvalidOperationException(
						$"No airport found within {searchRadius / 1000} km of destination. Plane is not suitable. Please choose another transport.");
				}
			}
		}

		if (transportMode == TransportMode.Ship)
		{
			if (distanceKm < 1)
			{
				throw new InvalidOperationException(
					$"Origin and destination are too close ({distanceKm:F2} km). Ship is not suitable. Please choose another transport.");
			}

			if (isSingapore && distanceKm < 50)
			{
				throw new InvalidOperationException(
					"Ship is not suitable for short trips within Singapore. Please use MRT, bus, taxi or car.");
			}

			if (distanceKm < 10)
			{
				throw new InvalidOperationException(
					$"Origin and destination are too close ({distanceKm:F1} km) for ship. Please choose another transport.");
			}

			var originHasPort = await HasNearbyInfrastructureAsync(origin.Latitude, origin.Longitude, "port", 30000, ct, strict: true) ||
			                    await HasNearbyInfrastructureAsync(origin.Latitude, origin.Longitude, "ferry terminal", 30000, ct, strict: true);
			var destHasPort = await HasNearbyInfrastructureAsync(destination.Latitude, destination.Longitude, "port", 30000, ct, strict: true) ||
			                  await HasNearbyInfrastructureAsync(destination.Latitude, destination.Longitude, "ferry terminal", 30000, ct, strict: true);

			if (!originHasPort)
			{
				throw new InvalidOperationException(
					"No port or ferry terminal found within 30 km of origin. Ship is only for coastal/ island locations. Please choose another transport.");
			}

			if (!destHasPort)
			{
				throw new InvalidOperationException(
					"No port or ferry terminal found within 30 km of destination. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Subway)
		{
			if (distanceKm > 200)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is too long for subway. Please use plane, train or long-distance bus.");
			}

			if (!sameCountry && !string.IsNullOrWhiteSpace(origin.Country) && !string.IsNullOrWhiteSpace(destination.Country))
			{
				throw new InvalidOperationException(
					$"Subway cannot be used across countries. From {origin.Country} to {destination.Country} please use plane or train.");
			}
		}

		if (transportMode == TransportMode.Bus)
		{
			if (distanceKm > 500)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is too long for bus. Please use plane or train.");
			}
		}

		if (transportMode == TransportMode.Walking)
		{
			if (distanceKm > 100)
			{
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is too long for walking. Please choose another transport.");
			}
		}

		if (transportMode == TransportMode.Bicycle || transportMode == TransportMode.Motorcycle)
		{
			if (distanceKm > 50)
			{
				var modeName = transportMode == TransportMode.Bicycle ? "bicycle" : "motorcycle";
				throw new InvalidOperationException(
					$"Distance {distanceKm:F1} km is too long for {modeName}. Please choose another transport.");
			}
		}

		_logger.LogInformation(
			"Transport mode validation passed: Mode={TransportMode}, Origin={OriginCountry}, Dest={DestCountry}, NavigationDistance={Distance}km",
			transportMode, origin.Country, destination.Country, distanceKm);
	}

	/// <summary>Check if infrastructure (e.g. airport, port) exists near the given location.</summary>
	/// <param name="latitude">Latitude.</param>
	/// <param name="longitude">Longitude.</param>
	/// <param name="keyword">Search keyword (e.g. "airport", "port").</param>
	/// <param name="radiusMeters">Search radius in meters.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <param name="strict">If true, API failure returns false; if false, returns true to avoid blocking user.</param>
	private async Task<bool> HasNearbyInfrastructureAsync(
		double latitude, 
		double longitude, 
		string keyword, 
		int radiusMeters,
		CancellationToken ct,
		bool strict = false)
	{
		try
		{
			var result = await _googleMapsService.SearchNearbyAsync(latitude, longitude, keyword, radiusMeters, ct);
			if (result == null || result.Places == null || result.Places.Count == 0)
			{
				_logger.LogDebug("No {Infrastructure} found near {Lat}, {Lng} within {Radius}m", keyword, MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude), radiusMeters);
				return false;
			}

			_logger.LogDebug("Found {Count} {Infrastructure} near {Lat}, {Lng}: {Places}", 
				result.Places.Count, keyword, MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude), 
				string.Join(", ", result.Places.Select(p => p.Name)));

			return result.Places.Count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error checking for {Infrastructure} near {Lat}, {Lng}", keyword, MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude));
			
			if (strict)
			{
				_logger.LogError(ex, "Strict mode: Infrastructure check failed for {Infrastructure} near {Lat}, {Lng}, rejecting transport mode", keyword, MaskCoordinateForLog(latitude), MaskCoordinateForLog(longitude));
				return false;
			}
			return true;
		}
	}

	/// <summary>Haversine formula: straight-line distance in km (for auxiliary use; validation should use navigation distance).</summary>
	private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
	{
		const double earthRadiusKm = 6371.0;

		var dLat = ToRadians(lat2 - lat1);
		var dLon = ToRadians(lon2 - lon1);

		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
		        Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
		        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return earthRadiusKm * c;
	}

	/// <summary>Convert degrees to radians.</summary>
	private static double ToRadians(double degrees)
	{
		return degrees * Math.PI / 180.0;
	}

	/// <summary>Format duration in seconds as readable string.</summary>
	private string? FormatDuration(int? seconds)
	{
		if (seconds == null)
			return null;

		var totalSeconds = seconds.Value;
		var hours = totalSeconds / 3600;
		var minutes = (totalSeconds % 3600) / 60;
		var secs = totalSeconds % 60;

		if (hours > 0)
			return $"{hours}h {minutes}m";
		if (minutes > 0)
			return $"{minutes}m";
		return $"{secs}s";
	}

	/// <summary>Get default passenger count for shared transport.</summary>
	private int GetPassengerCount(TransportMode transportMode)
	{
		return transportMode switch
		{
			TransportMode.Plane => 150,
			TransportMode.Ship => 200,
			TransportMode.Bus => 40,
			TransportMode.Subway => 200,
			_ => 1
		};
	}

	/// <summary>Per-person carbon emission; shared transport divided by passenger count.</summary>
	private decimal CalculatePerPersonCarbonEmission(TransportMode transportMode, decimal totalCarbonEmission, int passengerCount)
	{
		var isSharedTransport = transportMode == TransportMode.Plane ||
		                         transportMode == TransportMode.Ship ||
		                         transportMode == TransportMode.Bus ||
		                         transportMode == TransportMode.Subway;

		if (isSharedTransport && passengerCount > 0)
			return totalCarbonEmission / passengerCount;

		return totalCarbonEmission;
	}

	/// <summary>Update user total carbon emission from ActivityLogs, TravelLogs, UtilityBills.</summary>
	private async Task UpdateUserTotalCarbonEmissionAsync(int userId, CancellationToken ct)
	{
		try
		{
			var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
			if (user == null) return;

			var activityEmission = await _db.ActivityLogs
				.Where(a => a.UserId == userId)
				.SumAsync(a => (decimal?)a.TotalEmission, ct) ?? 0m;

			var travelEmission = await _db.TravelLogs
				.Where(t => t.UserId == userId)
				.SumAsync(t => (decimal?)t.CarbonEmission, ct) ?? 0m;

			var utilityEmission = await _db.UtilityBills
				.Where(u => u.UserId == userId)
				.SumAsync(u => (decimal?)u.TotalCarbonEmission, ct) ?? 0m;

			user.TotalCarbonEmission = activityEmission + travelEmission + utilityEmission;
			await _db.SaveChangesAsync(ct);
		}
		catch (Exception ex)
		{
				_logger.LogError(ex, "Failed to update TotalCarbonEmission for user {UserId}", userId);
		}
	}

	#endregion
}
