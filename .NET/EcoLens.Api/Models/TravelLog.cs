using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Models;

/// <summary>
/// 出行记录模型
/// </summary>
[Table("TravelLogs")]
public class TravelLog : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	/// <summary>
	/// 出行方式
	/// </summary>
	[Required]
	public TransportMode TransportMode { get; set; }

	/// <summary>
	/// 出发地地址
	/// </summary>
	[Required]
	[MaxLength(500)]
	public string OriginAddress { get; set; } = string.Empty;

	/// <summary>
	/// 出发地纬度
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(10,7)")]
	public decimal OriginLatitude { get; set; }

	/// <summary>
	/// 出发地经度
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(10,7)")]
	public decimal OriginLongitude { get; set; }

	/// <summary>
	/// 目的地地址
	/// </summary>
	[Required]
	[MaxLength(500)]
	public string DestinationAddress { get; set; } = string.Empty;

	/// <summary>
	/// 目的地纬度
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(10,7)")]
	public decimal DestinationLatitude { get; set; }

	/// <summary>
	/// 目的地经度
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(10,7)")]
	public decimal DestinationLongitude { get; set; }

	/// <summary>
	/// 路线距离（米）
	/// </summary>
	[Required]
	public int DistanceMeters { get; set; }

	/// <summary>
	/// 路线距离（公里）
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(10,2)")]
	public decimal DistanceKilometers { get; set; }

	/// <summary>
	/// 预计行驶时间（秒）
	/// </summary>
	public int? DurationSeconds { get; set; }

	/// <summary>
	/// 碳排放量（kg CO2）
	/// </summary>
	[Required]
	[Column(TypeName = "decimal(18,4)")]
	public decimal CarbonEmission { get; set; }

	/// <summary>
	/// 路线编码（Google Maps返回的polyline，用于前端绘制路线）
	/// </summary>
	[MaxLength(5000)]
	public string? RoutePolyline { get; set; }

	/// <summary>
	/// 备注
	/// </summary>
	[MaxLength(1000)]
	public string? Notes { get; set; }

	// 导航属性
	public ApplicationUser? User { get; set; }
}
