using System.ComponentModel.DataAnnotations;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.DTOs.Travel;

/// <summary>
/// 创建出行记录的请求 DTO
/// </summary>
public class CreateTravelLogDto
{
	/// <summary>
	/// 出发地地址（例如：北京市朝阳区，或新加坡邮编如160149）
	/// </summary>
	[Required(ErrorMessage = "Origin address is required")]
	[MaxLength(500, ErrorMessage = "Origin address cannot exceed 500 characters")]
	public string OriginAddress { get; set; } = string.Empty;

	/// <summary>
	/// 目的地地址（例如：北京市海淀区，或新加坡邮编如160149）
	/// </summary>
	[Required(ErrorMessage = "Destination address is required")]
	[MaxLength(500, ErrorMessage = "Destination address cannot exceed 500 characters")]
	public string DestinationAddress { get; set; } = string.Empty;

	/// <summary>
	/// 出行方式
	/// </summary>
	/// <remarks>
	/// 枚举值说明：
	/// - 0: 步行（Walking）
	/// - 1: 自行车（Bicycle）
	/// - 2: 摩托车/烧油（Motorcycle）
	/// - 3: 地铁（Subway）
	/// - 4: 公交车（Bus）
	/// - 5: 出租车/网约车（已取消，不可选）
	/// - 6: 私家车（汽油）（CarGasoline）
	/// - 7: 私家车（电动车）（CarElectric）
	/// - 8: 轮船（Ship）
	/// - 9: 飞机（Plane）
	/// </remarks>
	/// <example>3</example>
	[Required(ErrorMessage = "Transport mode is required")]
	public TransportMode TransportMode { get; set; }

	/// <summary>
	/// 备注（可选）
	/// </summary>
	[MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
	public string? Notes { get; set; }
}
