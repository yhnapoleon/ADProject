using EcoLens.Api.DTOs.Travel;

namespace EcoLens.Api.Services;

/// <summary>
/// 出行记录服务接口
/// </summary>
public interface ITravelService
{
	/// <summary>
	/// 创建出行记录
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="dto">创建出行记录的请求DTO</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>出行记录响应DTO</returns>
	Task<TravelLogResponseDto> CreateTravelLogAsync(int userId, CreateTravelLogDto dto, CancellationToken ct = default);

	/// <summary>
	/// 预览路线和碳排放（不保存到数据库）
	/// </summary>
	/// <param name="dto">创建出行记录的请求DTO</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>路线预览DTO</returns>
	Task<RoutePreviewDto> PreviewRouteAsync(CreateTravelLogDto dto, CancellationToken ct = default);

	/// <summary>
	/// 获取用户的出行记录列表
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="query">查询参数（可选）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>分页的出行记录列表</returns>
	Task<PagedResultDto<TravelLogResponseDto>> GetUserTravelLogsAsync(
		int userId,
		GetTravelLogsQueryDto? query = null,
		CancellationToken ct = default);

	/// <summary>
	/// 根据ID获取单条出行记录
	/// </summary>
	/// <param name="id">记录ID</param>
	/// <param name="userId">用户ID（用于验证权限）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>出行记录响应DTO</returns>
	Task<TravelLogResponseDto?> GetTravelLogByIdAsync(int id, int userId, CancellationToken ct = default);

	/// <summary>
	/// 删除出行记录
	/// </summary>
	/// <param name="id">记录ID</param>
	/// <param name="userId">用户ID（用于验证权限）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>是否删除成功</returns>
	Task<bool> DeleteTravelLogAsync(int id, int userId, CancellationToken ct = default);

	/// <summary>
	/// 获取用户的出行记录统计信息
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="startDate">开始日期（可选）</param>
	/// <param name="endDate">结束日期（可选）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>统计信息</returns>
	Task<TravelStatisticsDto> GetUserTravelStatisticsAsync(
		int userId,
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default);
}
