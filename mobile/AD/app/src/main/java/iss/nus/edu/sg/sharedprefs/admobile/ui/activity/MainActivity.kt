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
import iss.nus.edu.sg.sharedprefs.admobile.utils.StepCounterManager // 🌟 导入你的 Manager
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {

    private val authRepository by lazy { AuthRepository(this) }

    // 🌟 引入步数管理器
    private lateinit var stepCounterManager: StepCounterManager
    private val TAG = "ECO_DEBUG"

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        stepCounterManager = StepCounterManager(this)

        setupClickListeners()

        // 🌟 页面启动时加载看板、排行榜、步数
        loadDashboardData()
        loadRankingData()
        checkStepPermissionAndStart()

        NavigationUtils.setupBottomNavigation(this, R.id.nav_home)
    }

    /**
     * 🌟 新增：检查步数权限并开始监听
     */
    private fun checkStepPermissionAndStart() {
        if (checkSelfPermission(Manifest.permission.ACTIVITY_RECOGNITION) == PackageManager.PERMISSION_GRANTED) {
            startStepCounting()
        } else {
            // 请求身体活动权限 (API 29+)
            requestPermissions(arrayOf(Manifest.permission.ACTIVITY_RECOGNITION), 100)
        }
    }

    /**
     * 🌟 新增：启动步数监听并处理上传
     */
    private fun startStepCounting() {
        stepCounterManager.startListening { todaySteps ->
            Log.d(TAG, "Step Counter Triggered! Today's steps: $todaySteps")

            // 将步数实时反映在 UI 上（如果有对应的 TextView）
            // findViewById<TextView>(R.id.tv_today_steps)?.text = todaySteps.toString()

            // 异步上传步数到后端
            uploadSteps(todaySteps)
        }
    }

    /**
     * 🌟 新增：上传步数到服务器
     */
    private fun uploadSteps(steps: Int) {
        if (steps <= 0) return

        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = prefs.getString("access_token", "") ?: ""
                if (token.isEmpty()) return@launch

                val authHeader = "Bearer $token"

                // 🌟 格式化时间为后端要求的 ISO 8601 格式
                val sdf = java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", java.util.Locale.US)
                sdf.timeZone = java.util.TimeZone.getTimeZone("UTC")
                val currentDate = sdf.format(java.util.Date())

                val request = StepSyncRequest(
                    stepCount = steps,
                    date = currentDate
                )

                Log.d(TAG, "Syncing steps to backend: $steps at $currentDate")
                val response = NetworkClient.apiService.syncSteps(authHeader, request)

                if (response.isSuccessful && response.body() != null) {
                    val data = response.body()!!
                    Log.i(TAG, "Step Sync Success! Total: ${data.totalSteps}, Available: ${data.availableSteps}")

                    // 🌟 可选：将可用步数更新到首页的 UI 上（例如步数卡片）
                    // updateStepUI(data.availableSteps)
                } else {
                    Log.e(TAG, "Step Sync Failed: ${response.code()} ${response.errorBody()?.string()}")
                }

            } catch (e: Exception) {
                Log.e(TAG, "Network error during step sync: ${e.message}")
            }
        }
    }

    // 权限申请回调
    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == 100 && grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
            startStepCounting()
        }
    }

    private fun loadDashboardData() {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = prefs.getString("access_token", null)

            if (token == null) {
                startActivity(Intent(this@MainActivity, LoginActivity::class.java))
                finish()
                return@launch
            }

            Log.d(TAG, "Starting Dashboard API call...")
            val result = authRepository.getMainPageData(token)

            result.onSuccess { data ->
                Log.d(TAG, "Dashboard Success! Total: ${data.total}")
                updateDashboardUI(data)
            }.onFailure { e ->
                Log.e(TAG, "Dashboard Error: ${e.message}")
                Toast.makeText(this@MainActivity, "Update failed: ${e.message}", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun loadRankingData() {
        lifecycleScope.launch {
            try {
                val response = NetworkClient.apiService.getTodayLeaderboard(3)
                if (response.isSuccessful && response.body() != null) {
                    val list = response.body()!!
                    Log.d(TAG, "Ranking API Success! Found ${list.size} users")
                    updateRankingUI(list)
                }
            } catch (e: Exception) {
                Log.e(TAG, "Ranking API Error: ${e.message}")
            }
        }
    }

    private fun updateRankingUI(list: List<LeaderboardItem>) {
        list.getOrNull(0)?.let { item ->
            findViewById<TextView>(R.id.tv_rank1_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank1_value).text = String.format("%.1f kg", item.emissionsTotal)
        }
        list.getOrNull(1)?.let { item ->
            findViewById<TextView>(R.id.tv_rank2_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank2_value).text = String.format("%.1f kg", item.emissionsTotal)
        }
        list.getOrNull(2)?.let { item ->
            findViewById<TextView>(R.id.tv_rank3_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank3_value).text = String.format("%.1f kg", item.emissionsTotal)
        }
    }

    private fun updateDashboardUI(data: MainPageResponseDto) {
        findViewById<TextView>(R.id.tv_total_emissions).text = String.format("%.2f kg", data.total)
        findViewById<TextView>(R.id.tv_food_value).text = String.format("%.2f kg", data.food)
        findViewById<TextView>(R.id.tv_travel_value).text = String.format("%.2f kg", data.transport)
        findViewById<TextView>(R.id.tv_utility_value).text = String.format("%.2f kg", data.utility)

        val target = 5.0
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
        // 🌟 返回主页时再次检查并更新步数
        checkStepPermissionAndStart()
    }
}