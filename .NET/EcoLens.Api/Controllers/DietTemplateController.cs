using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EcoLens.Api.DTOs.Diet;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/diet/templates")]
[Authorize]
public class DietTemplateController : ControllerBase
{
	private readonly IDietTemplateService _service;

	public DietTemplateController(IDietTemplateService service)
	{
		_service = service;
	}

	private Guid? GetUserIdAsGuid()
	{
		// 尝试从常见的 claim 类型获取用户标识（例如 "sub" 或 NameIdentifier）
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("uid");
		if (Guid.TryParse(id, out var guid)) return guid;
		return null;
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CreateDietTemplateRequest request, CancellationToken ct)
	{
		var userId = GetUserIdAsGuid();
		if (userId is null) return Unauthorized();

		var created = await _service.CreateTemplateAsync(userId.Value, request, ct);
		// 返回 201 Created，定位到集合端点
		return Created(new Uri($"{Request.Scheme}://{Request.Host}/api/diet/templates"), created);
	}

	[HttpGet]
	public async Task<IActionResult> GetList(CancellationToken ct)
	{
		var userId = GetUserIdAsGuid();
		if (userId is null) return Unauthorized();

		var list = await _service.GetUserTemplatesAsync(userId.Value, ct);
		return Ok(list);
	}
}

