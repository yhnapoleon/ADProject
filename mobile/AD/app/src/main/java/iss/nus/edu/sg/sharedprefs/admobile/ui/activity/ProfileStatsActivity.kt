package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.charts.PieChart
import com.github.mikephil.charting.components.Legend
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.*
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter
import com.google.android.material.appbar.MaterialToolbar
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UserStatsResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import kotlinx.coroutines.launch

class ProfileStatsActivity : AppCompatActivity() {

    private var statsData: List<UserStatsResponse> = emptyList()

    private lateinit var tvTotalValue: TextView
    private lateinit var tvCompValue: TextView
    private lateinit var spinner: Spinner
    private lateinit var lineChart: LineChart
    private lateinit var pieChart: PieChart

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_statistics)

        // 设置状态栏
        window.statusBarColor = Color.WHITE
        window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR

        // 1. 初始化视图
        tvTotalValue = findViewById(R.id.tv_total_emissions_value)
        tvCompValue = findViewById(R.id.tv_comparison_value)
        spinner = findViewById(R.id.spinner_time_range)
        lineChart = findViewById(R.id.lineChart)
        pieChart = findViewById(R.id.pieChart)

        findViewById<MaterialToolbar>(R.id.toolbar).setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        // 2. 初始配置图表样式
        initChartStyles()

        // 3. 从后端获取真实数据
        fetchStatsFromServer()
    }

    private fun initChartStyles() {
        // LineChart 基础样式
        lineChart.apply {
            description.isEnabled = false
            axisRight.isEnabled = false
            xAxis.position = XAxis.XAxisPosition.BOTTOM
            xAxis.setDrawGridLines(false)
            animateY(1000)
        }

        // PieChart 基础样式
        pieChart.apply {
            isDrawHoleEnabled = false
            description.isEnabled = false
            legend.verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM
            legend.horizontalAlignment = Legend.LegendHorizontalAlignment.CENTER
            animateXY(800, 800)
        }
    }

    private fun fetchStatsFromServer() {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = "Bearer ${prefs.getString("access_token", "")}"

                val response = NetworkClient.apiService.getAboutMe(token)

                if (response.isSuccessful && response.body() != null) {
                    statsData = response.body()!!

                    // 🌟 打印后端返回的完整数据列表
                    android.util.Log.d("ECO_DEBUG", "About-Me Raw Data: $statsData")

                    // 🌟 也可以循环打印每一个月的数据，看得更清楚
                    statsData.forEach { data ->
                        android.util.Log.d("ECO_DEBUG", "Month: ${data.month} | Total: ${data.emissionsTotal} | AvgAll: ${data.averageAllUsers}")
                    }

                    // 更新 UI
                    updateTopCards()
                    setupLineChart()
                    setupTimeRangeSpinner()
                } else {
                    // 打印错误响应信息
                    android.util.Log.e("ECO_DEBUG", "API Error: ${response.code()} - ${response.errorBody()?.string()}")
                    Toast.makeText(this@ProfileStatsActivity, "Failed to load statistics", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                // 打印网络异常信息
                android.util.Log.e("ECO_DEBUG", "Fetch Stats Exception: ${e.message}")
                Toast.makeText(this@ProfileStatsActivity, "Network error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    /**
     * 更新顶部总排放量和对比卡片
     */
    private fun updateTopCards() {
        if (statsData.isEmpty()) return

        // 使用最近一个月的数据
        val latest = statsData.last()
        tvTotalValue.text = "${String.format("%.1f", latest.emissionsTotal)} kg"

        // 计算与全站平均水平的差异
        val avg = latest.averageAllUsers
        if (avg > 0) {
            val diffPercent = ((latest.emissionsTotal - avg) / avg) * 100
            if (diffPercent > 0) {
                tvCompValue.text = "↑ ${String.format("%.1f", diffPercent)}%"
                tvCompValue.setTextColor(Color.RED)
            } else {
                tvCompValue.text = "↓ ${String.format("%.1f", Math.abs(diffPercent))}%"
                tvCompValue.setTextColor(Color.parseColor("#4CAF50"))
            }
        }
    }

    /**
     * 配置折线图：显示个人排放趋势
     */
    private fun setupLineChart() {
        val entries = ArrayList<Entry>()
        val labels = ArrayList<String>()

        statsData.forEachIndexed { index, data ->
            entries.add(Entry(index.toFloat(), data.emissionsTotal.toFloat()))
            labels.add(data.month)
        }

        val dataSet = LineDataSet(entries, "Total Emissions (kg)").apply {
            color = Color.parseColor("#674fa3")
            setCircleColor(Color.parseColor("#674fa3"))
            lineWidth = 3f
            mode = LineDataSet.Mode.CUBIC_BEZIER
            setDrawFilled(true)
            fillColor = Color.parseColor("#674fa3")
            fillAlpha = 30
            setDrawValues(false)
        }

        lineChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)
        lineChart.xAxis.granularity = 1f
        lineChart.data = LineData(dataSet)
        lineChart.invalidate()
    }

    /**
     * 配置时间范围选择器
     */
    private fun setupTimeRangeSpinner() {
        val options = mutableListOf("All Time")
        options.addAll(statsData.map { it.month })

        val adapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, options)
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        spinner.adapter = adapter

        spinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                updatePieChart(options[position])
            }
            override fun onNothingSelected(parent: AdapterView<*>?) {}
        }
    }

    /**
     * 根据选择的月份更新饼图数据
     */
    private fun updatePieChart(selectedRange: String) {
        val entries = ArrayList<PieEntry>()

        if (selectedRange == "All Time") {
            // 计算所有月份的总和
            val foodSum = statsData.sumOf { it.food }.toFloat()
            val transportSum = statsData.sumOf { it.transport }.toFloat()
            val utilitySum = statsData.sumOf { it.utility }.toFloat()

            entries.add(PieEntry(foodSum, "Food"))
            entries.add(PieEntry(transportSum, "Travel"))
            entries.add(PieEntry(utilitySum, "Utility"))
        } else {
            // 查找特定月份的数据
            val data = statsData.find { it.month == selectedRange }
            data?.let {
                entries.add(PieEntry(it.food.toFloat(), "Food"))
                entries.add(PieEntry(it.transport.toFloat(), "Travel"))
                entries.add(PieEntry(it.utility.toFloat(), "Utility"))
            }
        }

        val dataSet = PieDataSet(entries, "").apply {
            colors = arrayListOf(
                Color.parseColor("#674fa3"),
                Color.parseColor("#64B5F6"),
                Color.parseColor("#FFEB3B")
            )
            sliceSpace = 2f
            valueTextColor = Color.BLACK
            valueTextSize = 12f
        }

        pieChart.data = PieData(dataSet)
        pieChart.invalidate()
    }
}