using EcoLens.Api.DTOs.Trip;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripController : ControllerBase
{
	/// <summary>
	/// 计算两点间出行的预计排放（Mock）。
	/// </summary>
	[HttpPost("calculate")]
	public async Task<ActionResult<TripCalculateResponseDto>> Calculate([FromBody] TripCalculateRequestDto dto, CancellationToken ct)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}

		// PB-005: 此处应替换为 Google Maps Routes API 调用，
		// 例如根据 StartLocation 与 EndLocation 调用 Directions/Routes API 获取精确距离（公里）。
		// 由于当前未配置真实 API Key，这里使用 1km - 50km 的随机距离进行模拟。
		await Task.CompletedTask;
		var distanceKm = Math.Round(1.0 + Random.Shared.NextDouble() * 49.0, 2);

		// 可选：这些因子也可以改为从数据库 CarbonReferences 中读取（Category=Transport）
		decimal factorPerKm = dto.TransportMode switch
		{
			TransportMode.CarGasoline => 0.21m, // 约 0.21 kgCO2/km
			TransportMode.Taxi => 0.20m, // 出租车
			TransportMode.Subway => 0.03m, // 地铁
			TransportMode.Bus => 0.05m, // 公交
			TransportMode.Ship => 0.03m, // 轮船
			TransportMode.Walking => 0.00m, // 步行为 0
			TransportMode.Bicycle => 0.00m, // 自行车为 0
			TransportMode.ElectricBike => 0.02m, // 电动车
			TransportMode.CarElectric => 0.05m, // 电动车
			TransportMode.Plane => 0.25m, // 飞机
			_ => 0.10m
		};

		var estimated = Math.Round((decimal)distanceKm * factorPerKm, 4);

		return Ok(new TripCalculateResponseDto
		{
			DistanceKm = distanceKm,
			EstimatedEmission = estimated,
			TransportMode = dto.TransportMode
		});
	}
}


