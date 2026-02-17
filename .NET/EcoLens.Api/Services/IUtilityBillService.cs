using EcoLens.Api.DTOs.Travel;
using EcoLens.Api.DTOs.UtilityBill;
using Microsoft.AspNetCore.Http;

namespace EcoLens.Api.Services;

/// <summary>Utility bill business service interface.</summary>
public interface IUtilityBillService
{
	/// <summary>Upload and recognize bill file (OCR → extract → carbon calc; no save).</summary>
	/// <param name="userId">User ID</param>
	/// <param name="file">Bill file (image or PDF)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Recognized bill DTO (Id=0 until saved via manual API)</returns>
	Task<UtilityBillResponseDto> UploadAndProcessBillAsync(int userId, IFormFile file, CancellationToken ct = default);

	/// <summary>Create bill manually (carbon calc → save).</summary>
	/// <param name="userId">User ID</param>
	/// <param name="dto">Manual bill data</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Created bill DTO</returns>
	Task<UtilityBillResponseDto> CreateBillManuallyAsync(int userId, CreateUtilityBillManuallyDto dto, CancellationToken ct = default);

	/// <summary>Get user bill list with optional filter and paging.</summary>
	/// <param name="userId">User ID</param>
	/// <param name="query">Query (optional)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Paged bill list</returns>
	Task<PagedResultDto<UtilityBillResponseDto>> GetUserBillsAsync(
		int userId,
		GetUtilityBillsQueryDto? query = null,
		CancellationToken ct = default);

	/// <summary>Get single bill by ID.</summary>
	/// <param name="id">Bill ID</param>
	/// <param name="userId">User ID (for auth)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Bill DTO or null</returns>
	Task<UtilityBillResponseDto?> GetBillByIdAsync(int id, int userId, CancellationToken ct = default);

	/// <summary>Delete a bill.</summary>
	/// <param name="id">Bill ID</param>
	/// <param name="userId">User ID (for auth)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>True if deleted</returns>
	Task<bool> DeleteBillAsync(int id, int userId, CancellationToken ct = default);

	/// <summary>Get user bill statistics.</summary>
	/// <param name="userId">User ID</param>
	/// <param name="startDate">Start date (optional)</param>
	/// <param name="endDate">End date (optional)</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Statistics DTO</returns>
	Task<UtilityBillStatisticsDto> GetUserStatisticsAsync(
		int userId,
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default);
}
