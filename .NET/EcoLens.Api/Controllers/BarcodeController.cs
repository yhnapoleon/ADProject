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
[Authorize] // 移除角色限制，但仍要求登录用户
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
                CarbonReference? carbonRef = null;
                decimal? extractedCo2Factor = null;
                string? extractedUnit = null;

                // 尝试从 Open Food Facts 的 EcoScoreData 中提取 CO2 Factor
                if (offProduct.Product.EcoScoreData?.Co2eTotal != null)
                {
                    extractedCo2Factor = offProduct.Product.EcoScoreData.Co2eTotal.Value / 100m; // 转换为 kgCO2e/kg
                    extractedUnit = offProduct.Product.EcoScoreData.Co2eUnit; // 假设单位是 kgCO2e

                    // 尝试在本地查找匹配的 CarbonReference，如果找到则更新其 Co2Factor
                    // 这里可以根据 LabelName, Category, Region 等进行更精细的查找
                    carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                        c => c.LabelName == offProduct.Product.ProductName || (offProduct.Product.CategoriesTags != null && offProduct.Product.CategoriesTags.Any(tag => c.LabelName == tag.Replace("en:", "", StringComparison.OrdinalIgnoreCase))), ct);
                    
                    if (carbonRef != null)
                    {
                        carbonRef.Co2Factor = extractedCo2Factor.Value; // 更新为实际值
                        carbonRef.Unit = extractedUnit ?? "kg"; // 更新单位
                        carbonRef.Source = "OpenFoodFacts";
                        await _db.SaveChangesAsync(ct);
                    }
                    else
                    {
                        // 如果没有找到现有匹配，创建一个新的 CarbonReference
                        carbonRef = new CarbonReference
                        {
                            LabelName = offProduct.Product.ProductName,
                            Category = offProduct.Product.CategoriesTags?.Any() == true ? Enum.Parse<Models.Enums.CarbonCategory>(offProduct.Product.CategoriesTags.FirstOrDefault()!.Replace("en:", "", StringComparison.OrdinalIgnoreCase), true) : Models.Enums.CarbonCategory.Food,
                            Co2Factor = extractedCo2Factor.Value,
                            Unit = extractedUnit ?? "kg",
                            Source = "OpenFoodFacts"
                        };
                        await _db.CarbonReferences.AddAsync(carbonRef, ct);
                        await _db.SaveChangesAsync(ct);
                    }
                }

                // 如果 EcoScoreData 中没有提取到，则执行现有匹配逻辑
                if (carbonRef is null)
                {
                    // 1. 尝试根据 Open Food Facts 的 CategoryTags 匹配 CarbonReference
                    if (offProduct.Product.CategoriesTags != null && offProduct.Product.CategoriesTags.Any())
                    {
                        foreach (var tag in offProduct.Product.CategoriesTags)
                        {
                            var cleanTag = tag.Replace("en:", "", StringComparison.OrdinalIgnoreCase);
                            if (Enum.TryParse<Models.Enums.CarbonCategory>(cleanTag, true, out var categoryEnum))
                            {
                                carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                                    c => c.LabelName == cleanTag || c.Category == categoryEnum, ct);
                            }
                            else
                            {
                                carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                                    c => c.LabelName == cleanTag, ct);
                            }
                            if (carbonRef != null) break;
                        }
                    }

                    // 2. 如果 CategoryTags 没有匹配到，尝试根据 ProductName 匹配
                    if (carbonRef is null && !string.IsNullOrWhiteSpace(offProduct.Product.ProductName))
                    {
                        carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                            c => c.LabelName == offProduct.Product.ProductName, ct);
                    }

                    // 3. 如果仍未找到匹配，则回退到查找或创建一个“Unknown Food”的 CarbonReference
                    if (carbonRef is null)
                    {
                        carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                            c => c.LabelName == "Unknown Food" && c.Category == Models.Enums.CarbonCategory.Food, ct);
                        if (carbonRef is null)
                        {
                            carbonRef = new CarbonReference
                            {
                                LabelName = "Unknown Food",
                                Category = Models.Enums.CarbonCategory.Food,
                                Co2Factor = 0.5m, // 默认值0.5，表示每公斤的碳排放量
                                Unit = "kg",
                                Source = "Local"
                            };
                            await _db.CarbonReferences.AddAsync(carbonRef, ct);
                            await _db.SaveChangesAsync(ct);
                        }
                    }
                }

                // 创建新的 BarcodeReference
                barcodeRef = new BarcodeReference
                {
                    Barcode = barcode,
                    ProductName = offProduct.Product.ProductName,
                    Category = offProduct.Product.CategoriesTags?.FirstOrDefault(),
                    Brand = offProduct.Product.Brands,
                    CarbonReferenceId = carbonRef?.Id // 关联到找到或创建的 CarbonReference
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
