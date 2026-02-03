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