package iss.nus.edu.sg.sharedprefs.admobile

import android.graphics.Color
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar

class EmissionRecordsActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_records)

        window.statusBarColor = Color.parseColor("#674fa3")

        // 🌟 返回逻辑
        findViewById<MaterialToolbar>(R.id.toolbar).setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        setupRecyclerView()
    }

    private fun setupRecyclerView() {
        val recyclerView = findViewById<RecyclerView>(R.id.rv_records)
        recyclerView.layoutManager = LinearLayoutManager(this)

        // 模拟 Web 端截图中的数据
        val mockData = listOf(
            EmissionRecord("Jan 23, 2026", "Food", "2.5", "Beef meal at restaurant"),
            EmissionRecord("Jan 22, 2026", "Transport", "1.8", "Drive car to office (25 km)"),
            EmissionRecord("Jan 21, 2026", "Utilities", "0.5", "Electricity usage"),
            EmissionRecord("Jan 20, 2026", "Food", "1.2", "Chicken pasta"),
            EmissionRecord("Jan 19, 2026", "Transport", "0.9", "Public bus ride")
        )

        recyclerView.adapter = RecordAdapter(mockData)
    }
}