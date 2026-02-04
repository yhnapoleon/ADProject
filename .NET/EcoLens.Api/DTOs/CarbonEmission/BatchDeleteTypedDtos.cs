namespace EcoLens.Api.DTOs.CarbonEmission;

/// <summary>
/// 批量删除条目（按类型）
/// </summary>
public class BatchDeleteItemDto
{
	public int Type { get; set; } // 1: food, 2: travel, 3: utility
	public int Id { get; set; }
}

/// <summary>
/// 批量删除请求（按类型）
/// </summary>
public class BatchDeleteTypedRequestDto
{
	public List<BatchDeleteItemDto> Items { get; set; } = new();
}

/// <summary>
/// 批量删除响应（按类型）
/// </summary>
public class BatchDeleteTypedResponseDto
{
	public int FoodRecordsDeleted { get; set; }
	public int TravelLogsDeleted { get; set; }
	public int UtilityBillsDeleted { get; set; }
	public int TotalDeleted { get; set; }
}


