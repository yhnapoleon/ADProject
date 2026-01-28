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
    /// <param name="useDefault">为 true 时强制使用默认值，忽略 Open Food Facts 的结果</param>
    [HttpGet("{barcode}")]
    public async Task<ActionResult<BarcodeReferenceResponseDto>> GetByBarcode(string barcode, [FromQuery(Name = "refresh")] bool? refresh = null, [FromQuery(Name = "useDefault")] bool? useDefault = null, CancellationToken ct = default)
    {
        var barcodeRef = await _db.BarcodeReferences
            .Include(b => b.CarbonReference)
            .FirstOrDefaultAsync(b => b.Barcode == barcode, ct);

        if (barcodeRef != null && refresh == true)
        {
            var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);
            
            // 检查 Open Food Facts 是否找到有效产品
            var hasOffProduct = offProduct?.Product != null && offProduct.Status == 1;
            var hasValidProductName = hasOffProduct 
                && !string.IsNullOrWhiteSpace(offProduct.Product.ProductName)
                && !offProduct.Product.ProductName.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
            var hasValidCategories = hasOffProduct 
                && offProduct.Product.CategoriesTags != null 
                && offProduct.Product.CategoriesTags.Length > 0;
            var isOffProductValid = hasValidProductName && hasValidCategories;
            
            if (isOffProductValid && barcodeRef.CarbonReference != null)
            {
                // Open Food Facts 找到了有效产品
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
                    // OFF 无 co2_total 时，只有当有有效类别时才尝试用 Climatiq 获取因子
                    string categoryLabelName = "Unknown Food";
                    if (offProduct.Product.CategoriesTags != null && offProduct.Product.CategoriesTags.Any())
                    {
                        var firstTag = offProduct.Product.CategoriesTags.FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(firstTag))
                        {
                            categoryLabelName = firstTag;
                            var languagePrefixes = new[] { "en:", "fr:", "de:", "es:", "it:", "pt:", "nl:", "pl:", "ru:", "ja:", "zh:" };
                            foreach (var prefix in languagePrefixes)
                            {
                                if (categoryLabelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                {
                                    categoryLabelName = categoryLabelName.Substring(prefix.Length);
                                    break;
                                }
                            }
                            categoryLabelName = categoryLabelName.Replace("-", " ");
                        }
                    }
                    
                    // 只有当类别有效（不是 "Unknown Food"）时，才查询 Climatiq
                    bool hasValidCategory = categoryLabelName != "Unknown Food" 
                        && !string.IsNullOrWhiteSpace(categoryLabelName);
                    
                    if (hasValidCategory)
                    {
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
                            else
                            {
                                // Climatiq 没有返回有效值，使用默认值
                                barcodeRef.CarbonReference.Co2Factor = 0.5m;
                                barcodeRef.CarbonReference.Unit = "kgCO2e/kg";
                                barcodeRef.CarbonReference.Source = "Default";
                                barcodeRef.CarbonReference.ClimatiqActivityId = null;
                                await _db.SaveChangesAsync(ct);
                            }
                        }
                        catch
                        {
                            // Climatiq 不可用时，使用默认值
                            barcodeRef.CarbonReference.Co2Factor = 0.5m;
                            barcodeRef.CarbonReference.Unit = "kgCO2e/kg";
                            barcodeRef.CarbonReference.Source = "Default";
                            barcodeRef.CarbonReference.ClimatiqActivityId = null;
                            await _db.SaveChangesAsync(ct);
                        }
                    }
                    else
                    {
                        // 没有有效类别，使用默认值
                        barcodeRef.CarbonReference.Co2Factor = 0.5m;
                        barcodeRef.CarbonReference.Unit = "kgCO2e/kg";
                        barcodeRef.CarbonReference.Source = "Default";
                        barcodeRef.CarbonReference.ClimatiqActivityId = null;
                        await _db.SaveChangesAsync(ct);
                    }
                }
            }
            else if (barcodeRef.CarbonReference != null)
            {
                // Open Food Facts 找不到或产品信息无效，强制使用默认值
                const string defaultCategoryLabelName = "Unknown Food";
                const decimal defaultCo2Factor = 0.5m;
                const string defaultUnit = "kgCO2e/kg";
                const string defaultSource = "Default";
                
                // 查找或创建默认的 CarbonReference
                var defaultCarbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                    c => c.LabelName == defaultCategoryLabelName && c.Category == Models.Enums.CarbonCategory.Food, ct);
                
                if (defaultCarbonRef == null)
                {
                    defaultCarbonRef = new CarbonReference
                    {
                        LabelName = defaultCategoryLabelName,
                        Category = Models.Enums.CarbonCategory.Food,
                        Co2Factor = defaultCo2Factor,
                        Unit = defaultUnit,
                        Source = defaultSource
                    };
                    await _db.CarbonReferences.AddAsync(defaultCarbonRef, ct);
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    // 确保使用 0.5 默认值
                    defaultCarbonRef.Co2Factor = defaultCo2Factor;
                    defaultCarbonRef.Unit = defaultUnit;
                    defaultCarbonRef.Source = defaultSource;
                    defaultCarbonRef.ClimatiqActivityId = null;
                    await _db.SaveChangesAsync(ct);
                }
                
                // 更新 BarcodeReference 关联到默认的 CarbonReference
                barcodeRef.CarbonReferenceId = defaultCarbonRef.Id;
                barcodeRef.CarbonReference = defaultCarbonRef;
                barcodeRef.ProductName = "Unknown Product";
                barcodeRef.Category = defaultCategoryLabelName;
                barcodeRef.Brand = null;
                await _db.SaveChangesAsync(ct);
            }
        }

        if (barcodeRef is null)
        {
            // 如果强制使用默认值，直接跳过 Open Food Facts 查询
            if (useDefault == true)
            {
                // 直接使用默认值创建记录
                const string defaultCategoryLabelName = "Unknown Food";
                const decimal defaultCo2Factor = 0.5m;
                const string defaultUnit = "kgCO2e/kg";
                const string defaultSource = "Default";

                // 查找或创建默认的 CarbonReference
                // 注意：即使数据库中已存在，也要确保使用 0.5 默认值，而不是之前可能存储的其他值
                var defaultCarbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                    c => c.LabelName == defaultCategoryLabelName && c.Category == Models.Enums.CarbonCategory.Food, ct);

                if (defaultCarbonRef == null)
                {
                    defaultCarbonRef = new CarbonReference
                    {
                        LabelName = defaultCategoryLabelName,
                        Category = Models.Enums.CarbonCategory.Food,
                        Co2Factor = defaultCo2Factor,
                        Unit = defaultUnit,
                        Source = defaultSource
                    };
                    await _db.CarbonReferences.AddAsync(defaultCarbonRef, ct);
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    // 如果已存在，确保使用默认值 0.5，而不是之前可能存储的值
                    defaultCarbonRef.Co2Factor = defaultCo2Factor;
                    defaultCarbonRef.Unit = defaultUnit;
                    defaultCarbonRef.Source = defaultSource;
                    defaultCarbonRef.ClimatiqActivityId = null;
                    await _db.SaveChangesAsync(ct);
                }

                // 创建 BarcodeReference 使用默认值
                barcodeRef = new BarcodeReference
                {
                    Barcode = barcode,
                    ProductName = "Unknown Product",
                    CarbonReferenceId = defaultCarbonRef.Id,
                    Category = defaultCategoryLabelName,
                    Brand = null
                };
                barcodeRef.CarbonReference = defaultCarbonRef;
                await _db.BarcodeReferences.AddAsync(barcodeRef, ct);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                // 尝试从 Open Food Facts API 获取
                var offProduct = await _openFoodFactsService.GetProductByBarcodeAsync(barcode, ct);

                // 检查Open Food Facts返回的产品是否有效且信息完整
                // Status == 1 表示找到产品，但还需要检查产品信息是否完整
                // 如果产品名称为空、未知，或者没有类别信息，视为无效，使用默认值
                var hasOffProduct = offProduct?.Product != null && offProduct.Status == 1;
                var hasValidProductName = hasOffProduct 
                    && !string.IsNullOrWhiteSpace(offProduct.Product.ProductName)
                    && !offProduct.Product.ProductName.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
                var hasValidCategories = hasOffProduct 
                    && offProduct.Product.CategoriesTags != null 
                    && offProduct.Product.CategoriesTags.Length > 0;
                
                // 只有当产品名称和类别都有效时，才认为产品信息完整
                var isOffProductValid = hasValidProductName && hasValidCategories;

                if (isOffProductValid)
                {
                // 从 Open Food Facts 获取到产品信息
                decimal? extractedCo2Factor = null;
                string extractedUnit = "kgCO2e/kg";

                // 从 categories_tags 提取 category 作为 LabelName 用于查找或创建 CarbonReference
                string categoryLabelName = "Unknown Food"; // 默认值                
                if (offProduct.Product.CategoriesTags != null && offProduct.Product.CategoriesTags.Any())
                {
                    // 从 CategoriesTags 提取第一个标签，去除语言前缀
                    // 例如: "en:beverages" -> "beverages", "en:carbonated-drinks" -> "carbonated drinks"
                    var firstTag = offProduct.Product.CategoriesTags.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(firstTag))
                    {
                        categoryLabelName = firstTag;
                        
                        // 去除语言前缀 "en:", "fr:", "de:", "es:", "it:", "pt:" 等
                        var languagePrefixes = new[] { "en:", "fr:", "de:", "es:", "it:", "pt:", "nl:", "pl:", "ru:", "ja:", "zh:" };
                        foreach (var prefix in languagePrefixes)
                        {
                            if (categoryLabelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                categoryLabelName = categoryLabelName.Substring(prefix.Length);
                                break;
                            }
                        }
                        
                        // 将连字符替换为空格，例如 "carbonated-drinks" -> "carbonated drinks"
                        categoryLabelName = categoryLabelName.Replace("-", " ");
                    }
                }

                // 从 Open Food Facts 的 EcoScoreData.Agribalyse 取 CO2 Factor（kg CO2e/kg）
                var co2Raw = offProduct.Product.EcoScoreData?.Agribalyse?.Co2Total
                    ?? offProduct.Product.EcoScoreData?.AgribalyseCo2Total;
                if (co2Raw != null)
                    extractedCo2Factor = co2Raw.Value;

                string? climatiqActivityId = null;
                var fromClimatiq = false;
                
                // 逻辑：只有当有有效类别信息（且不是 "Unknown Food"）时，才尝试从 Climatiq 获取 CO2 因子
                // 如果类别是 "Unknown Food" 或没有类别，直接使用 0.5 默认值
                bool hasValidCategory = categoryLabelName != "Unknown Food" 
                    && !string.IsNullOrWhiteSpace(categoryLabelName)
                    && offProduct.Product.CategoriesTags != null 
                    && offProduct.Product.CategoriesTags.Length > 0;
                
                // OFF 无 co2_total 时，如果有有效类别，用 Climatiq 作为后备获取重量型（kg）碳排放因子
                // 如果没有有效类别或类别是 "Unknown Food"，直接使用 0.5 默认值，不查询 Climatiq
                if (!extractedCo2Factor.HasValue && hasValidCategory)
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
                // 如果没有有效类别或 Climatiq 获取失败，extractedCo2Factor 保持为 null，后续会使用 0.5 默认值

                // 查找或创建 category 对应的 CarbonReference
                // Open Food Facts 返回的都是 Food 类别
                var carbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                    c => c.LabelName == categoryLabelName && c.Category == Models.Enums.CarbonCategory.Food, ct);

                if (carbonRef != null)
                {
                    // 如果找到了已存在的 CarbonReference，更新它
                    if (extractedCo2Factor.HasValue)
                    {
                        carbonRef.Co2Factor = extractedCo2Factor.Value;
                        carbonRef.Unit = extractedUnit;
                        carbonRef.Source = fromClimatiq ? "Climatiq" : "OpenFoodFacts";
                        carbonRef.ClimatiqActivityId = fromClimatiq ? climatiqActivityId : null;
                        await _db.SaveChangesAsync(ct);
                    }
                    // 如果没有 CO2 因子（既没有从 OFF 获取，也没有从 Climatiq 获取），使用 0.5 默认值
                    else
                    {
                        carbonRef.Co2Factor = 0.5m;
                        carbonRef.Unit = extractedUnit;
                        carbonRef.Source = "Default";
                        carbonRef.ClimatiqActivityId = null;
                        await _db.SaveChangesAsync(ct);
                    }
                }
                else
                {
                    // 创建新的 CarbonReference，使用 category 作为 LabelName
                    // 如果没有 CO2 因子，使用 0.5 默认值
                    carbonRef = new CarbonReference
                    {
                        LabelName = categoryLabelName,
                        Category = Models.Enums.CarbonCategory.Food,
                        Co2Factor = extractedCo2Factor ?? 0.5m,
                        Unit = extractedUnit,
                        Source = extractedCo2Factor.HasValue ? (fromClimatiq ? "Climatiq" : "OpenFoodFacts") : "Default",
                        ClimatiqActivityId = fromClimatiq ? climatiqActivityId : null
                    };
                    await _db.CarbonReferences.AddAsync(carbonRef, ct);
                    await _db.SaveChangesAsync(ct);
                }

                // 创建 BarcodeReference 并关联到 carbonRef
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
                // Open Food Facts 中找不到该条形码，使用默认值创建记录
                const string defaultCategoryLabelName = "Unknown Food";
                const decimal defaultCo2Factor = 0.5m;
                const string defaultUnit = "kgCO2e/kg";
                const string defaultSource = "Default";

                // 查找或创建默认的 CarbonReference
                var defaultCarbonRef = await _db.CarbonReferences.FirstOrDefaultAsync(
                    c => c.LabelName == defaultCategoryLabelName && c.Category == Models.Enums.CarbonCategory.Food, ct);

                if (defaultCarbonRef == null)
                {
                    defaultCarbonRef = new CarbonReference
                    {
                        LabelName = defaultCategoryLabelName,
                        Category = Models.Enums.CarbonCategory.Food,
                        Co2Factor = defaultCo2Factor,
                        Unit = defaultUnit,
                        Source = defaultSource
                    };
                    await _db.CarbonReferences.AddAsync(defaultCarbonRef, ct);
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    // 如果已存在，确保使用 0.5 默认值，而不是之前可能存储的其他值（如 Climatiq 的 3.7）
                    defaultCarbonRef.Co2Factor = defaultCo2Factor;
                    defaultCarbonRef.Unit = defaultUnit;
                    defaultCarbonRef.Source = defaultSource;
                    defaultCarbonRef.ClimatiqActivityId = null;
                    await _db.SaveChangesAsync(ct);
                }

                // 创建 BarcodeReference 使用默认值
                barcodeRef = new BarcodeReference
                {
                    Barcode = barcode,
                    ProductName = "Unknown Product",
                    CarbonReferenceId = defaultCarbonRef.Id,
                    Category = defaultCategoryLabelName,
                    Brand = null
                };
                barcodeRef.CarbonReference = defaultCarbonRef;
                await _db.BarcodeReferences.AddAsync(barcodeRef, ct);
                await _db.SaveChangesAsync(ct);
            }
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
