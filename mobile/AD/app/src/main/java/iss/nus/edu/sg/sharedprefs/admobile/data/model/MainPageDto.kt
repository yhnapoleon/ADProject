package iss.nus.edu.sg.sharedprefs.admobile.data.model

data class MainPageResponseDto(
    val total: Double,
    val food: Double,
    val transport: Double,
    val utility: Double
)

data class UserStatsResponse(
    val month: String,
    val emissionsTotal: Double,
    val food: Double,
    val transport: Double,
    val utility: Double,
    val averageAllUsers: Double
)

// 请求体
data class StepSyncRequest(
    val stepCount: Int,
    val date: String // 后端要求的格式: "2026-02-04T05:58:42.407Z"
)

// 响应体
data class StepSyncResponse(
    val totalSteps: Int,
    val usedSteps: Int,
    val availableSteps: Int
)

data class TreeResponse(
    val totalTrees: Int,
    val currentProgress: Int,
    val todaySteps: Int,
    val availableSteps: Int
)

data class PostTreeRequest(
    val totalTrees: Int,
    val currentProgress: Int,
    val usedSteps: Int
)