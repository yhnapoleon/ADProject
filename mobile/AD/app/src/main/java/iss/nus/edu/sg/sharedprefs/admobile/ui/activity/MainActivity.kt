package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.content.Intent
import android.os.Bundle
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
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.data.repository.AuthRepository
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {

    // 🌟 引入 Repository 处理看板 API 调用
    private val authRepository by lazy { AuthRepository(this) }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        setupClickListeners()

        // 🌟 页面启动时加载看板数据和今日排行数据
        loadDashboardData()
        loadRankingData()

        // 🌟 统一导航栏逻辑
        NavigationUtils.setupBottomNavigation(this, R.id.nav_home)
    }

    /**
     * 加载看板数据（总排放量、各分类数值等）
     */
    private fun loadDashboardData() {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = prefs.getString("access_token", null)

            if (token == null) {
                startActivity(Intent(this@MainActivity, LoginActivity::class.java))
                finish()
                return@launch
            }

            android.util.Log.d("ECO_DEBUG", "Starting Dashboard API call...")

            val result = authRepository.getMainPageData(token)

            result.onSuccess { data ->
                android.util.Log.d("ECO_DEBUG", "Dashboard Success! Total: ${data.total}")
                updateDashboardUI(data)
            }.onFailure { e ->
                android.util.Log.e("ECO_DEBUG", "Dashboard Error: ${e.message}")
                Toast.makeText(this@MainActivity, "Update failed: ${e.message}", Toast.LENGTH_SHORT).show()
            }
        }
    }

    /**
     * 🌟 新增：加载今日排行榜前三名数据
     */
    private fun loadRankingData() {
        lifecycleScope.launch {
            try {
                // 调用之前实现的获取今日排行接口，limit 设为 3
                val response = NetworkClient.apiService.getTodayLeaderboard(3)
                if (response.isSuccessful && response.body() != null) {
                    val list = response.body()!!
                    android.util.Log.d("ECO_DEBUG", "Ranking API Success! Found ${list.size} users")
                    updateRankingUI(list)
                }
            } catch (e: Exception) {
                android.util.Log.e("ECO_DEBUG", "Ranking API Error: ${e.message}")
            }
        }
    }

    /**
     * 🌟 新增：更新首页排行榜部分的 UI
     */
    private fun updateRankingUI(list: List<LeaderboardItem>) {
        // 更新第一名
        list.getOrNull(0)?.let { item ->
            findViewById<TextView>(R.id.tv_rank1_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank1_value).text = String.format("%.1f kg", item.emissionsTotal)
        }

        // 更新第二名
        list.getOrNull(1)?.let { item ->
            findViewById<TextView>(R.id.tv_rank2_name).text = item.nickname ?: item.username
            findViewById<TextView>(R.id.tv_rank2_value).text = String.format("%.1f kg", item.emissionsTotal)
        }

        // 更新第三名
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
        // 🌟 当从其他页面返回时，同时刷新看板和排行榜
        loadDashboardData()
        loadRankingData()
    }
}