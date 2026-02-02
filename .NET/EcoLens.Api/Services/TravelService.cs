using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using EcoLens.Api.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Services;

/// <summary>
/// 出行记录服务实现
/// </summary>
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
	/// 创建出行记录
	/// </summary>
	public async Task<TravelLogResponseDto> CreateTravelLogAsync(int userId, CreateTravelLogDto dto, CancellationToken ct = default)
	{
		// 1. 地址转坐标（先检查缓存）
		var originGeocode = await _cacheService.GetCachedGeocodeAsync(dto.OriginAddress);
		if (originGeocode == null)
		{
			originGeocode = await _googleMapsService.GeocodeAsync(dto.OriginAddress, ct);
			if (originGeocode == null)
			{
				throw new InvalidOperationException($"Unable to geocode origin address: {dto.OriginAddress}");
			}
			// 保存到缓存
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
			// 保存到缓存
			await _cacheService.SetCachedGeocodeAsync(dto.DestinationAddress, destGeocode);
		}

		// 2. 获取路线信息（先获取导航距离，用于验证和计算）
		var travelMode = GetGoogleMapsTravelMode(dto.TransportMode);
		var route = await _googleMapsService.GetRouteAsync(
			originGeocode.Latitude,
			originGeocode.Longitude,
			destGeocode.Latitude,
			destGeocode.Longitude,
			travelMode,
			ct);

		if (route == null)
		{
			throw new InvalidOperationException("Unable to get route information");
		}

		// 2.5. 验证出行方式的合理性（在获取导航距离之后，使用实际导航距离进行验证）
		var navigationDistanceKm = route.DistanceMeters / 1000.0;
		await ValidateTransportModeForRouteAsync(dto.TransportMode, originGeocode, destGeocode, navigationDistanceKm, ct);

		// 3. 查找碳排放因子
		var carbonFactor = await GetCarbonFactorAsync(dto.TransportMode, ct);
		if (carbonFactor == null)
		{
			throw new InvalidOperationException($"Carbon emission factor not found for transport mode: {dto.TransportMode}");
		}

		// 4. 计算碳排放（使用导航距离）
		var distanceKm = (decimal)navigationDistanceKm;
		var totalCarbonEmission = distanceKm * carbonFactor.Co2Factor;

		// 4.5. 对于共享交通工具，按乘客数量分摊碳排放（使用默认乘客数量）
		var passengerCount = GetPassengerCount(dto.TransportMode);
		var carbonEmission = CalculatePerPersonCarbonEmission(dto.TransportMode, totalCarbonEmission, passengerCount);

		// 5. 创建实体
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

		// 6. 保存到数据库
		await _db.TravelLogs.AddAsync(travelLog, ct);
		await _db.SaveChangesAsync(ct);

		// 7. 更新用户总碳排放
		await UpdateUserTotalCarbonEmissionAsync(userId, ct);

		_logger.LogInformation("Travel log created successfully: UserId={UserId}, TravelLogId={TravelLogId}", userId, travelLog.Id);

		// 8. 转换为DTO返回
		return ToResponseDto(travelLog);
	}

	/// <summary>
	/// 预览路线和碳排放（不保存到数据库）
	/// </summary>
	public async Task<RoutePreviewDto> PreviewRouteAsync(CreateTravelLogDto dto, CancellationToken ct = default)
	{
		// 1. 地址转坐标（先检查缓存）
		var originGeocode = await _cacheService.GetCachedGeocodeAsync(dto.OriginAddress);
		if (originGeocode == null)
		{
			originGeocode = await _googleMapsService.GeocodeAsync(dto.OriginAddress, ct);
			if (originGeocode == null)
			{
				throw new InvalidOperationException($"Unable to geocode origin address: {dto.OriginAddress}");
			}
			// 保存到缓存
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
			// 保存到缓存
			await _cacheService.SetCachedGeocodeAsync(dto.DestinationAddress, destGeocode);
		}

		// 2. 获取路线信息（先获取导航距离，用于验证和计算）
		var travelMode = GetGoogleMapsTravelMode(dto.TransportMode);
		var route = await _googleMapsService.GetRouteAsync(
			originGeocode.Latitude,
			originGeocode.Longitude,
			destGeocode.Latitude,
			destGeocode.Longitude,
			travelMode,
			ct);

		if (route == null)
		{
			throw new InvalidOperationException("Unable to get route information");
		}

		// 2.5. 验证出行方式的合理性（在获取导航距离之后，使用实际导航距离进行验证）
		var navigationDistanceKm = route.DistanceMeters / 1000.0;
		await ValidateTransportModeForRouteAsync(dto.TransportMode, originGeocode, destGeocode, navigationDistanceKm, ct);

		// 3. 查找碳排放因子
		var carbonFactor = await GetCarbonFactorAsync(dto.TransportMode, ct);
		if (carbonFactor == null)
		{
			throw new InvalidOperationException($"Carbon emission factor not found for transport mode: {dto.TransportMode}");
		}

		// 4. 计算碳排放（使用导航距离）
		var distanceKm = (decimal)navigationDistanceKm;
		var totalCarbonEmission = distanceKm * carbonFactor.Co2Factor;

		// 4.5. 对于共享交通工具，按乘客数量分摊碳排放（使用默认乘客数量）
		var passengerCount = GetPassengerCount(dto.TransportMode);
		var carbonEmission = CalculatePerPersonCarbonEmission(dto.TransportMode, totalCarbonEmission, passengerCount);

		// 5. 转换为预览DTO
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

	/// <summary>
	/// 获取用户的出行记录列表（支持筛选和分页）
	/// </summary>
	public async Task<PagedResultDto<TravelLogResponseDto>> GetUserTravelLogsAsync(
		int userId,
		GetTravelLogsQueryDto? query = null,
		CancellationToken ct = default)
	{
		query ??= new GetTravelLogsQueryDto();

		var baseQuery = _db.TravelLogs.Where(t => t.UserId == userId);

		// 日期筛选
		if (query.StartDate.HasValue)
		{
			baseQuery = baseQuery.Where(t => t.CreatedAt >= query.StartDate.Value);
		}

		if (query.EndDate.HasValue)
		{
			var endDateInclusive = query.EndDate.Value.Date.AddDays(1);
			baseQuery = baseQuery.Where(t => t.CreatedAt < endDateInclusive);
		}

		// 出行方式筛选
		if (query.TransportMode.HasValue)
		{
			baseQuery = baseQuery.Where(t => t.TransportMode == query.TransportMode.Value);
		}

		// 获取总数
		var totalCount = await baseQuery.CountAsync(ct);

		// 分页查询
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

	/// <summary>
	/// 根据ID获取单条出行记录
	/// </summary>
	public async Task<TravelLogResponseDto?> GetTravelLogByIdAsync(int id, int userId, CancellationToken ct = default)
	{
		var travelLog = await _db.TravelLogs
			.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

		if (travelLog == null)
			return null;

		return ToResponseDto(travelLog);
	}

	/// <summary>
	/// 删除出行记录
	/// </summary>
	public async Task<bool> DeleteTravelLogAsync(int id, int userId, CancellationToken ct = default)
	{
		var travelLog = await _db.TravelLogs
			.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

		if (travelLog == null)
			return false;

		_db.TravelLogs.Remove(travelLog);
		await _db.SaveChangesAsync(ct);

		// 更新用户总碳排放
		await UpdateUserTotalCarbonEmissionAsync(userId, ct);

		_logger.LogInformation("Travel log deleted successfully: UserId={UserId}, TravelLogId={TravelLogId}", userId, id);

		return true;
	}

	/// <summary>
	/// 获取用户的出行记录统计信息
	/// </summary>
	public async Task<TravelStatisticsDto> GetUserTravelStatisticsAsync(
		int userId,
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default)
	{
		var query = _db.TravelLogs.Where(t => t.UserId == userId);

		// 日期筛选
		if (startDate.HasValue)
		{
			query = query.Where(t => t.CreatedAt >= startDate.Value);
		}

		if (endDate.HasValue)
		{
			// 结束日期包含整天，所以加一天并小于
			var endDateInclusive = endDate.Value.Date.AddDays(1);
			query = query.Where(t => t.CreatedAt < endDateInclusive);
		}

		var travelLogs = await query.ToListAsync(ct);

		// 计算总体统计
		var statistics = new TravelStatisticsDto
		{
			TotalRecords = travelLogs.Count,
			TotalDistanceKilometers = travelLogs.Sum(t => t.DistanceKilometers),
			TotalCarbonEmission = travelLogs.Sum(t => t.CarbonEmission)
		};

		// 按出行方式分组统计
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

	#region 辅助方法

	/// <summary>
	/// 将 TravelLog 实体转换为响应DTO
	/// </summary>
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
	/// 获取出行方式的中文名称
	/// </summary>
	private string GetTransportModeName(TransportMode mode)
	{
		return mode switch
		{
			TransportMode.Walking => "步行",
			TransportMode.Bicycle => "自行车",
			TransportMode.ElectricBike => "电动车",
			TransportMode.Subway => "地铁",
			TransportMode.Bus => "公交车",
			TransportMode.Taxi => "出租车/网约车",
			TransportMode.CarGasoline => "私家车（汽油）",
			TransportMode.CarElectric => "私家车（电动车）",
			TransportMode.Ship => "轮船",
			TransportMode.Plane => "飞机",
			_ => mode.ToString()
		};
	}

	/// <summary>
	/// 将 TransportMode 转换为 Google Maps API 的 travel mode 字符串
	/// </summary>
	private string GetGoogleMapsTravelMode(TransportMode mode)
	{
		return mode switch
		{
			TransportMode.Walking => "walking",
			TransportMode.Bicycle => "bicycling",
			TransportMode.ElectricBike => "bicycling", // 电动车使用自行车模式
			TransportMode.Subway => "transit",
			TransportMode.Bus => "transit",
			TransportMode.Taxi => "driving",
			TransportMode.CarGasoline => "driving",
			TransportMode.CarElectric => "driving",
			TransportMode.Ship => "driving", // 轮船使用driving模式（Google Maps不支持轮船路线）
			TransportMode.Plane => "driving", // 飞机使用driving模式（Google Maps不支持飞机路线）
			_ => "driving"
		};
	}

	/// <summary>
	/// 获取碳排放因子
	/// </summary>
	private async Task<CarbonReference?> GetCarbonFactorAsync(TransportMode mode, CancellationToken ct)
	{
		var labelName = GetCarbonReferenceLabelName(mode);
		return await _db.CarbonReferences
			.FirstOrDefaultAsync(c => c.LabelName == labelName, ct);
	}

	/// <summary>
	/// 将 TransportMode 转换为 CarbonReference 的 LabelName
	/// </summary>
	private string GetCarbonReferenceLabelName(TransportMode mode)
	{
		return mode switch
		{
			TransportMode.Walking => "Walking",
			TransportMode.Bicycle => "Bicycle",
			TransportMode.ElectricBike => "ElectricBike",
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

	/// <summary>
	/// 验证出行方式是否适合该路线（异步版本，支持基础设施检查）
	/// 避免在新加坡内部使用飞机、轮船等不合理的出行方式
	/// 检查实际交通工具的可用性（如机场、港口）
	/// </summary>
	private async Task ValidateTransportModeForRouteAsync(
		TransportMode transportMode, 
		GeocodingResult origin, 
		GeocodingResult destination, 
		double navigationDistanceKm,
		CancellationToken ct)
	{
		// 使用导航距离而不是直线距离进行验证
		var distanceKm = navigationDistanceKm;

		// 检查地址中是否包含新加坡关键词（即使 Country 字段为空也能检测）
		var originAddressLower = origin.FormattedAddress?.ToLowerInvariant() ?? string.Empty;
		var destAddressLower = destination.FormattedAddress?.ToLowerInvariant() ?? string.Empty;
		var originCountryLower = origin.Country?.ToLowerInvariant() ?? string.Empty;
		var destCountryLower = destination.Country?.ToLowerInvariant() ?? string.Empty;

		var hasSingaporeKeyword = (originAddressLower.Contains("singapore") || originAddressLower.Contains("新加坡") ||
		                           originCountryLower.Contains("singapore") || originCountryLower.Contains("新加坡")) &&
		                          (destAddressLower.Contains("singapore") || destAddressLower.Contains("新加坡") ||
		                           destCountryLower.Contains("singapore") || destCountryLower.Contains("新加坡"));

		// 检查是否在同一国家（特别是新加坡）
		var sameCountry = !string.IsNullOrWhiteSpace(origin.Country) &&
		                  !string.IsNullOrWhiteSpace(destination.Country) &&
		                  string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);

		var isSingapore = (sameCountry && 
		                  (string.Equals(origin.Country, "Singapore", StringComparison.OrdinalIgnoreCase) ||
		                   string.Equals(origin.Country, "新加坡", StringComparison.OrdinalIgnoreCase))) ||
		                 hasSingaporeKeyword;

		// 验证规则：
		// 1. 如果距离为 0 或非常小（< 1 公里），不允许使用飞机和轮船
		// 2. 如果都在新加坡，不允许使用飞机和轮船（短距离）
		// 3. 如果距离小于100公里，不允许使用飞机
		// 4. 如果距离小于10公里，不允许使用轮船

		if (transportMode == TransportMode.Plane)
		{
			// 首先检查距离是否为 0 或非常小
			if (distanceKm < 1)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过短（{distanceKm:F2} 公里），不能使用飞机。请选择其他出行方式。");
			}

			// 检查是否在新加坡内部
			if (isSingapore)
			{
				throw new InvalidOperationException(
					"在新加坡内部不能使用飞机作为出行方式。请选择其他出行方式，如地铁、公交、出租车或私家车。");
			}

			// 检查距离是否足够长
			if (distanceKm < 100)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过短（{distanceKm:F1} 公里），不适合使用飞机。飞机通常用于长途旅行（100公里以上）。请选择其他出行方式。");
			}

			// 检查出发地和目的地附近是否有机场（使用 Google Places API）
			// 策略：
			// 1. 超长途飞行（>1000公里，通常是跨洲/跨国家）：搜索半径200公里，API失败时允许（大城市通常都有国际机场）
			// 2. 长途飞行（500-1000公里）：搜索半径150公里，API失败时允许（主要城市通常都有机场）
			// 3. 中短途飞行（100-500公里）：搜索半径100公里，严格检查（必须找到机场）
			var isVeryLongDistance = distanceKm > 1000;
			var isLongDistance = distanceKm > 500;
			var searchRadius = isVeryLongDistance ? 200000 : (isLongDistance ? 150000 : 100000);
			var strictMode = !isLongDistance; // 只有中短途飞行使用严格模式
			
			var originHasAirport = await HasNearbyInfrastructureAsync(
				origin.Latitude, origin.Longitude, "airport", searchRadius, ct, strict: strictMode);
			var destHasAirport = await HasNearbyInfrastructureAsync(
				destination.Latitude, destination.Longitude, "airport", searchRadius, ct, strict: strictMode);

			// 对于长途/超长途飞行，如果API调用失败但距离足够长，允许继续（假设主要城市都有机场）
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
						$"出发地附近（{searchRadius / 1000}公里内）没有找到机场，无法使用飞机作为出行方式。请选择其他出行方式。");
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
						$"目的地附近（{searchRadius / 1000}公里内）没有找到机场，无法使用飞机作为出行方式。请选择其他出行方式。");
				}
			}
		}

		if (transportMode == TransportMode.Ship)
		{
			// 首先检查距离是否为 0 或非常小
			if (distanceKm < 1)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过短（{distanceKm:F2} 公里），不能使用轮船。请选择其他出行方式。");
			}

			// 检查是否在新加坡内部短距离
			if (isSingapore && distanceKm < 50)
			{
				// 新加坡是岛国，但内部短距离不应该用轮船
				throw new InvalidOperationException(
					"在新加坡内部短距离出行不适合使用轮船。请选择其他出行方式，如地铁、公交、出租车或私家车。");
			}

			// 检查距离是否足够长
			if (distanceKm < 10)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过短（{distanceKm:F1} 公里），不适合使用轮船。请选择其他出行方式。");
			}

			// 检查出发地和目的地附近是否有港口/码头（使用 Google Places API）
			// 对于轮船，必须严格检查，如果 API 调用失败，应该拒绝而不是允许
			var originHasPort = await HasNearbyInfrastructureAsync(origin.Latitude, origin.Longitude, "port", 30000, ct, strict: true) ||
			                    await HasNearbyInfrastructureAsync(origin.Latitude, origin.Longitude, "ferry terminal", 30000, ct, strict: true);
			var destHasPort = await HasNearbyInfrastructureAsync(destination.Latitude, destination.Longitude, "port", 30000, ct, strict: true) ||
			                  await HasNearbyInfrastructureAsync(destination.Latitude, destination.Longitude, "ferry terminal", 30000, ct, strict: true);

			if (!originHasPort)
			{
				throw new InvalidOperationException(
					$"出发地附近（30公里内）没有找到港口或码头，无法使用轮船作为出行方式。轮船只能用于有港口或码头的沿海城市或岛屿。请选择其他出行方式。");
			}

			if (!destHasPort)
			{
				throw new InvalidOperationException(
					$"目的地附近（30公里内）没有找到港口或码头，无法使用轮船作为出行方式。轮船只能用于有港口或码头的沿海城市或岛屿。请选择其他出行方式。");
			}
		}

		// 验证地铁：地铁通常只在城市内部或相邻城市之间运行，不适合超长距离
		if (transportMode == TransportMode.Subway)
		{
			// 地铁通常用于城市内部或相邻城市，超过 200 公里可能不合理
			if (distanceKm > 200)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过长（{distanceKm:F1} 公里），不适合使用地铁。地铁通常用于城市内部或相邻城市之间的短距离出行（200公里以内）。请选择其他出行方式，如飞机、火车或长途巴士。");
			}

			// 如果跨国家，地铁通常不可行
			if (!sameCountry && !string.IsNullOrWhiteSpace(origin.Country) && !string.IsNullOrWhiteSpace(destination.Country))
			{
				throw new InvalidOperationException(
					$"地铁不能用于跨国出行。从 {origin.Country} 到 {destination.Country} 请选择其他出行方式，如飞机或火车。");
			}
		}

		// 验证巴士：巴士可以用于中短距离，但超长距离可能不合理
		if (transportMode == TransportMode.Bus)
		{
			// 巴士通常用于中短距离，超过 500 公里可能不合理（虽然技术上可行，但通常有更好的选择）
			if (distanceKm > 500)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过长（{distanceKm:F1} 公里），不适合使用巴士。巴士通常用于中短距离出行（500公里以内）。对于超长距离，建议选择飞机或火车。请选择其他出行方式。");
			}
		}

		// 验证步行：步行可以用于较长距离（支持背包客等长距离步行）
		if (transportMode == TransportMode.Walking)
		{
			// 放宽限制以支持背包客等长距离步行，但超过 100 公里仍然不合理
			if (distanceKm > 100)
			{
				throw new InvalidOperationException(
					$"出发地和目的地距离过长（{distanceKm:F1} 公里），不适合步行。即使是长距离步行，通常也不会超过 100 公里。请选择其他出行方式。");
			}
		}

		// 验证自行车：自行车适合短到中距离
		if (transportMode == TransportMode.Bicycle || transportMode == TransportMode.ElectricBike)
		{
			if (distanceKm > 50)
			{
				var modeName = transportMode == TransportMode.Bicycle ? "自行车" : "电动车";
				throw new InvalidOperationException(
					$"出发地和目的地距离过长（{distanceKm:F1} 公里），不适合使用{modeName}。{modeName}通常用于短到中距离出行（50公里以内）。请选择其他出行方式。");
			}
		}

		_logger.LogInformation(
			"Transport mode validation passed: Mode={TransportMode}, Origin={OriginCountry}, Dest={DestCountry}, NavigationDistance={Distance}km",
			transportMode, origin.Country, destination.Country, distanceKm);
	}

	/// <summary>
	/// 检查指定位置附近是否有特定类型的基础设施（如机场、港口）
	/// </summary>
	/// <param name="latitude">纬度</param>
	/// <param name="longitude">经度</param>
	/// <param name="keyword">搜索关键词（如 "airport", "port"）</param>
	/// <param name="radiusMeters">搜索半径（米）</param>
	/// <param name="ct">取消令牌</param>
	/// <param name="strict">是否严格模式：如果为 true，API 调用失败时返回 false（拒绝）；如果为 false，API 调用失败时返回 true（允许，避免阻塞用户）</param>
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
				_logger.LogDebug("No {Infrastructure} found near {Lat}, {Lng} within {Radius}m", keyword, latitude, longitude, radiusMeters);
				return false;
			}

			_logger.LogDebug("Found {Count} {Infrastructure} near {Lat}, {Lng}: {Places}", 
				result.Places.Count, keyword, latitude, longitude, 
				string.Join(", ", result.Places.Select(p => p.Name)));

			return result.Places.Count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error checking for {Infrastructure} near {Lat}, {Lng}", keyword, latitude, longitude);
			
			// 严格模式：对于飞机和轮船，如果 API 调用失败，应该拒绝（返回 false）
			// 这样可以避免在没有基础设施的情况下错误地允许使用这些交通工具
			if (strict)
			{
				_logger.LogError(ex, "Strict mode: Infrastructure check failed for {Infrastructure} near {Lat}, {Lng}, rejecting transport mode", keyword, latitude, longitude);
				return false;
			}
			
			// 非严格模式：对于其他交通工具，如果 API 调用失败，返回 true（允许使用）
			// 这样可以避免因为 API 调用失败而阻止合理的出行方式
			return true;
		}
	}

	/// <summary>
	/// 使用 Haversine 公式计算两点间的直线距离（公里）
	/// 注意：此方法仅用于辅助计算，实际距离验证应使用导航距离
	/// </summary>
	private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
	{
		const double earthRadiusKm = 6371.0; // 地球半径（公里）

		var dLat = ToRadians(lat2 - lat1);
		var dLon = ToRadians(lon2 - lon1);

		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
		        Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
		        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return earthRadiusKm * c;
	}

	/// <summary>
	/// 将角度转换为弧度
	/// </summary>
	private static double ToRadians(double degrees)
	{
		return degrees * Math.PI / 180.0;
	}

	/// <summary>
	/// 格式化时间显示（秒转换为可读字符串）
	/// </summary>
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

	/// <summary>
	/// 获取乘客数量（使用默认值）
	/// </summary>
	private int GetPassengerCount(TransportMode transportMode)
	{
		// 对于共享交通工具，使用默认乘客数量
		return transportMode switch
		{
			TransportMode.Plane => 150,   // 飞机平均载客量
			TransportMode.Ship => 200,    // 轮船平均载客量
			TransportMode.Bus => 40,      // 巴士平均载客量
			TransportMode.Subway => 200,  // 地铁平均载客量
			_ => 1  // 其他交通工具（步行、自行车、私家车等）按1人计算
		};
	}

	/// <summary>
	/// 计算每人分摊的碳排放量
	/// 对于共享交通工具（飞机、轮船、巴士、地铁），按乘客数量分摊
	/// 对于其他交通工具，直接返回总碳排放
	/// </summary>
	private decimal CalculatePerPersonCarbonEmission(TransportMode transportMode, decimal totalCarbonEmission, int passengerCount)
	{
		// 共享交通工具需要按乘客数量分摊
		var isSharedTransport = transportMode == TransportMode.Plane ||
		                         transportMode == TransportMode.Ship ||
		                         transportMode == TransportMode.Bus ||
		                         transportMode == TransportMode.Subway;

		if (isSharedTransport && passengerCount > 0)
		{
			// 按乘客数量分摊：总碳排放 / 乘客数量
			return totalCarbonEmission / passengerCount;
		}

		// 其他交通工具（步行、自行车、私家车等）不需要分摊，直接返回总碳排放
		return totalCarbonEmission;
	}

	/// <summary>
	/// 更新用户总碳排放（从 ActivityLogs、TravelLogs、UtilityBills 汇总）
	/// </summary>
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
			// 记录错误但不影响主流程
			_logger.LogError(ex, "Failed to update TotalCarbonEmission for user {UserId}", userId);
		}
	}

	#endregion
}
