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

		// 2. 获取路线信息
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

		// 3. 查找碳排放因子
		var carbonFactor = await GetCarbonFactorAsync(dto.TransportMode, ct);
		if (carbonFactor == null)
		{
			throw new InvalidOperationException($"Carbon emission factor not found for transport mode: {dto.TransportMode}");
		}

		// 4. 计算碳排放
		var distanceKm = (decimal)route.DistanceMeters / 1000m;
		var carbonEmission = distanceKm * carbonFactor.Co2Factor;

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
			RoutePolyline = route.Polyline,
			Notes = dto.Notes
		};

		// 6. 保存到数据库
		await _db.TravelLogs.AddAsync(travelLog, ct);
		await _db.SaveChangesAsync(ct);

		_logger.LogInformation("Travel log created successfully: UserId={UserId}, TravelLogId={TravelLogId}", userId, travelLog.Id);

		// 7. 转换为DTO返回
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

		// 2. 获取路线信息
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

		// 3. 查找碳排放因子
		var carbonFactor = await GetCarbonFactorAsync(dto.TransportMode, ct);
		if (carbonFactor == null)
		{
			throw new InvalidOperationException($"Carbon emission factor not found for transport mode: {dto.TransportMode}");
		}

		// 4. 计算碳排放
		var distanceKm = (decimal)route.DistanceMeters / 1000m;
		var carbonEmission = distanceKm * carbonFactor.Co2Factor;

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
			TransportMode.Train => "火车",
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
			TransportMode.Train => "transit",
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
			TransportMode.Train => "Train",
			TransportMode.Plane => "Plane",
			_ => mode.ToString()
		};
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

	#endregion
}
