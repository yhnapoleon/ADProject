using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoLens.Api.Data;

/// <summary>
/// 提供食物标签到碳排放因子（kg CO2e / serving）的映射数据。
/// 包含了 Food-101 模型（西餐）和 CLIP 模型（亚洲/东南亚）的所有支持标签。
/// </summary>
public static class CarbonEmissionData
{
  /// <summary>
  /// 获取指定食物标签的估算碳排放量。
  /// </summary>
  /// <param name="label">食物标签（区分大小写）</param>
  /// <returns>碳排放量 (kg CO2e)，如果未找到则返回默认值 1.0</returns>
  public static double GetFactor(string label)
  {
    if (string.IsNullOrWhiteSpace(label)) return 1.0;

    // 尝试直接匹配
    if (Factors.TryGetValue(label, out double factor))
    {
      return factor;
    }

    // 尝试忽略大小写匹配（作为后备）
    var key = Factors.Keys.FirstOrDefault(k => k.Equals(label, StringComparison.OrdinalIgnoreCase));
    if (key != null)
    {
      return Factors[key];
    }

    // 默认值：无法识别时假设为一般熟食的平均值
    return 1.0;
  }

  /// <summary>
  /// 完整的碳排放因子字典。
  /// 单位：kg CO2e per serving (每份标准餐)
  /// </summary>
  public static readonly Dictionary<string, double> Factors = new Dictionary<string, double>
    {
            // =========================================================
            // Part 1: CLIP Model - Asian / Southeast Asian Food Labels
            // (通常为 Title Case 格式，来源于 asian_food_labels.txt)
            // =========================================================
            
            // --- Rice & Noodles (主食类) ---
            { "Hainanese Chicken Rice", 1.10 }, // 鸡肉+鸡油饭
            { "Fried Rice", 0.90 },             // 基础炒饭
            { "Nasi Goreng", 1.20 },            // 印尼炒饭(含甜酱油/肉)
            { "Nasi Lemak", 1.30 },             // 椰浆饭+炸鸡/蛋
            { "Biryani", 1.70 },                // 印度香饭(通常含羊肉/鸡肉)
            { "Bibimbap", 0.90 },               // 韩式拌饭(多蔬菜)
            { "Mango Sticky Rice", 0.70 },      // 甜点主食
            { "Char Kway Teow", 1.20 },         // 炒粿条(含蛤蜊/腊肠)
            { "Hokkien Mee", 1.20 },            // 福建炒面
            { "Wanton Mee", 1.00 },             // 云吞面
            { "Laksa", 1.30 },                  // 叻沙(椰浆汤底)
            { "Ramen", 1.40 },                  // 日式拉面(骨汤)
            { "Udon", 0.80 },                   // 乌冬面
            { "Pho", 1.50 },                    // 越南河粉(牛肉汤底)
            { "Vietnamese Pho", 1.50 },         // 同上
            { "Pad Thai", 1.00 },               // 泰式炒河粉
            { "Chow Mein", 0.80 },              // 炒面

            // --- Meat & Seafood Dishes (肉类与海鲜) ---
            { "Beef Rendang", 4.50 },           // 仁当牛肉(高碳排)
            { "Bulgogi", 3.80 },                // 韩式烤牛肉
            { "Roast Duck Rice", 1.40 },        // 烧鸭饭
            { "Peking Duck", 1.50 },            // 北京烤鸭
            { "Bak Kut Teh", 1.80 },            // 肉骨茶(猪肉)
            { "Sweet and Sour Pork", 1.60 },    // 咕咾肉
            { "Mapo Tofu", 0.60 },              // 麻婆豆腐
            { "Ma Po Tofu", 0.60 },             // 兼容拼写
            { "Kung Pao Chicken", 1.00 },       // 宫保鸡丁
            { "General Tso's Chicken", 1.10 },  // 左宗棠鸡
            { "Butter Chicken", 1.60 },         // 黄油鸡(乳制品高)
            { "Tandoori Chicken", 1.10 },       // 坦都里烤鸡
            { "Korean Fried Chicken", 1.20 },   // 韩式炸鸡
            { "Satay", 1.50 },                  // 沙爹肉串
            { "Yakitori", 1.20 },               // 日式烤串
            { "Chili Crab", 1.80 },             // 辣椒螃蟹
            { "Black Pepper Crab", 1.70 },      // 黑胡椒蟹
            { "Curry Fish Head", 1.90 },        // 咖喱鱼头
            { "Oyster Omelette", 1.00 },        // 蚝煎
            { "Sashimi", 1.30 },                // 刺身(空运)
            { "Sushi", 0.80 },                  // 寿司

            // --- Dim Sum & Snacks (点心与小吃) ---
            { "Dim Sum", 1.00 },                // 点心拼盘
            { "Har Gow", 0.90 },                // 虾饺
            { "Siew Mai", 0.80 },               // 烧卖
            { "Xiao Long Bao", 0.90 },          // 小笼包
            { "Char Siu Bao", 0.80 },           // 叉烧包
            { "Dumplings", 0.80 },              // 饺子
            { "Wonton Soup", 0.60 },            // 云吞汤
            { "Spring Rolls", 0.50 },           // 炸春卷
            { "Summer Rolls", 0.40 },           // 越南春卷(生)
            { "Roti Prata", 0.60 },             // 印度煎饼
            { "Naan", 0.50 },                   // 馕
            { "Samosa", 0.40 },                 // 咖喱角
            { "Takoyaki", 0.60 },               // 章鱼小丸子
            { "Okonomiyaki", 1.10 },            // 大阪烧
            { "Tempura", 0.90 },                // 天妇罗
            { "Tteokbokki", 0.70 },             // 辣炒年糕
            { "Kimchi", 0.20 },                 // 泡菜

            // --- Soups & Others (汤与其他) ---
            { "Tom Yum Soup", 0.90 },           // 冬阴功汤
            { "Miso Soup", 0.20 },              // 味噌汤
            { "Green Curry", 1.20 },            // 绿咖喱
            { "Hot Pot", 2.50 },                // 火锅(高能耗/多肉)
            { "Bubble Tea", 0.40 },             // 珍珠奶茶

            // =========================================================
            // Part 2: Food-101 Model - Western / International Labels
            // (通常为 snake_case 格式，来源于 Food-101 数据集)
            // =========================================================

            // --- High Carbon (Beef/Lamb) ---
            { "filet_mignon", 5.50 },           // 菲力牛排
            { "prime_rib", 5.50 },              // 肋排
            { "steak", 5.00 },                  // 牛排
            { "beef_carpaccio", 4.50 },         // 生牛肉片
            { "beef_tartare", 4.50 },           // 鞑靼牛肉
            { "hamburger", 3.50 },              // 汉堡
            { "baby_back_ribs", 2.80 },         // 烤猪肋排
            { "pulled_pork_sandwich", 2.50 },   // 手撕猪肉三明治
            { "foie_gras", 2.50 },              // 鹅肝
            { "pork_chop", 2.20 },              // 猪排
            { "lasagna", 2.20 },                // 千层面(肉酱+芝士)
            { "spaghetti_bolognese", 2.00 },    // 肉酱意面

            // --- Medium-High Carbon (Seafood/Cheese/Sandwiches) ---
            { "lobster_roll_sandwich", 1.90 },  // 龙虾卷
            { "lobster_bisque", 1.80 },         // 龙虾浓汤
            { "cheese_plate", 1.80 },           // 芝士拼盘
            { "breakfast_burrito", 1.80 },      // 早餐卷饼
            { "shrimp_and_grits", 1.70 },       // 虾仁玉米粥
            { "paella", 1.60 },                 // 西班牙海鲜饭
            { "crab_cakes", 1.60 },             // 蟹肉饼
            { "tacos", 1.60 },                  // 塔可
            { "tuna_tartare", 1.50 },           // 鞑靼金枪鱼
            { "club_sandwich", 1.50 },          // 公司三明治
            { "croque_madame", 1.50 },          // 库克太太三明治
            { "poutine", 1.50 },                // 肉汁薯条
            { "hot_dog", 1.50 },                // 热狗
            { "cheesecake", 1.50 },             // 芝士蛋糕
            { "fish_and_chips", 1.40 },         // 炸鱼薯条
            { "grilled_salmon", 1.40 },         // 烤三文鱼
            { "chicken_curry", 1.40 },          // 咖喱鸡
            { "macaroni_and_cheese", 1.40 },    // 芝士通心粉
            { "pizza", 1.40 },                  // 披萨
            { "chicken_quesadilla", 1.30 },     // 鸡肉薄饼
            { "clam_chowder", 1.30 },           // 蛤蜊浓汤
            { "grilled_cheese_sandwich", 1.30 },// 烤芝士三明治
            { "spaghetti_carbonara", 1.30 },    // 培根蛋面

            // --- Medium Carbon (Chicken/Eggs/Desserts) ---
            { "chicken_wings", 1.20 },          // 炸鸡翅
            { "fried_calamari", 1.20 },         // 炸鱿鱼
            { "eggs_benedict", 1.20 },          // 班尼迪克蛋
            { "quiche", 1.20 },                 // 法式咸派
            { "scallops", 1.20 },               // 扇贝
            { "nachos", 1.20 },                 // 玉米片
            { "creme_brulee", 1.20 },           // 焦糖布丁
            { "ceviche", 1.10 },                // 腌生鱼
            { "mussels", 1.10 },                // 青口贝
            { "risotto", 1.10 },                // 炖饭
            { "tiramisu", 1.10 },               // 提拉米苏
            { "chocolate_mousse", 1.10 },       // 巧克力慕斯
            { "caprese_salad", 1.00 },          // 卡布里沙拉(芝士)
            { "omelette", 1.00 },               // 欧姆蛋
            { "escargots", 1.00 },              // 焗蜗牛
            { "guacamole", 1.00 },              // 牛油果酱
            { "oysters", 1.00 },                // 生蚝
            { "panna_cotta", 1.00 },            // 奶冻
            { "chocolate_cake", 1.00 },         // 巧克力蛋糕
            { "ice_cream", 1.00 },              // 冰淇淋

            // --- Low-Medium Carbon (Pastries/Starters) ---
            { "baklava", 0.90 },                // 果仁蜜饼
            { "bread_pudding", 0.90 },          // 面包布丁
            { "french_onion_soup", 0.90 },      // 洋葱汤
            { "french_toast", 0.90 },           // 法式吐司
            { "ravioli", 0.90 },                // 意大利饺
            { "strawberry_shortcake", 0.90 },   // 草莓蛋糕
            { "red_velvet_cake", 0.90 },        // 红丝绒蛋糕
            { "apple_pie", 0.80 },              // 苹果派
            { "cannoli", 0.80 },                // 芝士卷
            { "carrot_cake", 0.80 },            // 胡萝卜蛋糕
            { "cup_cakes", 0.80 },              // 纸杯蛋糕
            { "deviled_eggs", 0.80 },           // 魔鬼蛋
            { "frozen_yogurt", 0.80 },          // 冻酸奶
            { "gnocchi", 0.80 },                // 土豆面疙瘩
            { "beignets", 0.70 },               // 炸面团
            { "caesar_salad", 0.70 },           // 凯撒沙拉
            { "garlic_bread", 0.70 },           // 蒜香面包
            { "macarons", 0.70 },               // 马卡龙
            { "onion_rings", 0.70 },            // 洋葱圈
            { "pancakes", 0.70 },               // 松饼
            { "waffles", 0.70 },                // 华夫饼

            // --- Low Carbon (Vegetarian/Sides) ---
            { "churros", 0.60 },                // 吉事果
            { "donuts", 0.60 },                 // 甜甜圈
            { "french_fries", 0.60 },           // 薯条
            { "greek_salad", 0.60 },            // 希腊沙拉
            { "hot_and_sour_soup", 0.60 },      // 酸辣汤
            { "bruschetta", 0.50 },             // 烤面包片
            { "falafel", 0.50 },                // 炸鹰嘴豆丸子
            { "beet_salad", 0.40 },             // 甜菜沙拉
            { "hummus", 0.40 },                 // 鹰嘴豆泥
            { "edamame", 0.30 },                // 毛豆
            { "seaweed_salad", 0.20 },          // 海藻沙拉

            // --- Cross-Model Handling (Food-101 duplicates of Asian items) ---
            // 这些是 Food-101 中的 snake_case 版本，值保持与 Asian 模型一致
            { "fried_rice", 0.90 },
            { "dumplings", 0.80 },
            { "sushi", 0.80 },
            { "sashimi", 1.30 },
            { "ramen", 1.40 },
            { "pho", 1.50 },
            { "pad_thai", 1.00 },
            { "spring_rolls", 0.50 },
            { "peking_duck", 1.50 },
            { "bibimbap", 0.90 },
            { "gyoza", 0.80 },                  // 对应 Dumplings/Potstickers
            { "miso_soup", 0.20 },
            { "samosa", 0.40 },
            { "takoyaki", 0.60 }
    };
}