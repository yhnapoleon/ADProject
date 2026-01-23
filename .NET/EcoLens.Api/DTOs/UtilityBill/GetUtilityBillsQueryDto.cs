using System.ComponentModel.DataAnnotations;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.UtilityBill;

/// <summary>
/// 获取账单列表的查询参数 DTO
/// </summary>
public class GetUtilityBillsQueryDto
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
	/// 账单类型筛选（可选）
	/// </summary>
	/// <remarks>
	/// 枚举值说明：
	/// - 0: 电费（Electricity）
	/// - 1: 水费（Water）
	/// - 2: 燃气费（Gas）
	/// - 3: 综合账单（Combined）
	/// </remarks>
	public UtilityBillType? BillType { get; set; }

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
