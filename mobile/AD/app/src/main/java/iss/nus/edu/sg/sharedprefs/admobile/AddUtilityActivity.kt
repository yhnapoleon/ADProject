package iss.nus.edu.sg.sharedprefs.admobile

import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.provider.MediaStore
import android.text.Editable
import android.text.TextWatcher
import android.view.View
import android.widget.Button
import android.widget.EditText
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import com.bumptech.glide.Glide
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.card.MaterialCardView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import java.io.FileOutputStream

class AddUtilityActivity : AppCompatActivity() {

    // 定义排放因子常量 (新加坡 2023-2024 参考标准)
    private val ELEC_FACTOR = 0.4085 // kg CO2e / kWh
    private val WATER_FACTOR = 0.191  // kg CO2e / m3

    private lateinit var cardScan: MaterialCardView
    private lateinit var ivBillPreview: ImageView
    private lateinit var etElectricity: EditText
    private lateinit var etWater: EditText
    private lateinit var tvElecCarbon: TextView
    private lateinit var tvWaterCarbon: TextView
    private lateinit var btnSave: Button

    private var selectedImageUri: Uri? = null
    private var selectedImageFile: File? = null

    // 图片选择器
    private val imagePickerLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let { 
            selectedImageUri = it
            displayImage(it)
            uploadAndRecognizeBill(it)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_utility_activity)

        // 绑定视图
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        cardScan = findViewById(R.id.card_scan_bill)
        ivBillPreview = findViewById(R.id.iv_bill_preview)
        etElectricity = findViewById(R.id.et_electricity)
        etWater = findViewById(R.id.et_water)
        tvElecCarbon = findViewById(R.id.tv_electricity_carbon)
        tvWaterCarbon = findViewById(R.id.tv_water_carbon)
        btnSave = findViewById(R.id.save_button)

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

        // 点击卡片选择图片并识别
        cardScan.setOnClickListener {
            imagePickerLauncher.launch("image/*")
        }

        // 保存按钮
        btnSave.setOnClickListener {
            saveUtilityBill()
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

    private fun displayImage(uri: Uri) {
        Glide.with(this)
            .load(uri)
            .centerCrop()
            .into(ivBillPreview)
    }

    private fun uploadAndRecognizeBill(uri: Uri) {
        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        Toast.makeText(this, "Scanning bill...", Toast.LENGTH_SHORT).show()

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // 将 Uri 转换为 File
                val inputStream = contentResolver.openInputStream(uri)
                val tempFile = File(cacheDir, "bill_${System.currentTimeMillis()}.jpg")
                val outputStream = FileOutputStream(tempFile)
                
                inputStream?.use { input ->
                    outputStream.use { output ->
                        input.copyTo(output)
                    }
                }
                
                selectedImageFile = tempFile

                // 调用账单上传和OCR识别API
                val response = ApiHelper.executeUploadFile(this@AddUtilityActivity, "/api/utility-bills/upload", tempFile, "file")
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        try {
                            val jsonResponse = JSONObject(responseBody)
                            
                            // 提取识别出的数据（后端返回的是 Usage，不是 Cost）
                            val electricityUsage = jsonResponse.optDouble("electricityUsage", 0.0)
                            val waterUsage = jsonResponse.optDouble("waterUsage", 0.0)
                            val gasUsage = jsonResponse.optDouble("gasUsage", 0.0)
                            
                            // 如果有用电量数据，填充到输入框
                            if (electricityUsage > 0) {
                                etElectricity.setText(electricityUsage.toString())
                            }
                            
                            // 如果有用水量数据，填充到输入框
                            if (waterUsage > 0) {
                                etWater.setText(waterUsage.toString())
                            }
                            
                            Toast.makeText(this@AddUtilityActivity, "Bill recognized and saved successfully!", Toast.LENGTH_LONG).show()
                            finish() // 识别成功并保存后，关闭页面
                        } catch (e: Exception) {
                            Toast.makeText(this@AddUtilityActivity, "Failed to parse recognition result", Toast.LENGTH_SHORT).show()
                            android.util.Log.e("AddUtilityActivity", "Parse error", e)
                        }
                    } else {
                        val errorMsg = if (responseBody != null) {
                            try {
                                JSONObject(responseBody).optString("message", "Recognition failed")
                            } catch (e: Exception) {
                                "Recognition failed: ${response.code}"
                            }
                        } else {
                            "Recognition failed: ${response.code}"
                        }
                        Toast.makeText(this@AddUtilityActivity, errorMsg, Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@AddUtilityActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("AddUtilityActivity", "Recognition error", e)
                }
            }
        }
    }

    private fun saveUtilityBill() {
        val elecValue = etElectricity.text.toString()
        val waterValue = etWater.text.toString()

        if (elecValue.isEmpty() && waterValue.isEmpty()) {
            Toast.makeText(this, "Please enter usage manually or scan bill", Toast.LENGTH_SHORT).show()
            return
        }

        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        // 如果有图片，上传图片；否则只保存手动输入的数据
        if (selectedImageFile != null && selectedImageFile!!.exists()) {
            // 已经通过上传识别保存了，这里只需要提示
            Toast.makeText(this, "Bill already saved", Toast.LENGTH_SHORT).show()
            finish()
        } else {
            // 手动输入的数据需要单独保存（这里可以调用手动输入API，如果有的话）
            Toast.makeText(this, "Please scan bill to save automatically, or use manual entry API", Toast.LENGTH_LONG).show()
        }
    }
}