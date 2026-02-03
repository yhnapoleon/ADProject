package iss.nus.edu.sg.sharedprefs.admobile.data.model

data class LeaderboardItem(
    val rank: Int,
    val username: String,
    val nickname: String?,
    val emissionsTotal: Double,
    val avatarUrl: String?,
    val pointsTotal: Int
)