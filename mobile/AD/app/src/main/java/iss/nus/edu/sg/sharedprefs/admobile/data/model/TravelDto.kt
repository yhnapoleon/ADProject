package iss.nus.edu.sg.sharedprefs.admobile.data.model

// 1. 发送给后端的请求对象
data class AddTravelRequest(
    val originAddress: String,
    val destinationAddress: String,
    val transportMode: Int,
    val notes: String?
)

// 2. 后端返回的结果对象
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

data class TravelHistoryResponse(
    val items: List<TravelHistoryItem>,
    val totalCount: Int
)

data class TravelHistoryItem(
    val id: Int,
    val createdAt: String,
    val transportModeName: String,
    val originAddress: String,
    val destinationAddress: String,
    val carbonEmission: Double,
    val passengerCount: Int,
    val notes: String?
)