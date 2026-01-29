package iss.nus.edu.sg.sharedprefs.admobile.data.repository

import android.content.Context
import android.util.Log
import iss.nus.edu.sg.sharedprefs.admobile.data.model.*
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient.apiService

class AuthRepository(context: Context) {
    private val prefs = context.getSharedPreferences("EcoLensPrefs", Context.MODE_PRIVATE)
    private val api = NetworkClient.apiService

    suspend fun login(request: LoginRequestDto): Result<AuthResponseDto> {
        return try {
            val response = api.login(request)
            if (response.isSuccessful && response.body() != null) {
                saveUserData(response.body()!!)
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("Login failed: ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    // AuthRepository.kt 中的建议写法
    suspend fun register(request: RegisterRequestDto): Result<Unit> {
        return try {
            val response = apiService.register(request) // 假设返回的是 Response<Unit>
            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                // 🌟 关键：提取后端返回的原始 JSON 错误信息
                val errorJson = response.errorBody()?.string() ?: "Unknown server error"
                Log.e("API_DEBUG", "Server Response Error Body: $errorJson")

                // 将 JSON 放入 Exception 传递给 Activity
                Result.failure(Exception(errorJson))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    private fun saveUserData(data: AuthResponseDto) {
        prefs.edit().apply {
            putString("token", data.token)
            putString("userId", data.user.id)
            putString("username", data.user.username)
            // 存下这些，ProfileActivity 就能直接显示了
            apply()
        }
    }

    // AuthRepository.kt 增加以下方法
    suspend fun getMainPageData(token: String): Result<MainPageResponseDto> {
        return try {
            // 注意：这里需要传入 Bearer 前缀
            val response = apiService.getMainPageData("Bearer $token")
            if (response.isSuccessful) {
                Result.success(response.body()!!)
            } else {
                Result.failure(Exception("HTTP ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}