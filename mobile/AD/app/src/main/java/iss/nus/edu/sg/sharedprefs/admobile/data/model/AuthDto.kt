package iss.nus.edu.sg.sharedprefs.admobile.data.model

import com.google.gson.annotations.SerializedName

// 1. 注册请求 (根据你的要求新增了必填项)
data class RegisterRequestDto(
    val username: String,
    val email: String,
    val password: String,
    val birthDate: String,
    @SerializedName("Region") // 🌟 告诉 Retrofit：发请求时 JSON 字段叫 "Region"
    val region: String        // 🌟 告诉 Kotlin：代码里这个变量叫 region
)

// 2. 登录请求
data class LoginRequestDto(
    val email: String,
    val password: String
)

// 3. 用户详细信息 (从 Swagger 响应提取)
data class UserSummaryDto(
    val id: String,
    val username: String,
    val nickname: String?,
    val email: String,
    val location: String?,
    val birthDate: String?,
    val pointsTotal: Int,
    val totalCarbonSaved: Double,
    val avatarUrl: String?
)

// 4. 统一的身份验证响应
data class AuthResponseDto(
    val token: String,
    val accessToken: String,
    val user: UserSummaryDto
)

// 获取个人资料的响应
data class UserProfileResponse(
    val id: String,
    val name: String,
    val nickname: String?,
    val email: String?,
    val location: String?,
    val birthDate: String?,
    val avatar: String?,
    val joinDays: Int,
    val pointsTotal: Int
)

// 更新个人资料的请求
data class UpdateProfileRequest(
    val nickname: String?,
    val avatar: String?,
    val location: String?,
    val email: String?,
    val birthDate: String?
)

data class ChangePasswordRequest(
    val oldPassword: String,
    val newPassword: String
)