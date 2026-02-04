package iss.nus.edu.sg.sharedprefs.admobile.data.model

    // 上传识别响应 & 手动保存响应 (共用)
    data class UtilityBillResponse(
        val id: Int,
        val billType: Int,
        val billPeriodStart: String,
        val billPeriodEnd: String,
        val electricityUsage: Double,
        val waterUsage: Double,
        val gasUsage: Double,
        val electricityCarbonEmission: Double,
        val waterCarbonEmission: Double,
        val totalCarbonEmission: Double
    )

    // 手动保存请求
    data class ManualUtilityRequest(
        val billType: Int = 0, // 默认 0
        val billPeriodStart: String,
        val billPeriodEnd: String,
        val electricityUsage: Double,
        val waterUsage: Double,
        val gasUsage: Double = 0.0, // 默认 0
        val notes: String? = ""
    )

data class UtilityHistoryResponse(
    val items: List<UtilityHistoryItem>,
    val totalCount: Int
)

data class UtilityHistoryItem(
    val id: Int,
    val billTypeName: String,
    val totalCarbonEmission: Double,
    val createdAt: String,
    val billPeriodStart: String,
    val billPeriodEnd: String,
    val electricityUsage: Double,
    val waterUsage: Double,
    val notes: String?
)
