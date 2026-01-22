using System.ComponentModel.DataAnnotations;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Travel;

/// <summary>
/// 获取出行记录列表的查询参数
/// </summary>
public class GetTravelLogsQueryDto
{
	/// <summary>
	/// 开始日期（可选，格式：yyyy-MM-dd）
	/// </summary>
	public DateTime? StartDate { get; set; }

	/// <summary>
	/// 结束日期（可选，格式：yyyy-MM-dd）
	/// </summary>
	public DateTime? EndDate { get; set; }

	/// <summary>
	/// 出行方式筛选（可选）
	/// </summary>
	public TransportMode? TransportMode { get; set; }

	/// <summary>
	/// 页码（从1开始，默认1）
	/// </summary>
	[Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
	public int Page { get; set; } = 1;

	/// <summary>
	/// 每页数量（默认20，最大100）
	/// </summary>
	[Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
	public int PageSize { get; set; } = 20;
}
