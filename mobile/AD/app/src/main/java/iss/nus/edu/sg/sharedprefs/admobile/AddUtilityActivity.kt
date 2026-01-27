package iss.nus.edu.sg.sharedprefs.admobile

import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.card.MaterialCardView

class AddUtilityActivity : AppCompatActivity() {

    // 定义排放因子常量 (新加坡 2023-2024 参考标准)
    private val ELEC_FACTOR = 0.4085 // kg CO2e / kWh
    private val WATER_FACTOR = 0.191  // kg CO2e / m3

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_utility_activity)

        // 绑定视图
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        val cardScan: MaterialCardView = findViewById(R.id.card_scan_bill)
        val etElectricity: EditText = findViewById(R.id.et_electricity)
        val etWater: EditText = findViewById(R.id.et_water)
        val tvElecCarbon: TextView = findViewById(R.id.tv_electricity_carbon)
        val tvWaterCarbon: TextView = findViewById(R.id.tv_water_carbon)
        val btnSave: Button = findViewById(R.id.save_button)

        // 设置 Toolbar 返回
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        // --- 实时计算逻辑 ---

        // 电量输入监听
        etElectricity.addTextChangedListener(object : TextWatcher {
            override fun afterTextChanged(s: Editable?) {
                calculateAndShowCarbon(s.toString(), ELEC_FACTOR, tvElecCarbon)
            }
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
        })

        // 水量输入监听
        etWater.addTextChangedListener(object : TextWatcher {
            override fun afterTextChanged(s: Editable?) {
                calculateAndShowCarbon(s.toString(), WATER_FACTOR, tvWaterCarbon)
            }
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
        })

        // 点击卡片模拟拍照识别
        cardScan.setOnClickListener {
            Toast.makeText(this, "Scanning bill...", Toast.LENGTH_SHORT).show()
            // 填入数据会自动触发上面的 addTextChangedListener
            etElectricity.setText("125.8")
            etWater.setText("10.5")
            Toast.makeText(this, "Data extracted and Carbon calculated!", Toast.LENGTH_SHORT).show()
        }

        // 保存按钮
        btnSave.setOnClickListener {
            val elecValue = etElectricity.text.toString()
            val waterValue = etWater.text.toString()

            if (elecValue.isNotEmpty() || waterValue.isNotEmpty()) {
                // 这里可以进一步计算总碳排放并保存
                Toast.makeText(this, "Usage Saved Locally", Toast.LENGTH_SHORT).show()
                finish()
            } else {
                Toast.makeText(this, "Please enter usage manually or scan bill", Toast.LENGTH_SHORT).show()
            }
        }
    }

    /**
     * 计算并更新 UI
     */
    private fun calculateAndShowCarbon(input: String, factor: Double, resultView: TextView) {
        val usage = input.toDoubleOrNull() ?: 0.0
        val carbon = usage * factor
        resultView.text = String.format("Carbon: %.2f kg CO2e", carbon)
    }
}