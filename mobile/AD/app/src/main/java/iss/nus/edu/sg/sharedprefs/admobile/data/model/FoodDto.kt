package iss.nus.edu.sg.sharedprefs.admobile.data.model

// 2. 食物历史列表 (带分页)
data class FoodHistoryResponse(
    val items: List<FoodHistoryItem>,
    val totalCount: Int
)

data class FoodHistoryItem(
    val id: Int,
    val createdAt: String,
    val name: String,
    val emission: Double,
    val amount: Double,
    val emissionFactor: Double,
    val notes: String?
)

data class FoodAnalyzeResponse(
    val label: String,      // 识别出的食物名称
    val confidence: Double, // 置信度
    val source_model: String
)

// 定义请求体
data class CalculateFoodRequest(
    val name: String,
    val amount: Double
)

// 定义返回体
data class CalculateFoodResponse(
    val name: String = "",
    val amount: Double = 0.0,
    val emission_factor: Double = 0.0,
    val emission: Double = 0.0
)

data class AddFoodRequest(
    val name: String,
    val amount: Double,
    val emission_factor: Double,
    val emission: Double,
    val note: String?
)

data class AddFoodResponse(
    val success: Boolean
)

data class BarcodeResponse(
    val id: Int,
    val barcode: String,
    val productName: String,
    val co2Factor: Double,
    val unit: String?,
    val brand: String?,
    val category: String?
)

// 批量删除请求体
data class TypedDeleteEntry(
    val type: Int, // 1=Food, 2=Travel, 3=Utility
    val id: Int
)

// 批量删除响应 DTO
data class BatchDeleteTypedResponse(
    val foodRecordsDeleted: Int,
    val travelLogsDeleted: Int,
    val utilityBillsDeleted: Int,
    val totalDeleted: Int
)