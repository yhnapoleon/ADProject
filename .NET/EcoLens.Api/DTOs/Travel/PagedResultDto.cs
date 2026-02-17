namespace EcoLens.Api.DTOs.Travel;

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResultDto<T>
{
	/// <summary>
	/// 数据列表
	/// </summary>
	public List<T> Items { get; set; } = new();

	/// <summary>
	/// 总记录数
	/// </summary>
	public int TotalCount { get; set; }

	/// <summary>
	/// 当前页码
	/// </summary>
	public int Page { get; set; }

	/// <summary>
	/// 每页数量
	/// </summary>
	public int PageSize { get; set; }

	/// <summary>
	/// 总页数
	/// </summary>
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

	/// <summary>
	/// 是否有上一页
	/// </summary>
	public bool HasPreviousPage => Page > 1;

	/// <summary>
	/// 是否有下一页
	/// </summary>
	public bool HasNextPage => Page < TotalPages;
}
