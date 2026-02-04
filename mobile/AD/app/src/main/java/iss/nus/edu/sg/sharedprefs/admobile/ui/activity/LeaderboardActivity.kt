package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.graphics.Color
import android.graphics.Typeface
import android.os.Bundle
import android.view.View
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.bumptech.glide.load.engine.DiskCacheStrategy
import com.bumptech.glide.request.RequestOptions
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LeaderboardItem
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.ui.adapter.LeaderboardAdapter
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.launch

class LeaderboardActivity : AppCompatActivity() {

    private lateinit var tvDaily: TextView
    private lateinit var tvMonthly: TextView
    private lateinit var tvAllTime: TextView
    private lateinit var adapter: LeaderboardAdapter

    private val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net"

    private enum class RankingType { DAILY, MONTHLY, TOTAL }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_leaderboard)

        window.statusBarColor = Color.parseColor("#674fa3")

        tvDaily = findViewById(R.id.tv_daily)
        tvMonthly = findViewById(R.id.tv_monthly)
        tvAllTime = findViewById(R.id.tv_all_time)

        val recyclerView: RecyclerView = findViewById(R.id.rv_ranking_list)
        recyclerView.layoutManager = LinearLayoutManager(this)
        adapter = LeaderboardAdapter(emptyList())
        recyclerView.adapter = adapter

        tvDaily.setOnClickListener {
            updateTabSelection(RankingType.DAILY)
            fetchData(RankingType.DAILY)
        }

        tvMonthly.setOnClickListener {
            updateTabSelection(RankingType.MONTHLY)
            fetchData(RankingType.MONTHLY)
        }

        tvAllTime.setOnClickListener {
            updateTabSelection(RankingType.TOTAL)
            fetchData(RankingType.TOTAL)
        }

        setupPodiumIcons()
        fetchData(RankingType.DAILY)
        NavigationUtils.setupBottomNavigation(this, R.id.nav_rank)
    }

    private fun fetchData(type: RankingType) {
        lifecycleScope.launch {
            try {
                // 🌟 获取数据前先清理 Glide 内存缓存，防止列表复用旧位图
                Glide.get(this@LeaderboardActivity).clearMemory()

                val response = when (type) {
                    RankingType.DAILY -> NetworkClient.apiService.getTodayLeaderboard(50)
                    RankingType.MONTHLY -> NetworkClient.apiService.getMonthLeaderboard(50)
                    RankingType.TOTAL -> NetworkClient.apiService.getAllTimeLeaderboard(50)
                }

                if (response.isSuccessful && response.body() != null) {
                    val list = response.body()!!
                    updateUI(list)
                } else {
                    Toast.makeText(this@LeaderboardActivity, "Failed to load", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                android.util.Log.e("RANK_DEBUG", "Error: ${e.message}")
                Toast.makeText(this@LeaderboardActivity, "Network Error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun updateUI(list: List<LeaderboardItem>) {
        val top3 = list.take(3)
        updatePodium(top3)
        val others = if (list.size > 3) list.subList(3, list.size) else emptyList()
        adapter.updateData(others)
    }

    private fun updatePodium(top3: List<LeaderboardItem>) {
        setPodiumData(findViewById(R.id.rank1), top3.getOrNull(0))
        setPodiumData(findViewById(R.id.rank2), top3.getOrNull(1))
        setPodiumData(findViewById(R.id.rank3), top3.getOrNull(2))
    }

    private fun setPodiumData(view: View, data: LeaderboardItem?) {
        val tvName = view.findViewById<TextView>(R.id.tv_name)
        val tvValue = view.findViewById<TextView>(R.id.tv_value)
        val ivAvatar = view.findViewById<ImageView>(R.id.iv_avatar)

        if (data != null) {
            tvName.text = data.nickname ?: data.username
            tvValue.text = "${String.format("%.1f", data.emissionsTotal)} kg"

            // 🌟 核心：拼接 URL，并处理转义字符
            var avatarPath = data.avatarUrl ?: ""
            val fullAvatarUrl = if (avatarPath.isNotEmpty()) {
                if (avatarPath.startsWith("http")) avatarPath
                else "$BASE_URL${avatarPath.replace("\\", "/")}"
            } else null

            // 🌟 核心：使用 .skipMemoryCache(true) 强制刷新
            Glide.with(this)
                .load(fullAvatarUrl)
                .apply(RequestOptions.circleCropTransform())
                .skipMemoryCache(true) // 🌟 跳过内存缓存
                .diskCacheStrategy(DiskCacheStrategy.NONE) // 🌟 不使用磁盘缓存
                .placeholder(R.drawable.ic_avatar_placeholder)
                .error(R.drawable.ic_avatar_placeholder)
                .into(ivAvatar)
        } else {
            // ... 处理 data == null 的逻辑 ...
            tvName.text = "-"
            tvValue.text = "0 kg"
            ivAvatar.setImageResource(R.drawable.ic_avatar_placeholder)
        }
    }

    private fun updateTabSelection(type: RankingType) {
        val tabs = listOf(tvDaily, tvMonthly, tvAllTime)
        tabs.forEach {
            it.setBackground(null)
            it.setTextColor(Color.WHITE)
            it.setTypeface(null, Typeface.NORMAL)
        }

        val selectedTab = when (type) {
            RankingType.DAILY -> tvDaily
            RankingType.MONTHLY -> tvMonthly
            RankingType.TOTAL -> tvAllTime
        }
        selectedTab.setBackgroundResource(R.drawable.shape_tab_selected)
        selectedTab.setTextColor(Color.parseColor("#674fa3"))
        selectedTab.setTypeface(null, Typeface.BOLD)
    }

    private fun setupPodiumIcons() {
        findViewById<View>(R.id.rank1).findViewById<ImageView>(R.id.iv_crown).setImageResource(R.drawable.winner1)
        findViewById<View>(R.id.rank2).findViewById<ImageView>(R.id.iv_crown).setImageResource(R.drawable.winner2)
        findViewById<View>(R.id.rank3).findViewById<ImageView>(R.id.iv_crown).setImageResource(R.drawable.winner3)
    }
}