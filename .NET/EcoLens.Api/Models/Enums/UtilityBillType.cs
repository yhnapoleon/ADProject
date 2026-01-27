namespace EcoLens.Api.Models.Enums;

/// <summary>
/// 水电账单类型枚举
/// </summary>
public enum UtilityBillType
{
	/// <summary>
	/// 电费账单
	/// </summary>
	Electricity = 0,

	/// <summary>
	/// 水费账单
	/// </summary>
	Water = 1,

	/// <summary>
	/// 燃气费账单
	/// </summary>
	Gas = 2,

	/// <summary>
	/// 综合账单（包含多种类型）
	/// </summary>
	Combined = 3
}
