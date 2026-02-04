package iss.nus.edu.sg.sharedprefs.admobile.data.network

import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object NetworkClient {
    private const val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net/"

    // 1. 创建自定义的 OkHttpClient
    private val okHttpClient = OkHttpClient.Builder()
        .connectTimeout(30, TimeUnit.SECONDS) // 连接服务器超时：30秒
        .readTimeout(60, TimeUnit.SECONDS)    // 等待服务器返回数据超时（这对AI很重要）：60秒
        .writeTimeout(30, TimeUnit.SECONDS)   // 发送数据给服务器超时：30秒
        .build()

    val apiService: EcoLensApiService by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            // 2. 将自定义的 client 关联到 Retrofit
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(EcoLensApiService::class.java)
    }
}