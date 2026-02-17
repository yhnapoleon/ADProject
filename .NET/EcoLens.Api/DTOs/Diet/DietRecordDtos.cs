using System;
using System.ComponentModel.DataAnnotations;

namespace EcoLens.Api.DTOs.Diet;

public class CreateDietRecordDto
{
	[Required]
	[MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 重量（kg）
	/// </summary>
	[Range(0, double.MaxValue)]
	public double Amount { get; set; }

	/// <summary>
	/// 排放因子（kgCO2/kg）
	/// </summary>
	public decimal EmissionFactor { get; set; }
}

public class DietRecordResponseDto
{
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public string Name { get; set; } = string.Empty;
	public double Amount { get; set; }
	public decimal EmissionFactor { get; set; }
	public decimal Emission { get; set; }
}

public class GetDietRecordsQueryDto
{
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }

	[Range(1, int.MaxValue)]
	public int Page { get; set; } = 1;

	[Range(1, 100)]
	public int PageSize { get; set; } = 20;
}
