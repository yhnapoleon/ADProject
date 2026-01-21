using EcoLens.Api.Data;
using EcoLens.Api.DTOs.Barcode;
using EcoLens.Api.Models;
using EcoLens.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // 只有管理员才能管理条形码数据库
public class BarcodeController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IOpenFoodFactsService _openFoodFactsService;

    public BarcodeController(ApplicationDbContext db, IOpenFoodFactsService openFoodFactsService)
    {
        _db = db;
        _openFoodFactsService = openFoodFactsService;
    }

    /// <summary>
    /// 获取所有条形码映射或根据查询条件搜索。
    /// </summary>
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
                Category = b.Category,
                Brand = b.Brand
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// 根据条形码获取单个条形码映射（优先从本地获取，如果不存在则尝试从 Open Food Facts API 获取并缓存）。
    /// </summary>
    [HttpGet("{barcode}")]
    public async Task<ActionResult<BarcodeReferenceResponseDto>> GetByBarcode(string barcode, CancellationToken ct)
    {
        var barcodeRef = await _db.BarcodeReferences
            .Include(b => b.CarbonReference)
            .FirstOrDefaultAsync(b => b.Barcode == barcode, ct);

        if (barcodeRef is null)
        {
            // 尝试从 Open Food Facts API 获取数据
            var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);

            if (offProduct?.Product != null && offProduct.Status == 1)
            {
                // 查找或创建一个默认的 CarbonReference
                // 这里简化处理，实际应用中可能需要更复杂的映射逻辑
                var defaultCarbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(c => c.LabelName == "Unknown Food" && c.Category == Models.Enums.CarbonCategory.Food, ct);
                if (defaultCarbonRef is null)
                {
                    defaultCarbonRef = new CarbonReference
                    {
                        LabelName = "Unknown Food",
                        Category = Models.Enums.CarbonCategory.Food,
                        Co2Factor = 0m, // 默认0，需要手动或通过其他方式更新
                        Unit = "kg",
                        Source = "Local"
                    };
                    await _db.CarbonReferences.AddAsync(defaultCarbonRef, ct);
                    await _db.SaveChangesAsync(ct);
                }

                // 创建新的 BarcodeReference
                barcodeRef = new BarcodeReference
                {
                    Barcode = barcode,
                    ProductName = offProduct.Product.ProductName,
                    Category = offProduct.Product.CategoriesTags.FirstOrDefault(),
                    Brand = offProduct.Product.Brands.FirstOrDefault(),
                    CarbonReferenceId = defaultCarbonRef.Id // 关联到默认 CarbonReference
                };
                await _db.BarcodeReferences.AddAsync(barcodeRef, ct);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                return NotFound("Barcode reference not found locally or via Open Food Facts.");
            }
        }

        return Ok(new BarcodeReferenceResponseDto
        {
            Id = barcodeRef.Id,
            Barcode = barcodeRef.Barcode,
            ProductName = barcodeRef.ProductName,
            CarbonReferenceId = barcodeRef.CarbonReferenceId,
            CarbonReferenceLabel = barcodeRef.CarbonReference?.LabelName,
            Co2Factor = barcodeRef.CarbonReference?.Co2Factor,
            Unit = barcodeRef.CarbonReference?.Unit,
            Category = barcodeRef.Category,
            Brand = barcodeRef.Brand
        });
    }

    /// <summary>
    /// 创建新的条形码映射。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BarcodeReferenceResponseDto>> Create([FromBody] CreateBarcodeReferenceDto dto, CancellationToken ct)
    {
        if (await _db.BarcodeReferences.AnyAsync(b => b.Barcode == dto.Barcode, ct))
        {
            return Conflict("Barcode already exists.");
        }

        // 检查 CarbonReferenceId 是否有效
        if (dto.CarbonReferenceId.HasValue && !await _db.CarbonReferences.AnyAsync(c => c.Id == dto.CarbonReferenceId.Value, ct))
        {
            return BadRequest("Invalid CarbonReferenceId.");
        }

        var newBarcodeRef = new BarcodeReference
        {
            Barcode = dto.Barcode,
            ProductName = dto.ProductName,
            CarbonReferenceId = dto.CarbonReferenceId,
            Category = dto.Category,
            Brand = dto.Brand
        };

        await _db.BarcodeReferences.AddAsync(newBarcodeRef, ct);
        await _db.SaveChangesAsync(ct);

        var responseDto = new BarcodeReferenceResponseDto
        {
            Id = newBarcodeRef.Id,
            Barcode = newBarcodeRef.Barcode,
            ProductName = newBarcodeRef.ProductName,
            CarbonReferenceId = newBarcodeRef.CarbonReferenceId,
            // CarbonReferenceLabel, Co2Factor, Unit 需要通过 Include 加载才能获取
            Category = newBarcodeRef.Category,
            Brand = newBarcodeRef.Brand
        };

        // 如果 CarbonReferenceId 有值，需要重新加载以获取关联数据
        if (newBarcodeRef.CarbonReferenceId.HasValue)
        {
            var loadedRef = await _db.BarcodeReferences
                .Include(b => b.CarbonReference)
                .FirstOrDefaultAsync(b => b.Id == newBarcodeRef.Id, ct);
            if (loadedRef?.CarbonReference != null)
            {
                responseDto.CarbonReferenceLabel = loadedRef.CarbonReference.LabelName;
                responseDto.Co2Factor = loadedRef.CarbonReference.Co2Factor;
                responseDto.Unit = loadedRef.CarbonReference.Unit;
            }
        }

        return CreatedAtAction(nameof(GetByBarcode), new { barcode = newBarcodeRef.Barcode }, responseDto);
    }

    /// <summary>
    /// 更新现有条形码映射。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateBarcodeReferenceDto dto, CancellationToken ct)
    {
        var barcodeRef = await _db.BarcodeReferences.FirstOrDefaultAsync(b => b.Id == dto.Id, ct);
        if (barcodeRef is null) return NotFound("Barcode reference not found.");

        // 检查 CarbonReferenceId 是否有效
        if (dto.CarbonReferenceId.HasValue && !await _db.CarbonReferences.AnyAsync(c => c.Id == dto.CarbonReferenceId.Value, ct))
        {
            return BadRequest("Invalid CarbonReferenceId.");
        }

        barcodeRef.ProductName = dto.ProductName ?? barcodeRef.ProductName;
        barcodeRef.CarbonReferenceId = dto.CarbonReferenceId ?? barcodeRef.CarbonReferenceId;
        barcodeRef.Category = dto.Category ?? barcodeRef.Category;
        barcodeRef.Brand = dto.Brand ?? barcodeRef.Brand;

        _db.BarcodeReferences.Update(barcodeRef);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// 删除条形码映射。
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var barcodeRef = await _db.BarcodeReferences.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (barcodeRef is null) return NotFound("Barcode reference not found.");

        _db.BarcodeReferences.Remove(barcodeRef);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

