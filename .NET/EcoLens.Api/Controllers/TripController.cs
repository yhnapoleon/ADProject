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
			TransportMode.Driving => 0.21m, // 约 0.21 kgCO2/km
			TransportMode.Transit => 0.05m, // 地铁/公交较低
			TransportMode.Walking => 0.00m, // 步行为 0
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

	/// <summary>
	/// 路由别名：/api/travel/route/calculate（与前端文档对齐）。
	/// </summary>
	[HttpPost("/api/travel/route/calculate")]
	public Task<ActionResult<TripCalculateResponseDto>> CalculateAlias([FromBody] TripCalculateRequestDto dto, CancellationToken ct)
		=> Calculate(dto, ct);
}


