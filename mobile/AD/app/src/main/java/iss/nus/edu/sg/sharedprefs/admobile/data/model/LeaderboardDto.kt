package iss.nus.edu.sg.sharedprefs.admobile.data.model

enum class RankingType { DAILY, MONTHLY, TOTAL }
data class LeaderboardItem(
    val rank: Int,
    val username: String,
    val nickname: String?,
    val emissionsTotal: Double,
    val avatarUrl: String?,
    val pointsToday: Int = 0,
    val pointsMonth: Int = 0,
    val pointsTotal: Int = 0
)