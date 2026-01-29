package iss.nus.edu.sg.sharedprefs.admobile.data.model

// 1. 发送给后端的请求对象 (对应 Swagger Request Body)
data class AddTravelRequest(
    val originAddress: String,      // 起点地址文字
    val destinationAddress: String, // 终点地址文字
    val transportMode: Int,         // 交通工具 ID (0, 1, 2...)
    val notes: String?              // 备注
)

// 2. 后端返回的结果对象 (对应 Swagger 200 Response)
data class TravelResponse(
    val id: Int,
    val transportModeName: String,
    val distanceKilometers: Double,
    val carbonEmission: Double,
    val routePolyline: String,
    val notes: String?
)

data class TransportMode(
    val id: Int,
    val name: String,
    val animationResId: Int,
    val emissionFactor: Double
)