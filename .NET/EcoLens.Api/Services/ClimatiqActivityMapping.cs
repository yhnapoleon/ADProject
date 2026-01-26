namespace EcoLens.Api.Services;

/// <summary>
/// 将 Open Food Facts 的 categories_tags 映射到 Climatiq 的 activity_id。
/// 用于条形码在 OFF 无 co2_total 时，通过 Climatiq 获取重量型（kg）碳排放因子。
/// </summary>
public static class ClimatiqActivityMapping
{
    /// <summary>
    /// 食品类 weight-based 因子的默认 activity_id（BEIS UK，kg CO2e/kg）。
    /// 该因子在 Climatiq 中仅支持 region=GB，适用于所有食品类别。
    /// </summary>
    public const string DefaultFoodActivityId = "consumer_goods-type_food_and_drink_primary_material_production";

    /// <summary>
    /// 调用 Climatiq 食品类 weight-based 因子时建议使用的 region（该因子仅支持 GB）。
    /// </summary>
    public const string DefaultFoodRegion = "GB";

    /// <summary>
    /// 根据 OFF 的 categories_tags 返回 Climatiq 的 activity_id。
    /// 
    /// 注意：目前 Climatiq 中只有 consumer_goods-type_food_and_drink_primary_material_production 
    /// 是 BEIS 来源的 weight-based 因子。其他食品类 activity_id（如 beverages、confectionery 等）
    /// 大多是 money-based，需要价格信息，不适合条形码场景。
    /// 
    /// 此方法根据 categories_tags 进行智能匹配，为未来可能的 weight-based 因子预留扩展空间。
    /// </summary>
    /// <param name="categoriesTags">OFF 的 categories_tags 数组，格式如 ["en:beverages", "en:carbonated-drinks"]</param>
    /// <returns>Climatiq activity_id</returns>
    public static string GetActivityIdForFood(string[]? categoriesTags)
    {
        // 目前所有类别都使用同一个 activity_id
        // 但可以通过 GetCo2MultiplierForFood 方法为不同类别应用不同的调整系数
        return DefaultFoodActivityId;
    }

