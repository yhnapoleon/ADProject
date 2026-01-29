package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Spinner
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.github.mikephil.charting.charts.LineChart
import com.github.mikephil.charting.charts.PieChart
import com.github.mikephil.charting.components.Legend
import com.github.mikephil.charting.components.XAxis
import com.github.mikephil.charting.data.Entry
import com.github.mikephil.charting.data.LineData
import com.github.mikephil.charting.data.LineDataSet
import com.github.mikephil.charting.data.PieData
import com.github.mikephil.charting.data.PieDataSet
import com.github.mikephil.charting.data.PieEntry
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.button.MaterialButton
import iss.nus.edu.sg.sharedprefs.admobile.R

class ProfileStatsActivity : AppCompatActivity() {

    // 将 timeRanges 定义为类成员，方便多处使用
    private val timeRanges = arrayOf("All Time", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul")

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_statistics)

        // 设置状态栏颜色
        window.statusBarColor = Color.WHITE
        window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR

        val toolbar = findViewById<MaterialToolbar>(R.id.toolbar)
        toolbar.setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        setupLineChart()
        setupPieChart()
        setupTimeRangeSpinner() // 🌟 初始化选择器

    }

    private fun setupTimeRangeSpinner() {
        val spinner = findViewById<Spinner>(R.id.spinner_time_range)

        // 设置适配器
        val adapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, timeRanges)
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        spinner.adapter = adapter

        // 监听选择事件
        spinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                updatePieChartData(timeRanges[position])
            }
            override fun onNothingSelected(parent: AdapterView<*>?) {}
        }
    }

    private fun updatePieChartData(range: String) {
        val pieChart = findViewById<PieChart>(R.id.pieChart)
        val entries = ArrayList<PieEntry>()

        // 🌟 模拟不同时间范围的数据切换
        if (range == "All Time") {
            entries.add(PieEntry(1605f, "Food"))
            entries.add(PieEntry(1083f, "Travel"))
            entries.add(PieEntry(645f, "Utility"))
        } else {
            // 假设单月数据的比例有所不同
            entries.add(PieEntry(150f, "Food"))
            entries.add(PieEntry(90f, "Travel"))
            entries.add(PieEntry(60f, "Utility"))
        }

        val dataSet = PieDataSet(entries, "")
        dataSet.colors = arrayListOf(
            Color.parseColor("#674fa3"),
            Color.parseColor("#64B5F6"),
            Color.parseColor("#FFEB3B")
        )
        dataSet.sliceSpace = 2f
        dataSet.valueTextColor = Color.WHITE
        dataSet.valueTextSize = 12f

        pieChart.data = PieData(dataSet)
        pieChart.highlightValues(null) // 清除点击高亮
        pieChart.animateY(800)        // 🌟 切换时的平滑动画
        pieChart.invalidate()         // 刷新
    }

    private fun setupLineChart() {
        val lineChart = findViewById<LineChart>(R.id.lineChart)

        val entries = ArrayList<Entry>()
        entries.add(Entry(0f, 250f))
        entries.add(Entry(1f, 240f))
        entries.add(Entry(2f, 275f))
        entries.add(Entry(3f, 275f))
        entries.add(Entry(4f, 290f))
        entries.add(Entry(5f, 320f))
        entries.add(Entry(6f, 340f))

        val dataSet = LineDataSet(entries, "Total Emissions (kg)")

        dataSet.color = Color.parseColor("#674fa3")
        dataSet.setCircleColor(Color.parseColor("#674fa3"))
        dataSet.lineWidth = 3f
        dataSet.mode = LineDataSet.Mode.CUBIC_BEZIER
        dataSet.setDrawFilled(true)
        dataSet.fillColor = Color.parseColor("#674fa3")
        dataSet.fillAlpha = 30
        dataSet.setDrawValues(false)

        lineChart.xAxis.apply {
            position = XAxis.XAxisPosition.BOTTOM
            valueFormatter =
                IndexAxisValueFormatter(arrayOf("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul"))
            setDrawGridLines(false)
            granularity = 1f
        }

        lineChart.axisRight.isEnabled = false
        lineChart.description.isEnabled = false
        lineChart.data = LineData(dataSet)
        lineChart.animateY(1000)
    }

    private fun setupPieChart() {
        val pieChart = findViewById<PieChart>(R.id.pieChart)

        // 初始配置样式
        pieChart.apply {
            isDrawHoleEnabled = false
            description.isEnabled = false
            legend.isEnabled = true
            legend.verticalAlignment = Legend.LegendVerticalAlignment.BOTTOM
            legend.horizontalAlignment = Legend.LegendHorizontalAlignment.CENTER
        }

        // 初始填充一次 All Time 数据
        updatePieChartData("All Time")
    }

    private fun performLogout() {
        // 1. 清除 SharedPreferences 中的登录状态
        val prefs = getSharedPreferences("EcoLensPrefs", MODE_PRIVATE)
        prefs.edit().clear().apply() // 🌟 清空所有数据，包括 Token 和用户名

        // 2. 提示用户
        Toast.makeText(this, "Logged out successfully", Toast.LENGTH_SHORT).show()

        // 3. 跳转回登录页面
        val intent = Intent(this, LoginActivity::class.java)

        // 🌟 关键：清空 Activity 栈，防止用户按返回键又回到 Profile 页面
        intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        startActivity(intent)

        // 4. 销毁当前页面
        finish()
    }
}