package iss.nus.edu.sg.sharedprefs.admobile.data.network

import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object NetworkClient {
    // 🌟 确保 10.0.2.2 指向你的宿主机，端口 5133 与 .NET 项目一致
    private const val BASE_URL = "http://10.0.2.2:5133/"
    //private const val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net/"

    // 1. 创建日志拦截器
    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        // Level.BODY 会打印：请求行、请求头、请求体、响应行、响应头、响应体
        // 这对调试 Multipart 上传至关重要
        level = HttpLoggingInterceptor.Level.BODY
    }

    // 2. 创建自定义的 OkHttpClient
    private val okHttpClient = OkHttpClient.Builder()
        // 🌟 注入日志拦截器
        .addInterceptor(loggingInterceptor)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()

    val apiService: EcoLensApiService by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(EcoLensApiService::class.java)
    }
}