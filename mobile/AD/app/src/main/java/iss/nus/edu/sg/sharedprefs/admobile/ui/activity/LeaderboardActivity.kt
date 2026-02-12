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
import iss.nus.edu.sg.sharedprefs.admobile.data.model.RankingType // 🌟 引入共享枚举
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.ui.adapter.LeaderboardAdapter
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.launch

class LeaderboardActivity : AppCompatActivity() {

    private lateinit var tvDaily: TextView
    private lateinit var tvMonthly: TextView
    private lateinit var tvAllTime: TextView
    private lateinit var adapter: LeaderboardAdapter
    private var currentRankingType: RankingType = RankingType.DAILY

    private val BASE_URL = "http://10.0.2.2:5133/"
    //private const val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net/"

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
            currentRankingType = RankingType.DAILY
            updateTabSelection(RankingType.DAILY)
            fetchData(RankingType.DAILY)
        }

        tvMonthly.setOnClickListener {
            currentRankingType = RankingType.MONTHLY
            updateTabSelection(RankingType.MONTHLY)
            fetchData(RankingType.MONTHLY)
        }

        tvAllTime.setOnClickListener {
            currentRankingType = RankingType.TOTAL
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
                val response = when (type) {
                    RankingType.DAILY -> NetworkClient.apiService.getTodayLeaderboard(50)
                    RankingType.MONTHLY -> NetworkClient.apiService.getMonthLeaderboard(50)
                    RankingType.TOTAL -> NetworkClient.apiService.getAllTimeLeaderboard("total", 50)
                }

                if (response.isSuccessful && response.body() != null) {
                    val list = response.body()!!
                    updateUI(list, type)
                } else {
                    Toast.makeText(this@LeaderboardActivity, "Failed to load", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                android.util.Log.e("RANK_DEBUG", "Error: ${e.message}")
                Toast.makeText(this@LeaderboardActivity, "Network Error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun updateUI(list: List<LeaderboardItem>, type: RankingType) {
        val top3 = list.take(3)
        updatePodium(top3, type)

        val others = if (list.size > 3) list.subList(3, list.size) else emptyList()

        adapter.updateData(others, type)
    }

    private fun updatePodium(top3: List<LeaderboardItem>, type: RankingType) {
        setPodiumData(findViewById(R.id.rank1), top3.getOrNull(0), type)
        setPodiumData(findViewById(R.id.rank2), top3.getOrNull(1), type)
        setPodiumData(findViewById(R.id.rank3), top3.getOrNull(2), type)
    }

    private fun setPodiumData(view: View, data: LeaderboardItem?, type: RankingType) {
        val tvName = view.findViewById<TextView>(R.id.tv_name)
        val tvValue = view.findViewById<TextView>(R.id.tv_value)
        val ivAvatar = view.findViewById<ImageView>(R.id.iv_avatar)

        if (data != null) {
            tvName.text = data.nickname ?: data.username

            // 展示逻辑：显示排放量的同时，获取对应类型的积分
            val displayPoints = when(type) {
                RankingType.DAILY -> data.pointsToday
                RankingType.MONTHLY -> data.pointsMonth
                RankingType.TOTAL -> data.pointsTotal
            }
            tvValue.text = "${String.format("%.1f", data.emissionsTotal)} kg | $displayPoints pts"

            var avatarPath = data.avatarUrl ?: ""
            var fullAvatarUrl = if (avatarPath.isNotEmpty()) {
                if (avatarPath.startsWith("http")) {
                    avatarPath.replace("localhost", "10.0.2.2")
                } else {
                    "$BASE_URL${avatarPath.replace("\\", "/").removePrefix("/")}"
                }
            } else null

            if (fullAvatarUrl != null) {
                fullAvatarUrl = if (fullAvatarUrl.contains("?")) "$fullAvatarUrl&t=${System.currentTimeMillis()}"
                else "$fullAvatarUrl?t=${System.currentTimeMillis()}"
            }

            Glide.with(this)
                .load(fullAvatarUrl)
                .apply(RequestOptions.circleCropTransform())
                .signature(com.bumptech.glide.signature.ObjectKey(System.currentTimeMillis().toString()))
                .skipMemoryCache(true)
                .diskCacheStrategy(DiskCacheStrategy.NONE)
                .placeholder(R.drawable.ic_avatar_placeholder)
                .error(R.drawable.ic_avatar_placeholder)
                .into(ivAvatar)
        } else {
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