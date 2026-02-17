namespace EcoLens.Api.Models.Enums;

/// <summary>
/// 出行方式枚举
/// </summary>
public enum TransportMode
{
	/// <summary>
	/// 步行
	/// </summary>
	Walking = 0,

	/// <summary>
	/// 自行车
	/// </summary>
	Bicycle = 1,

	/// <summary>
	/// 烧油的摩托车
	/// </summary>
	Motorcycle = 2,

	/// <summary>
	/// 地铁
	/// </summary>
	Subway = 3,

	/// <summary>
	/// 公交车
	/// </summary>
	Bus = 4,

	/// <summary>
	/// 出租车/网约车
	/// </summary>
	Taxi = 5,

	/// <summary>
	/// 私家车（汽油）
	/// </summary>
	CarGasoline = 6,

	/// <summary>
	/// 私家车（电动车）
	/// </summary>
	CarElectric = 7,

	/// <summary>
	/// 轮船
	/// </summary>
	Ship = 8,

	/// <summary>
	/// 飞机
	/// </summary>
	Plane = 9
}
