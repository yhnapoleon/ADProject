package iss.nus.edu.sg.sharedprefs.admobile.utils

import android.content.Context
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File
import java.math.BigDecimal
import java.util.concurrent.TimeUnit

object ApiHelper {
    private const val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net"
    private const val PREFS_NAME = "EcoLensPrefs"
    private const val KEY_TOKEN = "token"

    fun getToken(context: Context): String? {
        val prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        return prefs.getString(KEY_TOKEN, null)
    }

    private fun defaultClient(timeoutSeconds: Long = 30): OkHttpClient {
        return OkHttpClient.Builder()
            .connectTimeout(timeoutSeconds, TimeUnit.SECONDS)
            .readTimeout(timeoutSeconds, TimeUnit.SECONDS)
            .writeTimeout(timeoutSeconds, TimeUnit.SECONDS)
            .build()
    }

    fun executeGet(context: Context, path: String): Response {
        val token = getToken(context) ?: ""
        val url = if (path.startsWith("http")) path else "$BASE_URL$path"
        val request = Request.Builder()
            .url(url)
            .addHeader("Authorization", "Bearer $token")
            .get()
            .build()
        return defaultClient().newCall(request).execute()
    }

    fun executeUploadFile(
        context: Context,
        path: String,
        file: File,
        partName: String,
        useLongTimeout: Boolean = false
    ): Response {
        val token = getToken(context) ?: ""
        val url = if (path.startsWith("http")) path else "$BASE_URL$path"
        val client = defaultClient(if (useLongTimeout) 120 else 30)
        val body = MultipartBody.Builder()
            .setType(MultipartBody.FORM)
            .addFormDataPart(partName, file.name, file.asRequestBody("image/*".toMediaType()))
            .build()
        val request = Request.Builder()
            .url(url)
            .addHeader("Authorization", "Bearer $token")
            .post(body)
            .build()
        return client.newCall(request).execute()
    }

    fun executeUploadActivity(
        context: Context,
        path: String,
        imageFile: File,
        foodName: String,
        amount: BigDecimal,
        unit: String
    ): Response {
        val token = getToken(context) ?: ""
        val url = if (path.startsWith("http")) path else "$BASE_URL$path"
        val body = MultipartBody.Builder()
            .setType(MultipartBody.FORM)
            .addFormDataPart("file", imageFile.name, imageFile.asRequestBody("image/*".toMediaType()))
            .addFormDataPart("activityType", "Food")
            .addFormDataPart("foodName", foodName)
            .addFormDataPart("amount", amount.toPlainString())
            .addFormDataPart("unit", unit)
            .build()
        val request = Request.Builder()
            .url(url)
            .addHeader("Authorization", "Bearer $token")
            .post(body)
            .build()
        return defaultClient().newCall(request).execute()
    }
}
