package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Intent
import android.os.Bundle
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.card.MaterialCardView

class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        findViewById<MaterialCardView>(R.id.card_monthly_emissions).setOnClickListener {
            // 跳转到详细统计页面 (ProfileStatsActivity)
            val intent = Intent(this, ProfileStatsActivity::class.java)
            startActivity(intent)
        }

        // 1. 设置功能卡片点击事件 (饮食、旅行、水电)
        findViewById<MaterialCardView>(R.id.food_card_entry).setOnClickListener {
            startActivity(Intent(this, AddFoodActivity::class.java))
        }

        findViewById<MaterialCardView>(R.id.travel_card_entry).setOnClickListener {
            startActivity(Intent(this, AddTravelActivity::class.java))
        }

        findViewById<MaterialCardView>(R.id.utility_card_entry).setOnClickListener {
            startActivity(Intent(this, AddUtilityActivity::class.java))
        }

        // 2. 设置其他交互 (查看排行榜和种树页面)
        findViewById<TextView>(R.id.tv_view_all_leaderboard).setOnClickListener {
            startActivity(Intent(this, LeaderboardActivity::class.java))
        }

        findViewById<MaterialCardView>(R.id.steps_card_view).setOnClickListener {
            startActivity(Intent(this, TreePlantingActivity::class.java))
            // 保持你原本的淡入淡出动画效果
            overridePendingTransition(android.R.anim.fade_in, android.R.anim.fade_out)
        }

        // 3. 顶部 Header 的 AI 按钮
        findViewById<com.google.android.material.button.MaterialButton>(R.id.tips_button).setOnClickListener {
            startActivity(Intent(this, AiAssistantActivity::class.java))
        }

        // 🌟 4. 核心：统一导航栏逻辑
        // 这一行代码会自动处理 nav_home, nav_rank, nav_chat, nav_person 的点击和颜色过滤
        NavigationUtils.setupBottomNavigation(this, R.id.nav_home)
    }
}