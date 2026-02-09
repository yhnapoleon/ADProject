package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.*
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
import java.util.ArrayList

class ProfileStatsActivity : AppCompatActivity() {

    private var statsData: List<UserStatsResponse> = emptyList()

    private lateinit var tvTotalValue: TextView
    private lateinit var tvComparisonValue: TextView
    private lateinit var tvComparisonDesc: TextView
    private lateinit var spinner: Spinner
    private lateinit var lineChart: LineChart
    private lateinit var pieChart: PieChart

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_statistics)

        window.statusBarColor = Color.WHITE
        window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR

        tvTotalValue = findViewById(R.id.tv_total_emissions_value)
        tvComparisonValue = findViewById(R.id.tv_comparison_value)
        tvComparisonDesc = findViewById(R.id.tv_comparison_desc)
        spinner = findViewById(R.id.spinner_time_range)
        lineChart = findViewById(R.id.lineChart)
        pieChart = findViewById(R.id.pieChart)

        findViewById<MaterialToolbar>(R.id.toolbar).setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        initChartStyles()
        fetchStatsFromServer()
    }

    private fun initChartStyles() {
        lineChart.apply {
            description.isEnabled = false
            axisRight.isEnabled = false
            xAxis.position = XAxis.XAxisPosition.BOTTOM
            xAxis.setDrawGridLines(false)
            animateY(1000)
        }

        pieChart.apply {
            isDrawHoleEnabled = true
            setHoleColor(Color.TRANSPARENT)
            holeRadius = 40f
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
                    updateTopCards()
                    setupLineChart()
                    setupTimeRangeSpinner()
                } else {
                    Toast.makeText(this@ProfileStatsActivity, "Failed to load statistics", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@ProfileStatsActivity, "Network error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun updateTopCards() {
        if (statsData.isEmpty()) return

        // 1. 显示总排放量
        val totalSum = statsData.sumOf { it.emissionsTotal }
        tvTotalValue.text = "${String.format("%.0f", totalSum)} kg"

        // 2. 🌟 修改后的 Comparison 逻辑：计算所有月份的平均对比
        val userAvg = statsData.map { it.emissionsTotal }.average()
        val baseAvg = statsData.map { it.averageAllUsers }.average()

        if (baseAvg > 0) {
            val diffPercent = ((userAvg - baseAvg) / baseAvg) * 100

            if (diffPercent > 0) {
                tvComparisonValue.text = "↑ ${String.format("%.1f", diffPercent)}%"
                tvComparisonValue.setTextColor(Color.parseColor("#FF5252"))
                tvComparisonDesc.text = "higher than global avg"
                tvComparisonDesc.setTextColor(Color.parseColor("#FF5252"))
            } else {
                val absPercent = Math.abs(diffPercent)
                tvComparisonValue.text = "↓ ${String.format("%.1f", absPercent)}%"
                tvComparisonValue.setTextColor(Color.parseColor("#4CAF50"))
                tvComparisonDesc.text = "lower than global avg"
                tvComparisonDesc.setTextColor(Color.parseColor("#4CAF50"))
            }
        } else {
            tvComparisonValue.text = "--"
            tvComparisonDesc.text = "no baseline data"
        }
    }

    private fun setupLineChart() {
        val entries = ArrayList<Entry>()
        val labels = ArrayList<String>()

        statsData.forEachIndexed { index, data ->
            entries.add(Entry(index.toFloat(), data.emissionsTotal.toFloat()))
            labels.add(data.month)
        }

        val dataSet = LineDataSet(entries, "Monthly (kg)").apply {
            color = Color.parseColor("#674fa3")
            setCircleColor(Color.parseColor("#674fa3"))
            lineWidth = 3f
            mode = LineDataSet.Mode.CUBIC_BEZIER
            setDrawFilled(true)
            fillColor = Color.parseColor("#674fa3")
            fillAlpha = 35
            setDrawValues(false)
        }

        lineChart.xAxis.valueFormatter = IndexAxisValueFormatter(labels)
        lineChart.xAxis.granularity = 1f
        lineChart.data = LineData(dataSet)
        lineChart.invalidate()
    }

    private fun setupTimeRangeSpinner() {
        val options = mutableListOf("All Time")
        options.addAll(statsData.map { it.month })

        val adapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, options)
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        spinner.adapter = adapter

        spinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(p0: AdapterView<*>?, p1: View?, pos: Int, p3: Long) {
                updatePieChart(options[pos])
            }
            override fun onNothingSelected(p0: AdapterView<*>?) {}
        }
    }

    private fun updatePieChart(selectedRange: String) {
        val entries = ArrayList<PieEntry>()

        if (selectedRange == "All Time") {
            val foodSum = statsData.sumOf { it.food }.toFloat()
            val transportSum = statsData.sumOf { it.transport }.toFloat()
            val utilitySum = statsData.sumOf { it.utility }.toFloat()
            entries.add(PieEntry(foodSum, "Food"))
            entries.add(PieEntry(transportSum, "Travel"))
            entries.add(PieEntry(utilitySum, "Utility"))
        } else {
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
                Color.parseColor("#FFD54F")
            )
            sliceSpace = 3f
            valueTextColor = Color.WHITE
            valueTextSize = 12f
        }

        pieChart.data = PieData(dataSet)
        pieChart.invalidate()
    }
}