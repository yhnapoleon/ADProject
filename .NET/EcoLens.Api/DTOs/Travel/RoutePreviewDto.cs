using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Travel;

/// <summary>
/// 路线预览 DTO（不保存到数据库，仅用于预览）
/// </summary>
public class RoutePreviewDto
{
	/// <summary>
	/// 出发地地址
	/// </summary>
	public string OriginAddress { get; set; } = string.Empty;

	/// <summary>
	/// 出发地纬度
	/// </summary>
	public decimal OriginLatitude { get; set; }

	/// <summary>
	/// 出发地经度
	/// </summary>
	public decimal OriginLongitude { get; set; }

	/// <summary>
	/// 目的地地址
	/// </summary>
	public string DestinationAddress { get; set; } = string.Empty;

	/// <summary>
	/// 目的地纬度
	/// </summary>
	public decimal DestinationLatitude { get; set; }

	/// <summary>
	/// 目的地经度
	/// </summary>
	public decimal DestinationLongitude { get; set; }

	/// <summary>
	/// 出行方式
	/// </summary>
	public TransportMode TransportMode { get; set; }

	/// <summary>
	/// 出行方式名称（中文）
	/// </summary>
	public string TransportModeName { get; set; } = string.Empty;

	/// <summary>
	/// 路线距离（米）
	/// </summary>
	public int DistanceMeters { get; set; }

	/// <summary>
	/// 路线距离（公里）
	/// </summary>
	public decimal DistanceKilometers { get; set; }

	/// <summary>
	/// 预计行驶时间（秒）
	/// </summary>
	public int? DurationSeconds { get; set; }

	/// <summary>
	/// 预计行驶时间（格式化字符串，例如：30分钟）
	/// </summary>
	public string? DurationText { get; set; }

	/// <summary>
	/// 预估碳排放量（kg CO2）
	/// </summary>
	public decimal EstimatedCarbonEmission { get; set; }

	/// <summary>
	/// 路线编码（Google Maps polyline，用于前端绘制地图路线）
	/// </summary>
	public string? RoutePolyline { get; set; }
}
