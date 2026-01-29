using System;
using System.ComponentModel.DataAnnotations;

namespace EcoLens.Api.DTOs.Food;

public class GetFoodRecordsQueryDto
{
	/// <summary>
	/// 开始日期（可选）
	/// </summary>
	public DateTime? StartDate { get; set; }

	/// <summary>
	/// 结束日期（可选）
	/// </summary>
	public DateTime? EndDate { get; set; }

	/// <summary>
	/// 页码（默认1）
	/// </summary>
	[Range(1, int.MaxValue)]
	public int Page { get; set; } = 1;

	/// <summary>
	/// 每页数量（默认20，最大100）
	/// </summary>
	[Range(1, 100)]
	public int PageSize { get; set; } = 20;
}

public class FoodRecordResponseDto
{
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public string Name { get; set; } = string.Empty;
	public double Amount { get; set; }
	public decimal EmissionFactor { get; set; }
	public decimal Emission { get; set; }
}

