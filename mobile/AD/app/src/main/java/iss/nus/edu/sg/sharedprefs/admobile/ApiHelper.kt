package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Context
import android.content.SharedPreferences
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File
import java.util.concurrent.TimeUnit

object ApiHelper {
    private const val PREFS_NAME = "EcoLensPrefs"
    private const val KEY_TOKEN = "auth_token"
    
    private val client = OkHttpClient.Builder()
        .connectTimeout(10, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .build()

    /**
     * 保存 Token 到 SharedPreferences
     */
    fun saveToken(context: Context, token: String) {
        val prefs: SharedPreferences = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        prefs.edit().putString(KEY_TOKEN, token).apply()
    }

    /**
     * 获取保存的 Token
     */
    fun getToken(context: Context): String? {
        val prefs: SharedPreferences = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        return prefs.getString(KEY_TOKEN, null)
    }

    /**
     * 清除 Token（登出时使用）
     */
    fun clearToken(context: Context) {
        val prefs: SharedPreferences = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        prefs.edit().remove(KEY_TOKEN).apply()
    }

    /**
     * 获取 API Base URL
     */
    fun getApiBaseUrl(context: Context): String {
        return context.resources.getString(R.string.api_base_url).trimEnd('/')
    }

    /**
     * 构建完整的头像 URL
     * 如果 avatarUrl 是相对路径（以 / 开头），则拼接 Base URL
     * 如果已经是完整 URL（以 http:// 或 https:// 开头），则直接返回
     */
    fun buildAvatarUrl(context: Context, avatarUrl: String?): String? {
        if (avatarUrl.isNullOrEmpty()) return null
        return if (avatarUrl.startsWith("http://") || avatarUrl.startsWith("https://")) {
            avatarUrl
        } else {
            "${getApiBaseUrl(context)}${if (avatarUrl.startsWith("/")) avatarUrl else "/$avatarUrl"}"
        }
    }

    /**
     * 创建带认证头的请求 Builder
     */
    fun createAuthenticatedRequest(context: Context, url: String): Request.Builder {
        val builder = Request.Builder().url(url)
        getToken(context)?.let { token ->
            builder.addHeader("Authorization", "Bearer $token")
        }
        return builder
    }

    /**
     * 执行 GET 请求
     */
    fun executeGet(context: Context, endpoint: String): okhttp3.Response {
        val url = "${getApiBaseUrl(context)}$endpoint"
        val request = createAuthenticatedRequest(context, url).get().build()
        return client.newCall(request).execute()
    }

    /**
     * 执行 POST 请求
     */
    fun executePost(context: Context, endpoint: String, jsonBody: String): okhttp3.Response {
        val url = "${getApiBaseUrl(context)}$endpoint"
        val requestBody = jsonBody.toRequestBody("application/json".toMediaType())
        val request = createAuthenticatedRequest(context, url)
            .post(requestBody)
            .build()
        return client.newCall(request).execute()
    }

    /**
     * 执行 PUT 请求
     */
    fun executePut(context: Context, endpoint: String, jsonBody: String): okhttp3.Response {
        val url = "${getApiBaseUrl(context)}$endpoint"
        val requestBody = jsonBody.toRequestBody("application/json".toMediaType())
        val request = createAuthenticatedRequest(context, url)
            .put(requestBody)
            .build()
        return client.newCall(request).execute()
    }

    /**
     * 执行 DELETE 请求
     */
    fun executeDelete(context: Context, endpoint: String): okhttp3.Response {
        val url = "${getApiBaseUrl(context)}$endpoint"
        val request = createAuthenticatedRequest(context, url).delete().build()
        return client.newCall(request).execute()
    }

    /**
     * 上传文件（用于头像上传）
     */
    fun executeUploadFile(context: Context, endpoint: String, file: File, fieldName: String = "file"): okhttp3.Response {
        val url = "${getApiBaseUrl(context)}$endpoint"
        val mediaType = "image/*".toMediaType()
        val fileRequestBody = file.asRequestBody(mediaType)
        
        // 创建 multipart/form-data 请求体
        val requestBody = MultipartBody.Builder()
            .setType(MultipartBody.FORM)
            .addFormDataPart(fieldName, file.name, fileRequestBody)
            .build()
        
        val requestBuilder = createAuthenticatedRequest(context, url)
        val request = requestBuilder
            .put(requestBody)
            .build()
        
        return client.newCall(request).execute()
    }

    /**
     * 上传活动记录（包含图片和JSON字段）
     */
    fun executeUploadActivity(
        context: Context,
        endpoint: String,
        imageFile: File,
        label: String,
        quantity: java.math.BigDecimal,
        unit: String
    ): okhttp3.Response {
        val url = "${getApiBaseUrl(context)}$endpoint"
        val mediaType = "image/*".toMediaType()
        val fileRequestBody = imageFile.asRequestBody(mediaType)
        
        // 创建 multipart/form-data 请求体，包含图片和JSON字段
        val requestBody = MultipartBody.Builder()
            .setType(MultipartBody.FORM)
            .addFormDataPart("imageFile", imageFile.name, fileRequestBody)
            .addFormDataPart("label", label)
            .addFormDataPart("quantity", quantity.toString())
            .addFormDataPart("unit", unit)
            .addFormDataPart("category", "Food")
            .build()
        
        val requestBuilder = createAuthenticatedRequest(context, url)
        val request = requestBuilder
            .post(requestBody)
            .build()
        
        return client.newCall(request).execute()
    }
}
