namespace EcoLens.Api.DTOs.Travel;

/// <summary>
/// 出行记录统计信息
/// </summary>
public class TravelStatisticsDto
{
	/// <summary>
	/// 总记录数
	/// </summary>
	public int TotalRecords { get; set; }

	/// <summary>
	/// 总距离（公里）
	/// </summary>
	public decimal TotalDistanceKilometers { get; set; }

	/// <summary>
	/// 总碳排放量（kg CO2）
	/// </summary>
	public decimal TotalCarbonEmission { get; set; }

	/// <summary>
	/// 按出行方式统计
	/// </summary>
	public List<TransportModeStatisticsDto> ByTransportMode { get; set; } = new();
}

/// <summary>
/// 按出行方式的统计信息
/// </summary>
public class TransportModeStatisticsDto
{
	/// <summary>
	/// 出行方式
	/// </summary>
	public Models.Enums.TransportMode TransportMode { get; set; }

	/// <summary>
	/// 出行方式名称
	/// </summary>
	public string TransportModeName { get; set; } = string.Empty;

	/// <summary>
	/// 记录数
	/// </summary>
	public int RecordCount { get; set; }

	/// <summary>
	/// 总距离（公里）
	/// </summary>
	public decimal TotalDistanceKilometers { get; set; }

	/// <summary>
	/// 总碳排放量（kg CO2）
	/// </summary>
	public decimal TotalCarbonEmission { get; set; }
}
