using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Barcode;
using EcoLens.Api.Models;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BarcodeController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IBarcodeLookupService _barcodeLookupService;

    public BarcodeController(ApplicationDbContext db, IBarcodeLookupService barcodeLookupService)
    {
        _db = db;
        _barcodeLookupService = barcodeLookupService;
    }

    /// <summary>
    /// ??????????????????   /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BarcodeReferenceResponseDto>>> Get([FromQuery] SearchBarcodeReferenceDto? searchDto, CancellationToken ct)
    {
        var query = _db.BarcodeReferences.AsQueryable();

        if (searchDto != null)
        {
            if (!string.IsNullOrWhiteSpace(searchDto.Barcode))
            {
                query = query.Where(b => b.Barcode.Contains(searchDto.Barcode));
            }
            if (!string.IsNullOrWhiteSpace(searchDto.ProductName))
            {
                query = query.Where(b => b.ProductName.Contains(searchDto.ProductName));
            }
            if (!string.IsNullOrWhiteSpace(searchDto.Category))
            {
                query = query.Where(b => b.Category != null && b.Category.Contains(searchDto.Category));
            }
            if (!string.IsNullOrWhiteSpace(searchDto.Brand))
            {
                query = query.Where(b => b.Brand != null && b.Brand.Contains(searchDto.Brand));
            }
        }

        var items = await query
            .Include(b => b.CarbonReference)
            .Select(b => new BarcodeReferenceResponseDto
            {
                Id = b.Id,
                Barcode = b.Barcode,
                ProductName = b.ProductName,
                CarbonReferenceId = b.CarbonReferenceId,
                CarbonReferenceLabel = b.CarbonReference!.LabelName,
                Co2Factor = b.CarbonReference.Co2Factor,
                Unit = b.CarbonReference.Unit,
                Source = b.CarbonReference.Source,
                ClimatiqActivityId = b.CarbonReference.ClimatiqActivityId,
                Category = b.Category,
                Brand = b.Brand
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// 根据条形码获取映射（优先本地；不存在时从 Open Food Facts 拉取并缓存）。
    /// </summary>
    /// <param name="barcode">条形码</param>
    /// <param name="refresh">为 true 时强制从 Open Food Facts 重新拉取并更新 Co2Factor</param>
    /// <param name="useDefault">为 true 时强制使用默认值，忽略 Open Food Facts 的结果</param>
    /// <param name="ct">取消令牌</param>
    [HttpGet("{barcode}")]
    public async Task<ActionResult<BarcodeReferenceResponseDto>> GetByBarcode(string barcode, [FromQuery(Name = "refresh")] bool? refresh = null, [FromQuery(Name = "useDefault")] bool? useDefault = null, CancellationToken ct = default)
    {
        var dto = await _barcodeLookupService.GetByBarcodeAsync(barcode, refresh, useDefault, ct);
        return Ok(dto);
    }

    /// <summary>
    /// 根据条形码删除记录
    /// </summary>
    [HttpDelete("{barcode}")]
    public async Task<IActionResult> DeleteByBarcode(string barcode, CancellationToken ct)
    {
        var barcodeRef = await _db.BarcodeReferences.FirstOrDefaultAsync(b => b.Barcode == barcode, ct);
        if (barcodeRef is null) return NotFound("Barcode reference not found.");

        _db.BarcodeReferences.Remove(barcodeRef);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
