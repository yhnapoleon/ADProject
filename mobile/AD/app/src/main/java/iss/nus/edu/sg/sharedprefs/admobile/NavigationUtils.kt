package iss.nus.edu.sg.sharedprefs.admobile

import android.app.Activity
import android.content.Intent
import android.graphics.Color
import android.view.View
import android.widget.ImageView

object NavigationUtils {
    fun setupBottomNavigation(activity: Activity, currentItemId: Int) {
        // 绑定 ID (必须与 XML 中一致)
        val navHome = activity.findViewById<ImageView>(R.id.nav_home)
        val navRank = activity.findViewById<ImageView>(R.id.nav_rank)
        val navChat = activity.findViewById<ImageView>(R.id.nav_chat)
        val navPerson = activity.findViewById<ImageView>(R.id.nav_person)

        // 定义颜色
        val activeColor = Color.parseColor("#674fa3")
        val inactiveColor = Color.parseColor("#757575")

        // 1. 设置当前页面的图标高亮
        navHome?.setColorFilter(if (currentItemId == R.id.nav_home) activeColor else inactiveColor)
        navRank?.setColorFilter(if (currentItemId == R.id.nav_rank) activeColor else inactiveColor)
        navChat?.setColorFilter(if (currentItemId == R.id.nav_chat) activeColor else inactiveColor)
        navPerson?.setColorFilter(if (currentItemId == R.id.nav_person) activeColor else inactiveColor)

        // 2. 统一跳转逻辑
        val clickListener = View.OnClickListener { v ->
            if (v.id == currentItemId) return@OnClickListener // 点击当前页不跳转

            val intent = when (v.id) {
                R.id.nav_home -> Intent(activity, MainActivity::class.java)
                R.id.nav_rank -> Intent(activity, LeaderboardActivity::class.java)
                R.id.nav_chat -> Intent(activity, AiAssistantActivity::class.java)
                R.id.nav_person -> Intent(activity, ProfileActivity::class.java)
                else -> null
            }

            intent?.let {
                activity.startActivity(it)
                activity.overridePendingTransition(0, 0) // 无缝切换
                activity.finish() // 保持任务栈简洁
            }
        }

        navHome?.setOnClickListener(clickListener)
        navRank?.setOnClickListener(clickListener)
        navChat?.setOnClickListener(clickListener)
        navPerson?.setOnClickListener(clickListener)
    }
}