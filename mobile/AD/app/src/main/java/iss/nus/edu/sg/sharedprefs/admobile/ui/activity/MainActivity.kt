package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Bundle
import android.util.Log
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.android.material.button.MaterialButton
import com.google.android.material.card.MaterialCardView
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LeaderboardItem
import iss.nus.edu.sg.sharedprefs.admobile.data.model.MainPageResponseDto
import iss.nus.edu.sg.sharedprefs.admobile.data.model.StepSyncRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.data.repository.AuthRepository
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import iss.nus.edu.sg.sharedprefs.admobile.utils.StepCounterManager
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.util.*

class MainActivity : AppCompatActivity() {

    private val authRepository by lazy { AuthRepository(this) }
    private lateinit var stepCounterManager: StepCounterManager
    private val TAG = "ECO_DEBUG"

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        stepCounterManager = StepCounterManager(this)

        setupClickListeners()

        // 加载所有数据
        loadDashboardData()
        loadRankingData()
        fetchStepDataFromBackend() // 从后端获取步数并更新 UI

        checkStepPermissionAndStart()

        NavigationUtils.setupBottomNavigation(this, R.id.nav_home)
    }

    /**
     * 小于 10,000 步显示具体数字（带千分位，如 9,277）
     * 大于等于 10,000 步显示为 "w"（如 1.2w）
     */
    private fun formatStepCount(steps: Int): String {
        return if (steps >= 10000) {
            val wan = steps / 10000.0
            String.format("%.1fw", wan)
        } else {
            NumberFormat.getInstance(Locale.US).format(steps)
        }
    }

    /**
     * 从 /api/getTree 获取今日总步数并更新主页 UI
     */
    private fun fetchStepDataFromBackend() {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = prefs.getString("access_token", "") ?: ""
                if (token.isEmpty()) return@launch

                val authHeader = "Bearer $token"
                val response = NetworkClient.apiService.getTreeData(authHeader)

                if (response.isSuccessful && response.body() != null) {
                    val data = response.body()!!
                    Log.d(TAG, "MainPage Step Sync: Today Total = ${data.todaySteps}")

                    // 使用格式化函数更新 UI
                    val formattedSteps = formatStepCount(data.todaySteps)
                    findViewById<TextView>(R.id.steps_number_text)?.text = formattedSteps
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error fetching step data: ${e.message}")
            }
        }
    }

    private fun checkStepPermissionAndStart() {
        if (checkSelfPermission(Manifest.permission.ACTIVITY_RECOGNITION) == PackageManager.PERMISSION_GRANTED) {
            startStepCounting()
        } else {
            requestPermissions(arrayOf(Manifest.permission.ACTIVITY_RECOGNITION), 100)
        }
    }

    private fun startStepCounting() {
        stepCounterManager.startListening { todaySteps ->
            Log.d(TAG, "Local Step Counter: $todaySteps")
            uploadSteps(todaySteps)
        }
    }

    private fun uploadSteps(steps: Int) {
        if (steps <= 0) return
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = prefs.getString("access_token", "") ?: ""
                val authHeader = "Bearer $token"

                val sdf = java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", java.util.Locale.US)
                sdf.timeZone = java.util.TimeZone.getTimeZone("UTC")
                val currentDate = sdf.format(java.util.Date())

                val request = StepSyncRequest(stepCount = steps, date = currentDate)
                val response = NetworkClient.apiService.syncSteps(authHeader, request)

                if (response.isSuccessful) {
                    // 上传成功后立即重新拉取后端数据，保证 UI 同步
                    fetchStepDataFromBackend()
                }
            } catch (e: Exception) {
                Log.e(TAG, "Step upload error: ${e.message}")
            }
        }
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == 100 && grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
            startStepCounting()
        }
    }

    private fun loadDashboardData() {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = prefs.getString("access_token", null) ?: return@launch

            val result = authRepository.getMainPageData(token)
            result.onSuccess { data ->
                updateDashboardUI(data)
            }.onFailure { e ->
                Log.e(TAG, "Dashboard Error: ${e.message}")
            }
        }
    }

    private fun loadRankingData() {
        lifecycleScope.launch {
            try {
                val response = NetworkClient.apiService.getTodayLeaderboard(3)
                if (response.isSuccessful && response.body() != null) {
                    updateRankingUI(response.body()!!)
                }
            } catch (e: Exception) {
                Log.e(TAG, "Ranking API Error: ${e.message}")
            }
        }
    }

    private fun updateRankingUI(list: List<LeaderboardItem>) {
        // 格式化显示 排放量 | 积分
        fun formatRankValue(item: LeaderboardItem): String {
            return String.format("%.1f kg | %d pts", item.emissionsTotal, item.pointsToday)
        }

        // 第 1 名
        list.getOrNull(0)?.let { item ->
            findViewById<TextView>(R.id.tv_rank1_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank1_value).text = formatRankValue(item)
        }

        // 第 2 名
        list.getOrNull(1)?.let { item ->
            findViewById<TextView>(R.id.tv_rank2_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank2_value).text = formatRankValue(item)
        }

        // 第 3 名
        list.getOrNull(2)?.let { item ->
            findViewById<TextView>(R.id.tv_rank3_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank3_value).text = formatRankValue(item)
        }
    }

    private fun updateDashboardUI(data: MainPageResponseDto) {
        findViewById<TextView>(R.id.tv_total_emissions).text = String.format("%.2f kg", data.total)
        findViewById<TextView>(R.id.tv_food_value).text = String.format("%.2f kg", data.food)
        findViewById<TextView>(R.id.tv_travel_value).text = String.format("%.2f kg", data.transport)
        findViewById<TextView>(R.id.tv_utility_value).text = String.format("%.2f kg", data.utility)

        val target = 200.0
        val progressPercent = ((data.total / target) * 100).toInt()
        findViewById<ProgressBar>(R.id.carbon_progress).progress = progressPercent.coerceAtMost(100)
    }

    private fun setupClickListeners() {
        findViewById<MaterialCardView>(R.id.card_monthly_emissions).setOnClickListener {
            startActivity(Intent(this, ProfileStatsActivity::class.java))
        }
        findViewById<MaterialCardView>(R.id.food_card_entry).setOnClickListener {
            startActivity(Intent(this, AddFoodActivity::class.java))
        }
        findViewById<MaterialCardView>(R.id.travel_card_entry).setOnClickListener {
            startActivity(Intent(this, AddTravelActivity::class.java))
        }
        findViewById<MaterialCardView>(R.id.utility_card_entry).setOnClickListener {
            startActivity(Intent(this, AddUtilityActivity::class.java))
        }
        findViewById<TextView>(R.id.tv_view_all_leaderboard).setOnClickListener {
            startActivity(Intent(this, LeaderboardActivity::class.java))
        }
        findViewById<MaterialCardView>(R.id.steps_card_view).setOnClickListener {
            startActivity(Intent(this, TreePlantingActivity::class.java))
            overridePendingTransition(android.R.anim.fade_in, android.R.anim.fade_out)
        }
        findViewById<MaterialButton>(R.id.tips_button).setOnClickListener {
            startActivity(Intent(this, AiAssistantActivity::class.java))
        }
    }

    override fun onResume() {
        super.onResume()
        loadDashboardData()
        loadRankingData()
        fetchStepDataFromBackend() // 返回页面时强制刷新步数
        checkStepPermissionAndStart()
    }
}