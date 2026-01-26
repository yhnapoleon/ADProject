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
[Authorize] // ???????????????
public class BarcodeController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IOpenFoodFactsService _openFoodFactsService;
    private readonly IClimatiqService _climatiqService;

    public BarcodeController(ApplicationDbContext db, IOpenFoodFactsService openFoodFactsService, IClimatiqService climatiqService)
    {
        _db = db;
        _openFoodFactsService = openFoodFactsService;
        _climatiqService = climatiqService;
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
    /// <param name="refresh">为 true 时强制从 Open Food Facts 重新拉取并更新 Co2Factor（解决缓存了错误/旧数据）</param>
    [HttpGet("{barcode}")]
    public async Task<ActionResult<BarcodeReferenceResponseDto>> GetByBarcode(string barcode, [FromQuery(Name = "refresh")] bool? refresh = null, CancellationToken ct = default)
    {
        var barcodeRef = await _db.BarcodeReferences
            .Include(b => b.CarbonReference)
            .FirstOrDefaultAsync(b => b.Barcode == barcode, ct);

        if (barcodeRef != null && refresh == true)
        {
            var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);
            if (offProduct?.Product != null && offProduct.Status == 1 && barcodeRef.CarbonReference != null)
            {
                var co2 = offProduct.Product.EcoScoreData?.Agribalyse?.Co2Total
                    ?? offProduct.Product.EcoScoreData?.AgribalyseCo2Total;
                if (co2 != null)
                {
                    barcodeRef.CarbonReference.Co2Factor = co2.Value;
                    barcodeRef.CarbonReference.Unit = "kgCO2e/kg";
                    barcodeRef.CarbonReference.Source = "OpenFoodFacts";
                    barcodeRef.CarbonReference.ClimatiqActivityId = null;
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    // OFF 无 co2_total 时，尝试用 Climatiq 获取因子并更新
                    try
                    {
                        var activityId = ClimatiqActivityMapping.GetActivityIdForFood(offProduct.Product.CategoriesTags);
                        var estimate = await _climatiqService.GetCarbonEmissionEstimateAsync(activityId, 1m, "kg", ClimatiqActivityMapping.DefaultFoodRegion);
                        if (estimate != null && estimate.Co2e > 0)
                        {
                            // 根据食物类别应用调整系数
                            var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(offProduct.Product.CategoriesTags);
                            barcodeRef.CarbonReference.Co2Factor = estimate.Co2e * multiplier;
                            barcodeRef.CarbonReference.Unit = "kgCO2e/kg";
                            barcodeRef.CarbonReference.Source = "Climatiq";
                            barcodeRef.CarbonReference.ClimatiqActivityId = activityId;
                            await _db.SaveChangesAsync(ct);
                        }
                    }
                    catch { /* Climatiq 不可用时忽略 */ }
                }
            }
        }

        if (barcodeRef is null)
        {
            // ??? Open Food Facts API ????
            var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);

            if (offProduct?.Product != null && offProduct.Status == 1)
            {
                // ?? CO2 Factor??????
                decimal? extractedCo2Factor = null;
                string extractedUnit = "kgCO2e/kg";

                // ????????category????LabelName????????????CarbonReference
                string categoryLabelName = "Unknown Food"; // ????                
                if (offProduct.Product.CategoriesTags != null && offProduct.Product.CategoriesTags.Any())
                {
                    // ??CategoriesTags ?????????????????????                    // ??: "en:beverages" -> "beverages", "en:carbonated-drinks" -> "carbonated drinks"
                    var firstTag = offProduct.Product.CategoriesTags.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(firstTag))
                    {
                        categoryLabelName = firstTag;
                        
                        // ???????? "en:", "fr:", "de:", "es:", "it:", "pt:" ??
                        var languagePrefixes = new[] { "en:", "fr:", "de:", "es:", "it:", "pt:", "nl:", "pl:", "ru:", "ja:", "zh:" };
                        foreach (var prefix in languagePrefixes)
                        {
                            if (categoryLabelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                categoryLabelName = categoryLabelName.Substring(prefix.Length);
                                break;
                            }
                        }
                        
                        // ???????????????????? "carbonated-drinks" -> "carbonated drinks"??                        categoryLabelName = categoryLabelName.Replace("-", " ");
                    }
                }

                // 从 Open Food Facts 的 EcoScoreData.Agribalyse 取 CO2 Factor（kg CO2e/kg）
                var co2Raw = offProduct.Product.EcoScoreData?.Agribalyse?.Co2Total
                    ?? offProduct.Product.EcoScoreData?.AgribalyseCo2Total;
                if (co2Raw != null)
                    extractedCo2Factor = co2Raw.Value;

                string? climatiqActivityId = null;
                var fromClimatiq = false;
                // OFF 无 co2_total 时，用 Climatiq 作为后备获取重量型（kg）碳排放因子
                if (!extractedCo2Factor.HasValue)
                {
                    try
                    {
                        var activityId = ClimatiqActivityMapping.GetActivityIdForFood(offProduct.Product.CategoriesTags);
                        var estimate = await _climatiqService.GetCarbonEmissionEstimateAsync(activityId, 1m, "kg", ClimatiqActivityMapping.DefaultFoodRegion);
                        if (estimate != null && estimate.Co2e > 0)
                        {
                            // 根据食物类别应用调整系数，使不同类别的食物有不同的 Co2Factor
                            var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(offProduct.Product.CategoriesTags);
                            extractedCo2Factor = estimate.Co2e * multiplier;
                            climatiqActivityId = activityId;
                            fromClimatiq = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Climatiq 不可用时继续，后续用 0.5 兜底
                        // 临时输出错误信息以便调试（生产环境应使用 ILogger）
                        System.Diagnostics.Debug.WriteLine($"Climatiq API call failed for barcode {barcode}: {ex.Message}");
                        if (ex.InnerException != null)
                            System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                }

                // 查找或创建 category 对应的 CarbonReference
                // ???Open Food Facts ??????Food ??
                var carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                    c => c.LabelName == categoryLabelName && c.Category == Models.Enums.CarbonCategory.Food, ct);

                if (carbonRef != null)
                {
                    if (extractedCo2Factor.HasValue)
                    {
                        carbonRef.Co2Factor = extractedCo2Factor.Value;
                        carbonRef.Unit = extractedUnit;
                        carbonRef.Source = fromClimatiq ? "Climatiq" : "OpenFoodFacts";
                        carbonRef.ClimatiqActivityId = fromClimatiq ? climatiqActivityId : null;
                        await _db.SaveChangesAsync(ct);
                    }
                }
                else
                {
                    // ??????????????????CarbonReference????category ?? LabelName
                    carbonRef = new CarbonReference
                    {
                        LabelName = categoryLabelName,
                        Category = Models.Enums.CarbonCategory.Food,
                        Co2Factor = extractedCo2Factor ?? 0.5m,
                        Unit = extractedUnit,
                        Source = extractedCo2Factor.HasValue ? (fromClimatiq ? "Climatiq" : "OpenFoodFacts") : "Local",
                        ClimatiqActivityId = fromClimatiq ? climatiqActivityId : null
                    };
                    await _db.CarbonReferences.AddAsync(carbonRef, ct);
                    await _db.SaveChangesAsync(ct);
                }

                // ???? BarcodeReference ???????carbonRef ????????????? null?
                barcodeRef = new BarcodeReference
                {
                    Barcode = barcode,
                    ProductName = offProduct.Product.ProductName ?? "Unknown",
                    CarbonReferenceId = carbonRef.Id,
                    Category = categoryLabelName,
                    Brand = offProduct.Product.Brands
                };
                barcodeRef.CarbonReference = carbonRef;
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
            Source = barcodeRef.CarbonReference?.Source,
            ClimatiqActivityId = barcodeRef.CarbonReference?.ClimatiqActivityId,
            Category = barcodeRef.Category,
            Brand = barcodeRef.Brand
        });
    }

    /// <summary>
    /// ???????????    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BarcodeReferenceResponseDto>> Create([FromBody] CreateBarcodeReferenceDto dto, CancellationToken ct)
    {
        if (await _db.BarcodeReferences.AnyAsync(b => b.Barcode == dto.Barcode, ct))
        {
            return Conflict("Barcode already exists.");
        }

        // ??CarbonReferenceId ????
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
            // CarbonReferenceLabel, Co2Factor, Unit ???? Include ??????
            Category = newBarcodeRef.Category,
            Brand = newBarcodeRef.Brand
        };

        // ?? CarbonReferenceId ????????????????
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
    /// ?????????    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateBarcodeReferenceDto dto, CancellationToken ct)
    {
        var barcodeRef = await _db.BarcodeReferences.FirstOrDefaultAsync(b => b.Id == dto.Id, ct);
        if (barcodeRef is null) return NotFound("Barcode reference not found.");

        // ??CarbonReferenceId ????
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
    /// ???????    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var barcodeRef = await _db.BarcodeReferences.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (barcodeRef is null) return NotFound("Barcode reference not found.");

        _db.BarcodeReferences.Remove(barcodeRef);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// 临时测试端点：直接调用 Climatiq API（用于调试）
    /// </summary>
    [HttpGet("test-climatiq")]
    public async Task<ActionResult> TestClimatiq(CancellationToken ct)
    {
        try
        {
            var activityId = ClimatiqActivityMapping.DefaultFoodActivityId;
            var estimate = await _climatiqService.GetCarbonEmissionEstimateAsync(activityId, 1m, "kg", ClimatiqActivityMapping.DefaultFoodRegion);
            return Ok(new { 
                Success = true, 
                ActivityId = activityId, 
                Co2e = estimate?.Co2e, 
                Co2eUnit = estimate?.Co2eUnit 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Success = false, 
                Error = ex.Message, 
                InnerException = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// 测试 activity_id 映射逻辑：根据 categories_tags 返回映射结果
    /// </summary>
    [HttpGet("test-mapping")]
    [AllowAnonymous]
    public ActionResult TestMapping([FromQuery] string[]? categoriesTags)
    {
        var activityId = ClimatiqActivityMapping.GetActivityIdForFood(categoriesTags);
        return Ok(new
        {
            CategoriesTags = categoriesTags ?? Array.Empty<string>(),
            MappedActivityId = activityId,
            Region = ClimatiqActivityMapping.DefaultFoodRegion
        });
    }
}
