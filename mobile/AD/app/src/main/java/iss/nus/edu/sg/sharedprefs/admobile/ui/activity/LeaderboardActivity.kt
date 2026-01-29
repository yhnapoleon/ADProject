package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.graphics.Color
import android.graphics.Typeface
import android.os.Bundle
import android.view.View
import android.widget.ImageView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import iss.nus.edu.sg.sharedprefs.admobile.R

class LeaderboardActivity : AppCompatActivity() {

    private lateinit var tvDaily: TextView
    private lateinit var tvMonthly: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_leaderboard)

        // 设置状态栏颜色
        window.statusBarColor = Color.parseColor("#674fa3")


        // 初始化 Tab 切换控件
        tvDaily = findViewById(R.id.tv_daily)
        tvMonthly = findViewById(R.id.tv_monthly)

        tvDaily.setOnClickListener {
            updateTabSelection(true)
            // 这里以后可以添加加载日榜数据的代码
        }

        tvMonthly.setOnClickListener {
            updateTabSelection(false)
            // 这里以后可以添加加载月榜数据的代码
        }

        // 4. 初始化其他 UI
        setupPodium()
        val recyclerView: RecyclerView = findViewById(R.id.rv_ranking_list)
        recyclerView.layoutManager = LinearLayoutManager(this)

        NavigationUtils.setupBottomNavigation(this, R.id.nav_rank)
    }

    /**
     * 切换 Tab 视觉样式
     * @param isDailySelected 是否选择了日榜
     */
    private fun updateTabSelection(isDailySelected: Boolean) {
        if (isDailySelected) {
            // 选中 Daily
            tvDaily.setBackgroundResource(R.drawable.shape_tab_selected)
            tvDaily.setTextColor(Color.parseColor("#674fa3"))
            tvDaily.setTypeface(null, Typeface.BOLD)

            // 取消选中 Monthly
            tvMonthly.setBackground(null)
            tvMonthly.setTextColor(Color.WHITE)
            tvMonthly.setTypeface(null, Typeface.NORMAL)
        } else {
            // 选中 Monthly
            tvMonthly.setBackgroundResource(R.drawable.shape_tab_selected)
            tvMonthly.setTextColor(Color.parseColor("#674fa3"))
            tvMonthly.setTypeface(null, Typeface.BOLD)

            // 取消选中 Daily
            tvDaily.setBackground(null)
            tvDaily.setTextColor(Color.WHITE)
            tvDaily.setTypeface(null, Typeface.NORMAL)
        }
    }

    private fun setupPodium() {
        val rank1 = findViewById<View>(R.id.rank1)
        val rank2 = findViewById<View>(R.id.rank2)
        val rank3 = findViewById<View>(R.id.rank3)

        val iv1 = rank1.findViewById<ImageView>(R.id.iv_crown)
        iv1.setImageResource(R.drawable.winner1)
        iv1.imageTintList = null

        val iv2 = rank2.findViewById<ImageView>(R.id.iv_crown)
        iv2.setImageResource(R.drawable.winner2)
        iv2.imageTintList = null

        val iv3 = rank3.findViewById<ImageView>(R.id.iv_crown)
        iv3.setImageResource(R.drawable.winner3)
        iv3.imageTintList = null
    }
}