namespace EcoLens.Api.DTOs.CarbonEmission;

/// <summary>
/// 批量删除响应DTO
/// </summary>
public class BatchDeleteResponseDto
{
	/// <summary>
	/// 删除的活动记录数量
	/// </summary>
	public int ActivityLogsDeleted { get; set; }

	/// <summary>
	/// 删除的出行记录数量
	/// </summary>
	public int TravelLogsDeleted { get; set; }

	/// <summary>
	/// 删除的水电账单数量
	/// </summary>
	public int UtilityBillsDeleted { get; set; }

	/// <summary>
	/// 总删除数量
	/// </summary>
	public int TotalDeleted { get; set; }
}
