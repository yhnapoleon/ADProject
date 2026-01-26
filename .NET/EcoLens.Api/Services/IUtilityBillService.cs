using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.DTOs.UtilityBill;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.Services;

/// <summary>
/// 水电账单业务服务接口
/// </summary>
public interface IUtilityBillService
{
	/// <summary>
	/// 上传账单文件并自动处理（OCR识别 → 数据提取 → 碳排放计算 → 保存）
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="file">账单文件（图片或PDF）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>处理后的账单响应DTO</returns>
	Task<UtilityBillResponseDto> UploadAndProcessBillAsync(int userId, IFormFile file, CancellationToken ct = default);

	/// <summary>
	/// 手动创建账单（碳排放计算 → 保存）
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="dto">手动输入账单数据</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>创建的账单响应DTO</returns>
	Task<UtilityBillResponseDto> CreateBillManuallyAsync(int userId, CreateUtilityBillManuallyDto dto, CancellationToken ct = default);

	/// <summary>
	/// 获取用户的账单列表（支持筛选和分页）
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="query">查询参数（可选）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>分页的账单列表</returns>
	Task<PagedResultDto<UtilityBillResponseDto>> GetUserBillsAsync(
		int userId,
		GetUtilityBillsQueryDto? query = null,
		CancellationToken ct = default);

	/// <summary>
	/// 根据ID获取单个账单详情
	/// </summary>
	/// <param name="id">账单ID</param>
	/// <param name="userId">用户ID（用于验证权限）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>账单响应DTO，如果不存在或无权访问返回null</returns>
	Task<UtilityBillResponseDto?> GetBillByIdAsync(int id, int userId, CancellationToken ct = default);

	/// <summary>
	/// 删除账单
	/// </summary>
	/// <param name="id">账单ID</param>
	/// <param name="userId">用户ID（用于验证权限）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>是否删除成功</returns>
	Task<bool> DeleteBillAsync(int id, int userId, CancellationToken ct = default);

	/// <summary>
	/// 获取用户的账单统计信息
	/// </summary>
	/// <param name="userId">用户ID</param>
	/// <param name="startDate">开始日期（可选）</param>
	/// <param name="endDate">结束日期（可选）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>统计信息DTO</returns>
	Task<UtilityBillStatisticsDto> GetUserStatisticsAsync(
		int userId,
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default);
}
