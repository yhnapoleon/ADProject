using System;

namespace EcoLens.Api.DTOs.Food;

public class FoodCalculationResultDto
{
	public string FoodName { get; set; } = string.Empty;

	/// <summary>
	/// 查询到的碳排放因子（与 FactorUnit 对应的单位一致）
	/// </summary>
	public decimal Co2Factor { get; set; }

	/// <summary>
	/// 碳因子的单位（例如：kg 或 g）
	/// </summary>
	public string FactorUnit { get; set; } = string.Empty;

	/// <summary>
	/// 前端提交的份量数值
	/// </summary>
	public double Quantity { get; set; }

	/// <summary>
	/// 前端提交的份量单位
	/// </summary>
	public string QuantityUnit { get; set; } = string.Empty;

	/// <summary>
	/// 将份量换算到碳因子单位后的数值
	/// </summary>
	public double NormalizedQuantityInFactorUnit { get; set; }

	/// <summary>
	/// 计算得到的总碳排放量（单位同 FactorUnit）
	/// </summary>
	public decimal TotalEmission { get; set; }
}


