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
import iss.nus.edu.sg.sharedprefs.admobile.data.model.MainPageResponseDto
import iss.nus.edu.sg.sharedprefs.admobile.data.repository.AuthRepository
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {

    // 🌟 引入 Repository 处理 API 调用
    private val authRepository by lazy { AuthRepository(this) }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        setupClickListeners()

        // 🌟 核心：页面启动时加载数据
        loadDashboardData()

        // 🌟 统一导航栏逻辑
        NavigationUtils.setupBottomNavigation(this, R.id.nav_home)
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

            // 🌟 添加调试日志
            android.util.Log.d("ECO_DEBUG", "Starting API call with token: Bearer ${token.take(10)}...")

            val result = authRepository.getMainPageData(token)

            result.onSuccess { data ->
                // 🌟 打印获取到的真实数据
                android.util.Log.d("ECO_DEBUG", "API Success! Total: ${data.total}, Food: ${data.food}")

                // 🌟 确保在主线程更新 UI
                runOnUiThread {
                    updateDashboardUI(data)
                }
            }.onFailure { e ->
                android.util.Log.e("ECO_DEBUG", "API Error: ${e.message}")
                runOnUiThread {
                    Toast.makeText(this@MainActivity, "Update failed: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun updateDashboardUI(data: MainPageResponseDto) {
        // 更新总排放量大文字 (需确保 XML 中对应的 ID 已添加)
        findViewById<TextView>(R.id.tv_total_emissions).text = String.format("%.2f kg", data.total)

        // 更新分类小卡片数据
        findViewById<TextView>(R.id.tv_food_value).text = String.format("%.2f kg", data.food)
        findViewById<TextView>(R.id.tv_travel_value).text = String.format("%.2f kg", data.transport)
        findViewById<TextView>(R.id.tv_utility_value).text = String.format("%.2f kg", data.utility)

        // 更新进度条 (假设月度目标为 5.0 kg)
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

    // 🌟 建议：当从添加页面返回时，自动刷新数据
    override fun onResume() {
        super.onResume()
        loadDashboardData()
    }
}