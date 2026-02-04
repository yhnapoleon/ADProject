namespace EcoLens.Api.DTOs.CarbonEmission;

/// <summary>
/// 批量删除碳排放记录请求DTO
/// </summary>
public class BatchDeleteCarbonEmissionDto
{
	/// <summary>
	/// 活动记录ID列表（可选）
	/// </summary>
	public List<int>? ActivityLogIds { get; set; }

	/// <summary>
	/// 出行记录ID列表（可选）
	/// </summary>
	public List<int>? TravelLogIds { get; set; }

	/// <summary>
	/// 水电账单ID列表（可选）
	/// </summary>
	public List<int>? UtilityBillIds { get; set; }
}