    /// <summary>
    /// 根据 OFF 的 categories_tags 返回 Co2Factor 的调整系数。
    /// 基于科学研究，不同类别的食物有不同的平均碳排放强度。
    /// 这些系数基于通用食品因子（3.7 kgCO2e/kg）进行调整。
    /// </summary>
    /// <param name="categoriesTags">OFF 的 categories_tags 数组</param>
    /// <returns>调整系数（multiplier），1.0 表示使用默认值</returns>
    public static decimal GetCo2MultiplierForFood(string[]? categoriesTags)
    {
        if (categoriesTags == null || categoriesTags.Length == 0)
            return 1.0m; // 默认系数

        // 移除语言前缀，只保留分类名称
        var normalizedTags = categoriesTags
            .Select(tag => NormalizeCategoryTag(tag))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToList();

        var allTags = string.Join(" ", normalizedTags).ToLowerInvariant();

        // ===== 肉类（高碳排放）=====
        // 基于研究：牛肉 ~27 kgCO2e/kg, 猪肉 ~12, 鸡肉 ~6, 羊肉 ~24
        // 通用食品因子 ~3.7，所以肉类需要更高的系数
        if (ContainsAny(allTags, new[] { "beef", "meat", "pork", "lamb", "mutton" }))
        {
            // 牛肉最高，其他肉类中等
            if (ContainsAny(allTags, new[] { "beef" }))
                return 7.3m; // 27 / 3.7 ≈ 7.3
            if (ContainsAny(allTags, new[] { "lamb", "mutton" }))
                return 6.5m; // 24 / 3.7 ≈ 6.5
            if (ContainsAny(allTags, new[] { "pork" }))
                return 3.2m; // 12 / 3.7 ≈ 3.2
            // 其他肉类（鸡肉、火鸡等）
            return 1.6m; // 6 / 3.7 ≈ 1.6
        }

        // ===== 鱼类/海鲜 =====
        // 基于研究：鱼类 ~3-5 kgCO2e/kg
        if (ContainsAny(allTags, new[] { "fish", "seafood", "salmon", "tuna", "shrimp", "prawn" }))
        {
            return 1.1m; // 4 / 3.7 ≈ 1.1
        }

        // ===== 乳制品 =====
        // 基于研究：奶酪 ~10-12, 黄油 ~12, 牛奶 ~3
        if (ContainsAny(allTags, new[] { "cheese" }))
            return 3.0m; // 11 / 3.7 ≈ 3.0
        if (ContainsAny(allTags, new[] { "butter" }))
            return 3.2m; // 12 / 3.7 ≈ 3.2
        if (ContainsAny(allTags, new[] { "dairy", "milk", "yogurt", "yoghurt", "cream" }))
            return 0.8m; // 3 / 3.7 ≈ 0.8

        // ===== 零食/糖果类（加工食品，中等碳排放）=====
        // 基于研究：巧克力 ~19, 糖果 ~3-4
        if (ContainsAny(allTags, new[] { "chocolate" }))
            return 5.1m; // 19 / 3.7 ≈ 5.1
        if (ContainsAny(allTags, new[] { 
            "snack", "confectionery", "sweet", "candy", 
            "dessert", "biscuit", "cookie", "cracker", "chip", "crisp" }))
        {
            return 1.0m; // 3.5 / 3.7 ≈ 0.95，四舍五入为 1.0
        }

        // ===== 饮料类（低到中等碳排放）=====
        // 基于研究：果汁 ~1.5, 软饮料 ~0.5, 咖啡 ~17（但按重量算很低）
        if (ContainsAny(allTags, new[] { "coffee" }))
            return 0.4m; // 咖啡豆密度低，按重量算系数较低
        if (ContainsAny(allTags, new[] { "juice" }))
            return 0.4m; // 1.5 / 3.7 ≈ 0.4
        if (ContainsAny(allTags, new[] { 
            "beverage", "drink", "carbonated", "non-carbonated", "water", 
            "soft", "soda", "tea", "alcoholic", "beer", "wine", "spirits", "cider" }))
        {
            return 0.3m; // 0.5-1.0 / 3.7 ≈ 0.1-0.3，取 0.3
        }

        // ===== 面包/谷物（低碳排放）=====
        // 基于研究：面包 ~1.0, 米饭 ~4, 面食 ~1.5
        if (ContainsAny(allTags, new[] { "rice" }))
            return 1.1m; // 4 / 3.7 ≈ 1.1
        if (ContainsAny(allTags, new[] { 
            "bread", "cereal", "grain", "pasta", "flour", "wheat" }))
        {
            return 0.4m; // 1.5 / 3.7 ≈ 0.4
        }

        // ===== 水果/蔬菜（最低碳排放）=====
        // 基于研究：水果 ~0.5-1.0, 蔬菜 ~0.3-0.8
        if (ContainsAny(allTags, new[] { 
            "fruit", "vegetable", "fresh", "organic" }))
        {
            return 0.2m; // 0.5-0.8 / 3.7 ≈ 0.1-0.2，取 0.2
        }

        // 默认系数（适用于未分类或混合食品）
        return 1.0m;
    }

    /// <summary>
    /// 标准化 OFF 的 category tag，移除语言前缀（如 "en:", "fr:" 等）。
    /// </summary>
    private static string NormalizeCategoryTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        var normalized = tag.Trim();

        // 移除常见语言前缀
        var languagePrefixes = new[] { 
            "en:", "fr:", "de:", "es:", "it:", "pt:", "nl:", "pl:", 
            "ru:", "ja:", "zh:", "zh-cn:", "zh-tw:", "ko:", "ar:" 
        };

        foreach (var prefix in languagePrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(prefix.Length);
                break;
            }
        }

        // 将连字符替换为空格，便于匹配
        normalized = normalized.Replace("-", " ").Replace("_", " ");

        return normalized.Trim();
    }

    /// <summary>
    /// 检查字符串是否包含任意一个关键词（不区分大小写）。
    /// </summary>
    private static bool ContainsAny(string text, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text) || keywords == null || keywords.Length == 0)
            return false;

        return keywords.Any(keyword => 
            text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
