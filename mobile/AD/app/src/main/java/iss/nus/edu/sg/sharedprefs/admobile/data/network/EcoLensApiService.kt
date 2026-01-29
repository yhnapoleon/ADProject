package iss.nus.edu.sg.sharedprefs.admobile.data.network

import iss.nus.edu.sg.sharedprefs.admobile.data.model.AddTravelRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AuthResponseDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LoginRequestDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.MainPageResponseDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.RegisterRequestDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.TravelResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.GET


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
}