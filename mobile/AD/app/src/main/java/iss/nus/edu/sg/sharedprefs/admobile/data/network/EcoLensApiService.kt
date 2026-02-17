package iss.nus.edu.sg.sharedprefs.admobile.data.network

import iss.nus.edu.sg.sharedprefs.admobile.data.model.AddFoodRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AddFoodResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AddTravelRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AuthResponseDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AvatarUploadResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.BarcodeResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.BatchDeleteTypedResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.CalculateFoodRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.CalculateFoodResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ChangePasswordRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ChatRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ChatResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.FoodAnalyzeResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.FoodHistoryResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LeaderboardItem
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LoginRequestDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.MainPageResponseDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ManualUtilityRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.PostTreeRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.RegisterRequestDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.StepSyncRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.StepSyncResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.TravelHistoryResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.TravelResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.TreeResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.TypedDeleteEntry
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UpdateProfileRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UserProfileResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UserStatsResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UtilityBillResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UtilityHistoryItem
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UtilityHistoryResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.model.VerifyPasswordRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.VerifyPasswordResponse
import okhttp3.MultipartBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.PUT
import retrofit2.http.*


interface EcoLensApiService {
    @POST("api/Auth/register")
    suspend fun register(@Body request: RegisterRequestDto): Response<AuthResponseDto>

    @POST("api/Auth/login")
    suspend fun login(@Body request: LoginRequestDto): Response<AuthResponseDto>
    @POST("api/Travel")
    suspend fun addTravelRecord(
        @Header("Authorization") token: String,
        @Body request: AddTravelRequest
    ): Response<TravelResponse>

    @GET("api/mainpage")
    suspend fun getMainPageData(@Header("Authorization") token: String): Response<MainPageResponseDto>

    @GET("api/user/profile")
    suspend fun getUserProfile(
        @Header("Authorization") token: String
    ): Response<UserProfileResponse>

    @PUT("api/user/profile")
    suspend fun updateUserProfile(
        @Header("Authorization") token: String,
        @Body request: UpdateProfileRequest
    ): Response<UserProfileResponse>

    @Multipart
    @POST("api/UtilityBill/upload")
    suspend fun uploadUtilityBill(
        @Header("Authorization") token: String,
        @Part file: MultipartBody.Part
    ): Response<UtilityBillResponse>

    @POST("api/UtilityBill/manual")
    suspend fun saveUtilityManual(
        @Header("Authorization") token: String,
        @Body request: ManualUtilityRequest
    ): Response<UtilityBillResponse>

    // 在 ApiService.kt 中添加
    @GET("api/Leaderboard/today")
    suspend fun getTodayLeaderboard(@Query("limit") limit: Int): Response<List<LeaderboardItem>>

    // 获取月度排行榜
    @GET("api/Leaderboard/month")
    suspend fun getMonthLeaderboard(@Query("limit") limit: Int): Response<List<LeaderboardItem>>

    // 获取总排行榜 (假设路径为原有的 api/Leaderboard)
    @GET("api/Leaderboard")
    suspend fun getAllTimeLeaderboard(@Query("period") period: String = "total",@Query("limit") limit: Int): Response<List<LeaderboardItem>>

    // ApiService.kt
    @POST("api/ai/chat")
    suspend fun postChatMessage(@Body request: ChatRequest): Response<ChatResponse>

    @GET("api/Travel/my-travels")
    suspend fun getTravelHistory(
        @Header("Authorization") token: String, // 🌟 必须传 Token
        @Query("PageSize") pageSize: Int = 100
    ): Response<TravelHistoryResponse>

    @GET("api/FoodRecords/my-records")
    suspend fun getFoodHistory(
        @Header("Authorization") token: String, // 🌟 必须传 Token
        @Query("PageSize") pageSize: Int = 100
    ): Response<FoodHistoryResponse>

    @GET("api/UtilityBill/my-bills") // 路径确保与 Swagger 一致
    suspend fun getUtilityHistory(
        @Header("Authorization") token: String,
        @Query("PageSize") pageSize: Int = 100
    ): Response<UtilityHistoryResponse>

    @Multipart
    @POST("api/Vision/analyze")
    suspend fun analyzeFoodImage(
        @Header("Authorization") token: String, // 🌟 补全 Token 参数
        @Part file: MultipartBody.Part
    ): Response<FoodAnalyzeResponse>

    @POST("api/calculateFood")
    suspend fun calculateFoodEmission(
        @Header("Authorization") token: String, // 🌟 补全 Token 参数
        @Body request: CalculateFoodRequest
    ): Response<CalculateFoodResponse>

    @GET("api/Barcode/{barcode}")
    suspend fun getProductByBarcode(
        @Header("Authorization") token: String,
        @Path("barcode") barcode: String,
        @Query("refresh") refresh: Boolean = false,
        @Query("useDefault") useDefault: Boolean = false
    ): Response<BarcodeResponse>

    @POST("api/addFood")
    suspend fun addFoodRecord(
        @Header("Authorization") token: String,
        @Body request: AddFoodRequest
    ): Response<AddFoodResponse>

    @POST("api/user/verify-password")
    suspend fun verifyPassword(
        @Header("Authorization") token: String,
        @Body request: VerifyPasswordRequest
    ): Response<VerifyPasswordResponse>

    @POST("api/user/change-password")
    suspend fun changePassword(
        @Header("Authorization") token: String,
        @Body request: ChangePasswordRequest
    ): Response<Unit>

    @Multipart
    @PUT("api/user/avatar")
    suspend fun uploadAvatar(
        @Header("Authorization") token: String,
        @Part file: MultipartBody.Part
    ): Response<AvatarUploadResponse> // 🌟 这里改为对应的返回结构

    @GET("api/about-me")
    suspend fun getAboutMe(@Header("Authorization") token: String): Response<List<UserStatsResponse>>

    // 删除交通记录
    @DELETE("api/Travel/{id}")
    suspend fun deleteTravel(
        @Header("Authorization") token: String,
        @Path("id") id: Int
    ): Response<Unit>

    // 删除食物记录
    @DELETE("api/FoodRecords/{id}")
    suspend fun deleteFood(
        @Header("Authorization") token: String,
        @Path("id") id: Int
    ): Response<Unit>

    // 删除账单记录
    @DELETE("api/UtilityBill/{id}")
    suspend fun deleteUtility(
        @Header("Authorization") token: String,
        @Path("id") id: Int
    ): Response<Unit>

    @POST("/api/carbon-emission/batch-delete-typed")
    suspend fun batchDeleteTypedRecords(
        @Header("Authorization") token: String,
        @Body request: List<TypedDeleteEntry> // 注意：这里直接传 List
    ): Response<BatchDeleteTypedResponse>

    @POST("api/Step/sync")
    suspend fun syncSteps(
        @Header("Authorization") token: String,
        @Body request: StepSyncRequest
    ): Response<StepSyncResponse>

    @GET("api/getTree")
    suspend fun getTreeData(
        @Header("Authorization") token: String
    ): Response<TreeResponse>

    @POST("api/postTree")
    suspend fun postTreeData(
        @Header("Authorization") token: String,
        @Body request: PostTreeRequest
    ): Response<Unit>
}